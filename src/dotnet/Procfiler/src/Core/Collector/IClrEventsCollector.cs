using Procfiler.Core.Collector.CustomTraceEvents;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Collector;

public readonly record struct ClrEventsCollectionContext(
  int Pid,
  int Duration,
  int Timeout,
  ProvidersCategoryKind ProvidersCategoryKind,
  string? PathToBinaryStacks
);

public interface IClrEventsCollector
{
  ValueTask<CollectedEvents> CollectEventsAsync(ClrEventsCollectionContext context);
}

[AppComponent]
public class ClrEventsCollector : IClrEventsCollector
{
  private readonly IProcfilerLogger myLogger;
  private readonly IEventPipeProvidersProvider myEventPipeProvidersProvider;
  private readonly ITransportCreationWaiter myTransportCreationWaiter;
  private readonly ICustomClrEventsFactory myCustomClrEventsFactory;
  private readonly IBinaryShadowStacksReader myBinaryShadowStacksReader;


  public ClrEventsCollector(
    IProcfilerLogger logger,
    IEventPipeProvidersProvider eventPipeProvidersProvider,
    ITransportCreationWaiter transportCreationWaiter,
    ICustomClrEventsFactory customClrEventsFactory, 
    IBinaryShadowStacksReader binaryShadowStacksReader)
  {
    myLogger = logger;
    myEventPipeProvidersProvider = eventPipeProvidersProvider;
    myTransportCreationWaiter = transportCreationWaiter;
    myCustomClrEventsFactory = customClrEventsFactory;
    myBinaryShadowStacksReader = binaryShadowStacksReader;
  }


  public async ValueTask<CollectedEvents> CollectEventsAsync(ClrEventsCollectionContext context)
  {
    try
    {
      using var tempPathCookie = new TempFileCookie(myLogger);
      var (pid, duration, timeout, category, binaryStacksPath) = context;
      await ListenToProcessAndWriteToFile(pid, duration, timeout, category, tempPathCookie);
      return await ReadEventsFromFileAsync(tempPathCookie, binaryStacksPath);
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

  private Task<CollectedEvents> ReadEventsFromFileAsync(TempFileCookie tempPathCookie, string binaryStacks)
  {
    var options = new TraceLogOptions
    {
      ContinueOnError = true
    };
    
    var etlxFilePath = TraceLog.CreateFromEventPipeDataFile(tempPathCookie.FullFilePath, options: options);
    using var etlxCookie = new TempFileCookie(etlxFilePath, myLogger);

    return ReadEventsFromFileInternalAsync(etlxFilePath, binaryStacks);
  }

  private async Task<CollectedEvents> ReadEventsFromFileInternalAsync(string etlxFilePath, string binaryStacksPath)
  {
    using var performanceCookie = new PerformanceCookie(nameof(ReadEventsFromFileAsync), myLogger);
    using var traceLog = new TraceLog(etlxFilePath);
    var stackSource = InitializeStackSource(traceLog);

    myLogger.LogInformation(
      "We GOT {EventCount} events, We LOST {EventsLost} events", traceLog.EventCount, traceLog.EventsLost);
    
    var statistics = new Statistics();
    var events = new EventRecordWithMetadata[traceLog.EventCount];
    var context = new CreatingEventContext(stackSource, traceLog);
    var stacks = myBinaryShadowStacksReader.ReadStackEvents(binaryStacksPath);
    var globalData = new SessionGlobalData(stacks);

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

    return new CollectedEvents(GetSortedLinkedListOfEvents(events), globalData);
  }
  
  private IEventsCollection GetSortedLinkedListOfEvents(EventRecordWithMetadata[] events)
  {
    using (new PerformanceCookie($"{GetType()}::SortingEvents", myLogger))
    {
      Array.Sort(events, static (first, second) =>
      {
        if (first.Stamp > second.Stamp) return 1;
        if (first.Stamp < second.Stamp) return -1;
        return 0;
      });

      return new EventsCollectionImpl(events, myLogger);
    }
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

    UpdateStatisticsAfterEventProcession(traceEvent, ref statistics);
    var managedThreadId = traceEvent.GetManagedThreadIdThroughStack(context.Source);
    var record = new EventRecordWithMetadata(traceEvent, managedThreadId);

    var typeIdToName = TryExtractTypeIdToName(traceEvent, record.Metadata);
    var methodIdToFqn = TryExtractMethodToId(traceEvent, record.Metadata);
    
    return new EventWithGlobalDataUpdate(record, typeIdToName, methodIdToFqn);
  }

  private static TypeIdToName? TryExtractTypeIdToName(
    TraceEvent traceEvent, IDictionary<string, string> metadata)
  {
    if (traceEvent.EventName is not TraceEventsConstants.TypeBulkType) return null;
    
    var id = metadata.GetValueOrDefault(TraceEventsConstants.TypeBulkTypeTypeId);
    var name = metadata.GetValueOrDefault(TraceEventsConstants.TypeBulkTypeTypeName);

    if (id is { } && name is { })
    {
      return new TypeIdToName(id.ParseId(), name);
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
      return new MethodIdToFqn(methodId.ParseId(), mergedName);
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

  private static void UpdateStatisticsAfterEventProcession(TraceEvent @event, ref Statistics statistics)
  {
    statistics.EventsCountMap.AddOrIncrement(@event.EventName);
    statistics.EventsWithManagedThreadIs.AddCase(@event.ThreadID != -1);
    ++statistics.EventsCount;

    if (!statistics.EventNamesToPayloadProperties.TryGetValue(@event.EventName, out _))
    {
      statistics.EventNamesToPayloadProperties[@event.EventName] = @event.PayloadNames.ToList();
    }
  }
}