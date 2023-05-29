using JetBrains.Lifetimes;
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
    Lifetime lifetime,
    string filterPattern,
    bool inlineInnerCalls,
    bool mergeUndefinedThreadEvents,
    bool addAsyncMethods);
}

[AppComponent]
public class ByMethodsSplitterImpl : IByMethodsSplitter
{
  private readonly IManagedEventsFromUndefinedThreadExtractor myManagedEventsExtractor;
  private readonly IEventsCollectionByMethodsSplitter mySplitter;
  private readonly IProcfilerLogger myLogger;
  private readonly IAsyncMethodsGrouper myAsyncMethodsGrouper;
  private readonly IUnitedEventsProcessor myUnitedEventsProcessor;
  private readonly IUndefinedThreadsEventsMerger myUndefinedThreadsEventsMerger;

  
  public ByMethodsSplitterImpl(
    IProcfilerLogger logger,
    IEventsCollectionByMethodsSplitter splitter, 
    IManagedEventsFromUndefinedThreadExtractor managedEventsExtractor, 
    IAsyncMethodsGrouper asyncMethodsGrouper, 
    IUnitedEventsProcessor unitedEventsProcessor, 
    IUndefinedThreadsEventsMerger undefinedThreadsEventsMerger)
  {
    mySplitter = splitter;
    myManagedEventsExtractor = managedEventsExtractor;
    myAsyncMethodsGrouper = asyncMethodsGrouper;
    myUnitedEventsProcessor = unitedEventsProcessor;
    myUndefinedThreadsEventsMerger = undefinedThreadsEventsMerger;
    myLogger = logger;
  }

  
  public Dictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>> Split(
    CollectedEvents events,
    Lifetime lifetime,
    string filterPattern,
    bool inlineInnerCalls,
    bool mergeUndefinedThreadEvents,
    bool addAsyncMethods)
  {
    SplitEventsByThreads(events, out var eventsByManagedThreads, out var undefinedThreadEvents);
    undefinedThreadEvents = myManagedEventsExtractor.Extract(eventsByManagedThreads, undefinedThreadEvents);

    var tracesByMethods = new Dictionary<string, List<IReadOnlyList<EventRecordWithMetadata>>>();
    foreach (var (key, threadEvents) in eventsByManagedThreads)
    {
      using var _ = new PerformanceCookie($"{GetType().Name}::{nameof(Split)}::PreparingTrace_{key}", myLogger);

      ProcessManagedThreadEvents(threadEvents, events.GlobalData);
      lifetime.AddDispose(threadEvents);
      
      var mergedEvents = MergeUndefinedThreadEvents(mergeUndefinedThreadEvents, threadEvents, undefinedThreadEvents);
      var eventsTracesByMethods = mySplitter.Split(mergedEvents, filterPattern, inlineInnerCalls);

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
    var asyncMethodsTraces = myAsyncMethodsGrouper.GroupAsyncMethods(tracesByMethods.Keys, eventsByManagedThreads);
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
    eventsByThreads = SplitEventsHelper.SplitByKey(myLogger, events.Events, SplitEventsHelper.ManagedThreadIdExtractor);
    undefinedThreadEvents = eventsByThreads[-1];
    eventsByThreads.Remove(-1);
  }

  private void ProcessManagedThreadEvents(IEventsCollection threadEvents, SessionGlobalData globalData)
  {
    using var _ = new PerformanceCookie($"{GetType().Name}::{nameof(ProcessManagedThreadEvents)}", myLogger);
    myUnitedEventsProcessor.ApplyMultipleMutators(threadEvents, globalData, EmptyCollections<Type>.EmptySet);
  }

  private IEventsCollection MergeUndefinedThreadEvents(
    bool mergeUndefinedThreadEvents,
    IEventsCollection managedThreadEvents,
    IEventsCollection undefinedThreadEvents)
  {
    if (!mergeUndefinedThreadEvents) return managedThreadEvents;

    using var __ = new PerformanceCookie($"{GetType().Name}::{nameof(MergeUndefinedThreadEvents)}", myLogger);
    return myUndefinedThreadsEventsMerger.Merge(managedThreadEvents, undefinedThreadEvents);
  }
}