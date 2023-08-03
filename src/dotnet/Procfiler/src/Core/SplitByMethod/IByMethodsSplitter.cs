using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.SplitByMethod;

public interface IByMethodsSplitter
{
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
      
      var mergedEvents = MergeUndefinedThreadEvents(mergeUndefinedThreadEvents, threadEvents, undefinedThreadEvents);
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
      AddAsyncMethods(tracesByMethods, eventsByManagedThreads);
    }

    return tracesByMethods;
  }

  private void AddAsyncMethods(
    IDictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>> tracesByMethods,
    IDictionary<long, IEventsCollection> eventsByManagedThreads)
  {
    var asyncMethodsTraces = asyncMethodsGrouper.GroupAsyncMethods(tracesByMethods.Keys, eventsByManagedThreads);
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

  private IEventsCollection MergeUndefinedThreadEvents(
    bool mergeUndefinedThreadEvents,
    IEventsCollection managedThreadEvents,
    IEventsCollection undefinedThreadEvents)
  {
    if (!mergeUndefinedThreadEvents) return managedThreadEvents;

    using var __ = new PerformanceCookie($"{GetType().Name}::{nameof(MergeUndefinedThreadEvents)}", logger);
    return undefinedThreadsEventsMerger.Merge(managedThreadEvents, undefinedThreadEvents);
  }
}