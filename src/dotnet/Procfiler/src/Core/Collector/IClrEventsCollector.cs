using Procfiler.Core.Collector.CustomTraceEvents;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Collector;

public interface IClrEventsCollector
{
  ValueTask<CollectedEvents> CollectEventsAsync(int pid, int duration, int timeout, ProvidersCategoryKind category);
}

[AppComponent]
public class ClrEventsCollector : IClrEventsCollector
{
  private readonly IProcfilerLogger myLogger;
  private readonly IEventPipeProvidersProvider myEventPipeProvidersProvider;
  private readonly ITransportCreationWaiter myTransportCreationWaiter;
  private readonly ICustomClrEventsFactory myCustomClrEventsFactory;


  public ClrEventsCollector(
    IProcfilerLogger logger,
    IEventPipeProvidersProvider eventPipeProvidersProvider,
    ITransportCreationWaiter transportCreationWaiter,
    ICustomClrEventsFactory customClrEventsFactory)
  {
    myLogger = logger;
    myEventPipeProvidersProvider = eventPipeProvidersProvider;
    myTransportCreationWaiter = transportCreationWaiter;
    myCustomClrEventsFactory = customClrEventsFactory;
  }


  public async ValueTask<CollectedEvents> CollectEventsAsync(int pid, int duration, int timeout, ProvidersCategoryKind category)
  {
    try
    {
      using var tempPathCookie = new TempFileCookie(myLogger);
      await ListenToProcessAndWriteToFile(pid, duration, timeout, category, tempPathCookie);
      return ReadEventsFromFile(tempPathCookie);
    }
    catch (Exception ex)
    {
      myLogger.LogError("{Method}: {Message}", nameof(CollectEventsAsync), ex.Message);
      throw;
    }
  }

  private async ValueTask ListenToProcessAndWriteToFile(
    int processId,
    int durationMs,
    int maxWaitForLogWriteTimeoutMs,
    ProvidersCategoryKind category,
    TempFileCookie tempPathCookie)
  {
    using var performanceCookie = new PerformanceCookie(nameof(ListenToProcessAndWriteToFile), myLogger);
    
    var client = new DiagnosticsClient(processId);
    myTransportCreationWaiter.WaitUntilTransportIsCreatedOrThrow(processId);
    var providers = myEventPipeProvidersProvider.GetProvidersFor(category);
    using var session = client.StartEventPipeSession(providers, circularBufferMB: 2048);
    client.ResumeRuntime();

    await using var fs = new FileStream(tempPathCookie.FullFilePath, FileMode.Create, FileAccess.Write);
    var copyTask = session.EventStream.CopyToAsync(fs);
    var firstFinishedTask = await Task.WhenAny(Task.Delay(durationMs), copyTask);

    if (firstFinishedTask != copyTask)
    {
      try
      {
        await session.StopAsync(CancellationToken.None);
      }
      catch (ServerNotAvailableException)
      {
        myLogger.LogInformation("The server is already stopped, so no need to terminate session");
      }
    }

    var delayTask = Task.Delay(maxWaitForLogWriteTimeoutMs);
    firstFinishedTask = await Task.WhenAny(copyTask, delayTask);

    if (firstFinishedTask == delayTask)
    {
      myLogger.LogInformation("The timeout for waiting for ending of write event pipe logs has expired");
      myLogger.LogInformation("Now we will wait until the writing of events to temp file will be finished");
      await copyTask;
    }
  }

  private CollectedEvents ReadEventsFromFile(TempFileCookie tempPathCookie)
  {
    var options = new TraceLogOptions
    {
      ContinueOnError = true
    };
    
    var etlxFilePath = TraceLog.CreateFromEventPipeDataFile(tempPathCookie.FullFilePath, options: options);
    using var etlxCookie = new TempFileCookie(etlxFilePath, myLogger);

    return ReadEventsFromFileInternal(etlxFilePath);
  }

  private CollectedEvents ReadEventsFromFileInternal(string etlxFilePath)
  {
    using var performanceCookie = new PerformanceCookie(nameof(ReadEventsFromFile), myLogger);
    using var traceLog = new TraceLog(etlxFilePath);
    var stackSource = InitializeStackSource(traceLog);

    myLogger.LogInformation(
      "We GOT {EventCount} events, We LOST {EventsLost} events", traceLog.EventCount, traceLog.EventsLost);
    
    var statistics = new Statistics();
    var events = new EventRecordWithMetadata[traceLog.EventCount];
    var context = new CreatingEventContext(stackSource, traceLog, new Dictionary<int, StackTraceInfo>());
    var globalData = new SessionGlobalData();

    using (var _ = new PerformanceCookie("ProcessingEvents", myLogger))
    {
      var index = 0;
      // ReSharper disable once LoopCanBeConvertedToQuery
      foreach (var traceEvent in traceLog.Events)
      {
        var record = CreateEventWithMetadataFromClrEvent(traceEvent, context, ref statistics);
        events[index++] = record.Event;

        globalData.AddInfoFrom(record);
      }
    }

    statistics.LogMyself(myLogger);

    return new CollectedEvents(CreateEventCollection(events), globalData);
  }
  
