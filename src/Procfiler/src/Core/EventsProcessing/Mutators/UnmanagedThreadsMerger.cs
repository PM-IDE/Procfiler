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
    var managedCurrent = managedThreadEvents.First;
    var undefinedCurrent = undefinedThreadEvents.First;

    while (managedCurrent is { } || undefinedCurrent is { })
    {
      if (managedCurrent is null)
      {
        while (undefinedCurrent is { })
        {
          yield return undefinedThreadEvents.GetFor(undefinedCurrent.Value);
          undefinedCurrent = undefinedThreadEvents.NextNotDeleted(undefinedCurrent.Value);
        }

        break;
      }

      if (undefinedCurrent is null)
      {
        while (managedCurrent is { })
        {
          yield return managedThreadEvents.GetFor(managedCurrent.Value);
          managedCurrent = managedThreadEvents.NextNotDeleted(managedCurrent.Value);
        }

        break;
      }

      var managedEvent = managedThreadEvents.GetFor(managedCurrent.Value);
      var undefinedEvent = undefinedThreadEvents.GetFor(undefinedCurrent.Value);

      if (managedEvent.Stamp < undefinedEvent.Stamp)
      {
        yield return managedEvent;
        managedCurrent = managedThreadEvents.NextNotDeleted(managedCurrent.Value);
      }
      else
      {
        yield return undefinedEvent;
        undefinedCurrent = undefinedThreadEvents.NextNotDeleted(undefinedCurrent.Value);
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