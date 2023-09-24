using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Core.Serialization.XES;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.SplitByMethod;

public interface IByMethodsSplitter
{
  Dictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>>? SplitNonAlloc(
    OnlineMethodsXesSerializer serializer,
    CollectedEvents events,
    string filterPattern,
    InlineMode inlineMode,
    bool mergeUndefinedThreadEvents,
    bool addAsyncMethods);
  
  Dictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>> Split(
    CollectedEvents events,
    string filterPattern,
    InlineMode inlineMode,
    bool mergeUndefinedThreadEvents,
    bool addAsyncMethods);
}

[AppComponent]
public class ByMethodsSplitterImpl(
  IProcfilerLogger logger,
  IEventsCollectionByMethodsSplitter splitter,
  IManagedEventsFromUndefinedThreadExtractor managedEventsExtractor,
  IAsyncMethodsGrouper asyncMethodsGrouper,
  IUnitedEventsProcessor unitedEventsProcessor,
  IUndefinedThreadsEventsMerger undefinedThreadsEventsMerger
) : IByMethodsSplitter
{
  public Dictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>>? SplitNonAlloc(
    OnlineMethodsXesSerializer serializer,
    CollectedEvents events,
    string filterPattern,
    InlineMode inlineMode,
    bool mergeUndefinedThreadEvents,
    bool addAsyncMethods)
  {
    SplitEventsByThreads(events, out var eventsByManagedThreads, out var undefinedThreadEvents);
    undefinedThreadEvents = managedEventsExtractor.Extract(eventsByManagedThreads, undefinedThreadEvents);

    foreach (var (key, threadEvents) in eventsByManagedThreads)
    {
      using var _ = new PerformanceCookie($"{GetType().Name}::{nameof(Split)}::PreparingTrace_{key}", logger);

      ProcessManagedThreadEvents(threadEvents, events.GlobalData);

      var mergedEvents = mergeUndefinedThreadEvents switch
      {
        true => MergeUndefinedThreadEvents(threadEvents, undefinedThreadEvents),
        false => threadEvents
      };
      
      serializer.SerializeThreadEvents(mergedEvents, filterPattern, inlineMode);
    }

    if (addAsyncMethods)
    {
      var result = new Dictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>>();
      AddAsyncMethods(serializer.AllMethodNames, result, eventsByManagedThreads);

      return result;
    }

    return null;
  }
  
  public Dictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>> Split(
    CollectedEvents events,
    string filterPattern,
    InlineMode inlineMode,
    bool mergeUndefinedThreadEvents,
    bool addAsyncMethods)
  {
    SplitEventsByThreads(events, out var eventsByManagedThreads, out var undefinedThreadEvents);
    undefinedThreadEvents = managedEventsExtractor.Extract(eventsByManagedThreads, undefinedThreadEvents);

    var tracesByMethods = new Dictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>>();
    foreach (var (key, threadEvents) in eventsByManagedThreads)
    {
      using var _ = new PerformanceCookie($"{GetType().Name}::{nameof(Split)}::PreparingTrace_{key}", logger);

      ProcessManagedThreadEvents(threadEvents, events.GlobalData);

      var mergedEvents = mergeUndefinedThreadEvents switch
      {
        true => MergeUndefinedThreadEvents(threadEvents, undefinedThreadEvents),
        false => threadEvents
      };

      var eventsTracesByMethods = splitter.Split(mergedEvents, filterPattern, inlineMode);

      foreach (var (methodName, traces) in eventsTracesByMethods)
      {
        var tracesForMethod =
          tracesByMethods.GetOrCreate(methodName, () => new List<IReadOnlyList<EventRecordWithMetadata>>());
        tracesForMethod.AddRange(traces);
      }
    }

    if (addAsyncMethods)
    {
      AddAsyncMethods(tracesByMethods.Keys, tracesByMethods, eventsByManagedThreads);
    }

    return tracesByMethods;
  }

  private void AddAsyncMethods(
    IEnumerable<string> methodsNames,
    Dictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>> tracesByMethods,
    Dictionary<long, IEventsCollection> eventsByManagedThreads)
  {
    var asyncMethodsTraces = asyncMethodsGrouper.GroupAsyncMethods(methodsNames, eventsByManagedThreads);
    foreach (var (asyncMethodName, collection) in asyncMethodsTraces)
    {
      var traces = new List<IReadOnlyList<EventRecordWithMetadata>>();
      traces.AddRange(collection);

      tracesByMethods[asyncMethodName] = traces;
    }
  }

  private void SplitEventsByThreads(
    CollectedEvents events,
    out Dictionary<long, IEventsCollection> eventsByThreads,
    out IEventsCollection undefinedThreadEvents)
  {
    eventsByThreads = SplitEventsHelper.SplitByKey(logger, events.Events, SplitEventsHelper.ManagedThreadIdExtractor);
    undefinedThreadEvents = eventsByThreads[-1];
    eventsByThreads.Remove(-1);
  }

  private void ProcessManagedThreadEvents(IEventsCollection threadEvents, SessionGlobalData globalData)
  {
    using var _ = new PerformanceCookie($"{GetType().Name}::{nameof(ProcessManagedThreadEvents)}", logger);
    unitedEventsProcessor.ApplyMultipleMutators(threadEvents, globalData, EmptyCollections<Type>.EmptySet);
  }

  private IEventsCollection MergeUndefinedThreadEvents(IEventsCollection managedThreadEvents, IEventsCollection undefinedThreadEvents)
  {
    using var __ = new PerformanceCookie($"{GetType().Name}::{nameof(MergeUndefinedThreadEvents)}", logger);
    return undefinedThreadsEventsMerger.Merge(managedThreadEvents, undefinedThreadEvents);
  }
}