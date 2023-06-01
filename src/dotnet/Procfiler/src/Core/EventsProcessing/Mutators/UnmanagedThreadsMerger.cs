using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators;

public interface IUndefinedThreadsEventsMerger
{
  [Pure]
  IEventsCollection Merge(IEventsCollection managedThreadEvents, IEventsCollection undefinedThreadEvents);

  [Pure]
  IEnumerable<EventRecordWithMetadata> MergeLazy(
    IEventsCollection managedThreadEvents, IEventsCollection undefinedThreadEvents);
}

[AppComponent]
public class UndefinedThreadsEventsMerger : IUndefinedThreadsEventsMerger
{
  private readonly IProcfilerLogger myLogger;


  public UndefinedThreadsEventsMerger(IProcfilerLogger logger)
  {
    myLogger = logger;
  }
  
  
  [Pure]
  public IEventsCollection Merge(
    IEventsCollection managedThreadEvents, IEventsCollection undefinedThreadEvents)
  {
    if (undefinedThreadEvents.Count == 0) return managedThreadEvents;
    
    var mergedArray = new EventRecordWithMetadata[managedThreadEvents.Count + undefinedThreadEvents.Count];
    var index = 0;
    foreach (var eventRecord in MergeLazyInternal(managedThreadEvents, undefinedThreadEvents))
    {
      mergedArray[index++] = eventRecord;
    }

    Array.Resize(ref mergedArray, index);
    return new EventsCollectionImpl(mergedArray, myLogger);
  }

  public IEnumerable<EventRecordWithMetadata> MergeLazy(
    IEventsCollection managedThreadEvents, IEventsCollection undefinedThreadEvents)
  {
    managedThreadEvents.Freeze();
    undefinedThreadEvents.Freeze();
    return MergeLazyInternal(managedThreadEvents, undefinedThreadEvents);
  }

  private IEnumerable<EventRecordWithMetadata> MergeLazyInternal(
    IEventsCollection managedThreadEvents, IEventsCollection undefinedThreadEvents)
  {
    var managedFinished = false;
    var undefinedFinished = false;
    using var managedEnumerator = managedThreadEvents.GetEnumerator();
    using var undefinedEnumerator = undefinedThreadEvents.GetEnumerator();
    
    if (!managedEnumerator.MoveNext()) managedFinished = true;
    if (!undefinedEnumerator.MoveNext()) undefinedFinished = true;

    while (!managedFinished || !undefinedFinished)
    {
      if (managedFinished)
      {
        while (!undefinedFinished)
        {
          yield return undefinedEnumerator.Current.Event;
          if (!undefinedEnumerator.MoveNext())
          {
            undefinedFinished = true;
          }
        }

        break;
      }

      if (undefinedFinished)
      {
        while (!managedFinished)
        {
          yield return managedEnumerator.Current.Event;
          if (!managedEnumerator.MoveNext())
          {
            managedFinished = true;
          }
        }

        break;
      }

      var managedEvent = managedEnumerator.Current.Event;
      var undefinedEvent = undefinedEnumerator.Current.Event;

      if (managedEvent.Stamp < undefinedEvent.Stamp)
      {
        yield return managedEvent;
        if (!managedEnumerator.MoveNext())
        {
          managedFinished = true;
        }
      }
      else
      {
        yield return undefinedEvent;
        if (!undefinedEnumerator.MoveNext())
        {
          undefinedFinished = true;
        }
      }
    }
  }
}

public static class ExtensionsForIUndefinedThreadsEventsMerger
{
  public static void Merge<TKey>(
    this IUndefinedThreadsEventsMerger merger,
    IDictionary<TKey, IEventsCollection> eventsByKeys, 
    IEventsCollection undefinedThreadEvents)
  {
    foreach (var key in eventsByKeys.Keys)
    {
      eventsByKeys[key] = merger.Merge(eventsByKeys[key], undefinedThreadEvents);
    }
  }
}