using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core;
using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Core.Serialization;
using Procfiler.Utils;

namespace Procfiler.Commands.CollectClrEvents.Base;

public abstract class CollectAndSplitCommandBase<TKey> : CollectCommandBase where TKey : notnull
{
  protected record struct CollectAndSplitContext(bool UseFilters, bool UseMutators, bool MergeFromUndefinedThread)
  {
    public static CollectAndSplitContext DoNothing { get; } = new(false, false, false);
    public static CollectAndSplitContext DoEverything { get; } = new(true, true, true);
  }
  
  private readonly IDelegatingEventsSerializer myDelegatingEventsSerializer;
  private readonly IStackTraceSerializer myStackTraceSerializer;
  private readonly IUndefinedThreadsEventsMerger myUndefinedThreadsEventsMerger;
  
  protected readonly IUnitedEventsProcessor UnitedEventsProcessor;


  protected CollectAndSplitCommandBase(
    IProcfilerLogger logger,
    ICommandExecutorDependantOnContext commandExecutor,
    IUndefinedThreadsEventsMerger undefinedThreadsEventsMerger,
    IUnitedEventsProcessor unitedEventsProcessor,
    IDelegatingEventsSerializer delegatingEventsSerializer, 
    IStackTraceSerializer stackTraceSerializer) : base(logger, commandExecutor)
  {
    myUndefinedThreadsEventsMerger = undefinedThreadsEventsMerger;
    UnitedEventsProcessor = unitedEventsProcessor;
    myDelegatingEventsSerializer = delegatingEventsSerializer;
    myStackTraceSerializer = stackTraceSerializer;
  }


  protected ValueTask ExecuteSimpleSplitCommand(
    CollectClrEventsContext context,
    Func<EventRecordWithMetadata, TKey> keyExtractor,
    CollectAndSplitContext collectAndSplitContext)
  {
    return ExecuteCommandAsync(context, async collectedEvents =>
    {
      PathUtils.CheckIfDirectoryOrThrow(context.CommonContext.OutputPath);
      
      var (allEvents, globalData) = collectedEvents;
      var (useFilters, useMutators, mergeFromUndefinedThread) = collectAndSplitContext;
      var processingContext = CreateContext(allEvents, globalData, useFilters, useMutators, false);
      UnitedEventsProcessor.ProcessFullEventLog(processingContext);
      
      var eventsByKey = SplitEventsHelper.SplitByKey(Logger, allEvents, keyExtractor);
      var undefinedEventsPair = FindEventsForUndefinedThread(eventsByKey);
      
      var undefinedEvents = undefinedEventsPair?.Value;
      undefinedEvents = AddNewManagedThreadsFromUndefined(eventsByKey, globalData, undefinedEvents);

      if (!mergeFromUndefinedThread)
      {
        undefinedEvents = null;
      }
      else if (undefinedEvents is { } && undefinedEventsPair is { Key: { } key })
      {
        eventsByKey.Remove(key);
      }

      foreach (var (key, managedEvents) in eventsByKey)
      {
        await ProcessEventsAsync(managedEvents, undefinedEvents, context, key, globalData, collectAndSplitContext);
      }

      await SerializeStacksAsync(context, globalData);
    });
  }

  private async ValueTask ProcessEventsAsync(
    IEventsCollection correspondingEvents,
    IEventsCollection? undefinedThreadEvents,
    CollectClrEventsContext context,
    TKey key,
    SessionGlobalData globalData, 
    CollectAndSplitContext collectAndSplitContext)
  {
    if (collectAndSplitContext.UseMutators)
    {
      UnitedEventsProcessor.ApplyMultipleMutators(correspondingEvents, globalData, EmptyCollections<Type>.EmptySet);
    }

    if (correspondingEvents.Count == 0) return;

    IEnumerable<EventRecordWithMetadata> mergedEvents = correspondingEvents;
    if (undefinedThreadEvents is { })
    {
      using (new PerformanceCookie($"{GetType().Name}::MergingEvents", Logger))
      {
        mergedEvents = myUndefinedThreadsEventsMerger.MergeLazy(correspondingEvents, undefinedThreadEvents);
      }
    }

    var outputFormat = context.CommonContext.SerializationContext.OutputFormat;
    var extension = outputFormat.GetExtension();
    var filePath = Path.Combine(context.CommonContext.OutputPath, $"{key.ToString()}.{extension}");

    await using var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
    await myDelegatingEventsSerializer.SerializeEventsAsync(mergedEvents, fs, outputFormat);
  }

  private async ValueTask SerializeStacksAsync(CollectClrEventsContext context, SessionGlobalData globalData)
  {
    var stacksFilePath = Path.Combine(context.CommonContext.OutputPath, "stacks.txt");
    await using var fs = new FileStream(stacksFilePath, FileMode.OpenOrCreate, FileAccess.Write);
    await myStackTraceSerializer.SerializeStackTracesAsync(globalData.Stacks.Values, fs);
  }

  private static EventsProcessingContext CreateContext(
    IEventsCollection currentEvents,
    SessionGlobalData globalData,
    bool useFilters,
    bool useMutators,
    bool useMethodStartEndMutator)
  {
    var config = useMethodStartEndMutator switch
    {
      false => EventsProcessingConfig.CreateContextWithoutStartAndEnd(useFilters, useMutators),
      true => new EventsProcessingConfig(useFilters, useMutators, EmptyCollections<Type>.EmptySet)
    };
    
    return new EventsProcessingContext(currentEvents, globalData, config);
  }
  
  protected virtual KeyValuePair<TKey, IEventsCollection?>? FindEventsForUndefinedThread(
    Dictionary<TKey, IEventsCollection> eventsByKey)
  {
    return null;
  }

  protected virtual IEventsCollection? AddNewManagedThreadsFromUndefined(
    IDictionary<TKey, IEventsCollection> events,
    SessionGlobalData globalData,
    IEventsCollection? undefinedEvents)
  {
    return undefinedEvents;
  }
}