  private IEventsCollection CreateEventCollection(EventRecordWithMetadata[] events)
  {
    using (new PerformanceCookie($"{GetType()}::SortingEvents", myLogger))
    {
      Array.Sort(events, static (first, second) =>
      {
        if (first.Stamp > second.Stamp) return 1;
        if (first.Stamp < second.Stamp) return -1;
        return 0;
      });
    }
    
    return new EventsCollectionImpl(events, myLogger);
  }
  
  private EventWithGlobalDataUpdate CreateEventWithMetadataFromClrEvent(
    TraceEvent traceEvent, CreatingEventContext context, ref Statistics statistics)
  {
    if (statistics.EventsCount % 10_000 == 0)
    {
      var processedCount = statistics.EventsCount;
      var allEvents = context.Log.EventCount;
      myLogger.LogTrace("Processed {Processed} out of {OverallCount}", processedCount, allEvents);
    }

    var eventId = (int)traceEvent.ID;
    if (myCustomClrEventsFactory.NeedToCreateCustomWrapper(eventId))
    {
      traceEvent = myCustomClrEventsFactory.CreateWrapperEvent(traceEvent);
    }

    var stackTraceInfo = context.GetsStackTraceInfo(traceEvent);
    UpdateStatisticsAfterEventProcession(traceEvent, stackTraceInfo, ref statistics);
    var managedThreadId = stackTraceInfo?.ManagedThreadId ?? -1;
    var stackTraceId = stackTraceInfo?.StackTraceId ?? -1;
    var record = new EventRecordWithMetadata(traceEvent, managedThreadId, stackTraceId);

    var typeIdToName = TryExtractTypeIdToName(traceEvent, record.Metadata);
    var methodIdToFqn = TryExtractMethodToId(traceEvent, record.Metadata);
    
    return new EventWithGlobalDataUpdate(record, stackTraceInfo, typeIdToName, methodIdToFqn);
  }

  private static TypeIdToName? TryExtractTypeIdToName(
    TraceEvent traceEvent, IDictionary<string, string> metadata)
  {
    if (traceEvent.EventName is not TraceEventsConstants.TypeBulkType) return null;
    
    var id = metadata.GetValueOrDefault(TraceEventsConstants.TypeBulkTypeTypeId);
    var name = metadata.GetValueOrDefault(TraceEventsConstants.TypeBulkTypeTypeName);

    if (id is { } && name is { })
    {
      return new TypeIdToName(id, name);
    }

    return null;
  }

  private static MethodIdToFqn? TryExtractMethodToId(
    TraceEvent traceEvent, IDictionary<string, string> metadata)
  {
    if (traceEvent.EventName is not TraceEventsConstants.MethodLoadVerbose) return null;
    
    var methodId = metadata.GetValueOrDefault(TraceEventsConstants.MethodId);
    var name = metadata.GetValueOrDefault(TraceEventsConstants.MethodName);
    var methodNamespace = metadata.GetValueOrDefault(TraceEventsConstants.MethodNamespace);
    var signature = metadata.GetValueOrDefault(TraceEventsConstants.MethodSignature);

    if (name is { } && methodNamespace is { } && signature is { } && methodId is { })
    {
      var mergedName = MutatorsUtil.ConcatenateMethodDetails(name, methodNamespace, signature);
      return new MethodIdToFqn(methodId, mergedName);
    }

    return null;
  }
  
  private static MutableTraceEventStackSource InitializeStackSource(TraceLog traceLog)
  {
    using var symbolReader = new SymbolReader(TextWriter.Null) { SymbolPath = SymbolPath.MicrosoftSymbolServerPath };
    var stackSource = new MutableTraceEventStackSource(traceLog)
    {
      OnlyManagedCodeStacks = true,
    };

    var computer = new SampleProfilerThreadTimeComputer(traceLog, symbolReader)
    {
      IncludeEventSourceEvents = true,
      GroupByStartStopActivity = true,
      UseTasks = true
    };
    
    computer.GenerateThreadTimeStacks(stackSource);
    return stackSource;
  }

  private static void UpdateStatisticsAfterEventProcession(
    TraceEvent @event,
    StackTraceInfo? stackTraceInfo,
    ref Statistics statistics)
  {
    statistics.EventsCountMap.AddOrIncrement(@event.EventName);
    statistics.EventsWithManagedThreadIs.AddCase((stackTraceInfo?.ManagedThreadId ?? -1) != -1);
    ++statistics.EventsCount;

    if (stackTraceInfo is { } && stackTraceInfo.Frames.Length > 0)
    {
      statistics.StackTracesPerEvents.AddOrIncrement(@event.EventName);
      ++statistics.EventsWithStackTraces;
    }

    if (!statistics.EventNamesToPayloadProperties.TryGetValue(@event.EventName, out _))
    {
      statistics.EventNamesToPayloadProperties[@event.EventName] = @event.PayloadNames.ToList();
    }
  }
}