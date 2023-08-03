using Procfiler.Core.Collector.CustomTraceEvents;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Collector;

public record ClrEventsCollectionContext(
  int Pid,
  int Duration,
  int Timeout,
  ProvidersCategoryKind ProvidersCategoryKind
);

public record ClrEventsCollectionContextWithBinaryStacks(
  int Pid,
  int Duration,
  int Timeout,
  ProvidersCategoryKind ProvidersCategoryKind,
  string? PathToBinaryStacks
) : ClrEventsCollectionContext(Pid, Duration, Timeout, ProvidersCategoryKind);

public interface IClrEventsCollector
{
  CollectedEvents CollectEvents(ClrEventsCollectionContext context);
}

[AppComponent]
public class ClrEventsCollector(
  IProcfilerLogger logger,
  IEventPipeProvidersProvider eventPipeProvidersProvider,
  ITransportCreationWaiter transportCreationWaiter,
  ICustomClrEventsFactory customClrEventsFactory,
  IBinaryShadowStacksReader binaryShadowStacksReader
) : IClrEventsCollector
{
  public CollectedEvents CollectEvents(ClrEventsCollectionContext context)
  {
    try
    {
      using var tempPathCookie = new TempFileCookie(logger);
      var (pid, duration, timeout, category) = context;
      ListenToProcessAndWriteToFile(pid, duration, timeout, category, tempPathCookie);
      var binaryStacksPath = context switch
      {
        ClrEventsCollectionContextWithBinaryStacks ctx => ctx.PathToBinaryStacks,
        _ => null
      };
      
      return ReadEventsFromFile(tempPathCookie, binaryStacksPath);
    }
    catch (Exception ex)
    {
      logger.LogError("{Method}: {Message}", nameof(CollectEvents), ex.Message);
      throw;
    }
  }

  private void ListenToProcessAndWriteToFile(
    int processId,
    int durationMs,
    int maxWaitForLogWriteTimeoutMs,
    ProvidersCategoryKind category,
    TempFileCookie tempPathCookie)
  {
    using var performanceCookie = new PerformanceCookie(nameof(ListenToProcessAndWriteToFile), logger);
    
    var client = new DiagnosticsClient(processId);
    transportCreationWaiter.WaitUntilTransportIsCreatedOrThrow(processId);
    var providers = eventPipeProvidersProvider.GetProvidersFor(category);
    using var session = client.StartEventPipeSession(providers, circularBufferMB: 2048);
    client.ResumeRuntime();

    using var fs = new FileStream(tempPathCookie.FullFilePath, FileMode.Create, FileAccess.Write);
    var copyTask = session.EventStream.CopyToAsync(fs);
    var whenAnyTask = Task.WhenAny(Task.Delay(durationMs), copyTask);
    whenAnyTask.Wait();
    var firstFinishedTask = whenAnyTask.Result;

    if (firstFinishedTask != copyTask)
    {
      try
      {
        session.Stop();
      }
      catch (ServerNotAvailableException)
      {
        logger.LogInformation("The server is already stopped, so no need to terminate session");
      }
    }

    var delayTask = Task.Delay(maxWaitForLogWriteTimeoutMs);
    whenAnyTask = Task.WhenAny(copyTask, delayTask);
    whenAnyTask.Wait();
    firstFinishedTask = whenAnyTask.Result;

    if (firstFinishedTask == delayTask)
    {
      logger.LogInformation("The timeout for waiting for ending of write event pipe logs has expired");
      logger.LogInformation("Now we will wait until the writing of events to temp file will be finished");
      copyTask.Wait();
    }
  }

  private CollectedEvents ReadEventsFromFile(TempFileCookie tempPathCookie, string? binaryStacks)
  {
    var options = new TraceLogOptions
    {
      ContinueOnError = true
    };
    
    var etlxFilePath = TraceLog.CreateFromEventPipeDataFile(tempPathCookie.FullFilePath, options: options);
    using var etlxCookie = new TempFileCookie(etlxFilePath, logger);

    return ReadEventsFromFileInternal(etlxFilePath, binaryStacks);
  }

  private CollectedEvents ReadEventsFromFileInternal(string etlxFilePath, string? binaryStacksPath)
  {
    using var performanceCookie = new PerformanceCookie(nameof(ReadEventsFromFile), logger);
    using var traceLog = new TraceLog(etlxFilePath);
    var stackSource = InitializeStackSource(traceLog);

    logger.LogInformation(
      "We GOT {EventCount} events, We LOST {EventsLost} events", traceLog.EventCount, traceLog.EventsLost);
    
    var statistics = new Statistics();
    var events = new EventRecordWithMetadata[traceLog.EventCount];
    var context = new CreatingEventContext(stackSource, traceLog);
    var shadowStacks = binaryStacksPath switch
    {
      { } => binaryShadowStacksReader.ReadStackEvents(binaryStacksPath),
      null => new FromEventsShadowStacks(stackSource)
    };
    
    var globalData = new SessionGlobalData(shadowStacks);

    using (var _ = new PerformanceCookie("ProcessingEvents", logger))
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

    statistics.LogMyself(logger);

    return new CollectedEvents(CreateEventCollection(events), globalData);
  }

  private IEventsCollection CreateEventCollection(EventRecordWithMetadata[] events)
  {
    using (new PerformanceCookie($"{GetType()}::SortingEvents", logger))
    {
      Array.Sort(events, static (first, second) =>
      {
        if (first.Stamp > second.Stamp) return 1;
        if (first.Stamp < second.Stamp) return -1;
        return 0;
      });
    }
    
    return new EventsCollectionImpl(events, logger);
  }
  
  private EventWithGlobalDataUpdate CreateEventWithMetadataFromClrEvent(
    TraceEvent traceEvent, CreatingEventContext context, ref Statistics statistics)
  {
    if (statistics.EventsCount % 10_000 == 0)
    {
      var processedCount = statistics.EventsCount;
      var allEvents = context.Log.EventCount;
      logger.LogTrace("Processed {Processed} out of {OverallCount}", processedCount, allEvents);
    }

    var eventId = (int)traceEvent.ID;
    if (customClrEventsFactory.NeedToCreateCustomWrapper(eventId))
    {
      traceEvent = customClrEventsFactory.CreateWrapperEvent(traceEvent);
    }

    UpdateStatisticsAfterEventProcession(traceEvent, ref statistics);
    var managedThreadId = traceEvent.GetManagedThreadIdThroughStack(context.Source);
    var record = new EventRecordWithMetadata(traceEvent, managedThreadId, (int)traceEvent.CallStackIndex());

    var typeIdToName = TryExtractTypeIdToName(traceEvent, record.Metadata);
    var methodIdToFqn = TryExtractMethodToId(traceEvent, record.Metadata);
    
    return new EventWithGlobalDataUpdate(traceEvent, record, typeIdToName, methodIdToFqn);
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