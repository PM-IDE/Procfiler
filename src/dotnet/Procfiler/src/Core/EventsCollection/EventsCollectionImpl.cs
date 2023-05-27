using System.Runtime.CompilerServices;
using Procfiler.Core.EventRecord;
using Procfiler.Utils;

namespace Procfiler.Core.EventsCollection;

public class EventsCollectionImpl : IEventsCollection
{
  private readonly IProcfilerLogger myLogger;
  private readonly EventRecordWithMetadata[] myInitialEvents;
  private readonly InsertedEvents myInsertedEvents;
  private readonly EventPointersManager myPointersManager;


  private bool myIsFrozen;

  
  public int Count { get; private set; }

  
  public EventsCollectionImpl(EventRecordWithMetadata[] initialEvents, IProcfilerLogger logger)
  {
    if (initialEvents.Length == 0) throw new IndexOutOfRangeException();

    myInsertedEvents = new InsertedEvents();
    myPointersManager = new EventPointersManager(initialEvents.Length, myInsertedEvents);
    myInitialEvents = initialEvents;
    Count = myInitialEvents.Length;
    myLogger = logger;
  }


  public EventPointer? First
  {
    get
    {
      EventPointer? startEvent = myInsertedEvents.FirstEvents switch
      {
        { } => EventPointer.ForFirstEvent(0),
        _ => EventPointer.ForInitialArray(0)
      };
      
      if (TryGetForWithDeletionCheck(startEvent.Value) is null)
      {
        startEvent = NextNotDeleted(startEvent.Value);
      }

      return startEvent;
    }
  }

  public EventPointer? Last
  {
    get
    {
      EventPointer? startPtr = myInsertedEvents[myInitialEvents.Length - 1] is { } insertedEvents 
        ? EventPointer.ForInsertionMap(myInitialEvents.Length - 1, insertedEvents.Count - 1) 
        : EventPointer.ForInitialArray(myInitialEvents.Length - 1);

      if (TryGetForWithDeletionCheck(startPtr.Value) is null)
      {
        startPtr = PrevNotDeleted(startPtr.Value);
      }

      return startPtr;
    }
  }
  
  public void Freeze()
  {
    myIsFrozen = true;
  }

  public void UnFreeze()
  {
    myIsFrozen = false;
  }

  private void AssertNotFrozen()
  {
    if (myIsFrozen) throw new CollectionIsFrozenException();
  }
  
  public EventRecordWithMetadata GetFor(EventPointer pointer)
  {
    myPointersManager.AssertStateOrThrow(pointer);
    return pointer.IsInInsertedMap switch
    {
      true => pointer.IsInFirstEvents switch
      {
        true => myInsertedEvents.FirstEvents![pointer.IndexInInsertionMap],
        false => myInsertedEvents.GetOrThrow(pointer)
      },
      _ => myInitialEvents[pointer.IndexInArray]
    };
  }
  
  public EventRecordWithMetadata? TryGetForWithDeletionCheck(EventPointer pointer)
  {
    var eventRecord = GetFor(pointer);
    return IsRemoved(eventRecord) ? null : eventRecord;
  }

  public EventPointer? NextNotDeleted(in EventPointer current)
  {
    myPointersManager.AssertStateOrThrow(current);
    var result = myPointersManager.NextInternal(current);
    while (result is { } && TryGetForWithDeletionCheck(result.Value) is null)
    {
      result = myPointersManager.NextInternal(result.Value);
    }

    return result;
  }

  public EventPointer? PrevNotDeleted(in EventPointer current)
  {
    myPointersManager.AssertStateOrThrow(current);
    var result = myPointersManager.PrevInternal(current);
    while (result is { } && TryGetForWithDeletionCheck(result.Value) is null)
    {
      result = myPointersManager.PrevInternal(result.Value);
    }

    return result;
  }
  
  public void InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    myPointersManager.AssertStateOrThrow(pointer);
    AssertNotFrozen();
    myInsertedEvents.InsertAfter(pointer, eventToInsert);
    IncreaseCount();
  }
  
  public void InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    myPointersManager.AssertStateOrThrow(pointer);
    AssertNotFrozen();
    myInsertedEvents.InsertBefore(pointer, eventToInsert);
    IncreaseCount();
  }

  private void IncreaseCount() => ++Count;
  private void DecreaseCount() => --Count;
  
  public void Remove(EventRecordWithMetadata eventRecord)
  {
    AssertNotFrozen();
    eventRecord.IsDeleted = true;
    DecreaseCount();
  }

  public void ApplyNotPureActionForAllEvents(Func<EventPointer, EventRecordWithMetadata, bool> action)
  {
    foreach (var (eventRecord, pointer) in GetEnumeratorInternal())
    {
      var shouldStop = action(pointer, eventRecord);
      if (shouldStop) return;
    }
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static bool IsRemoved(EventRecordWithMetadata eventRecord) => eventRecord.IsDeleted;

  public IEnumerator<EventRecordWithMetadata> GetEnumerator()
  {
    foreach (var (eventRecord, _) in GetEnumeratorInternal())
    {
      yield return eventRecord;
    }
  }

  private readonly record struct EventWithPointer(EventRecordWithMetadata Event, EventPointer Pointer);
  
  private IEnumerable<EventWithPointer> GetEnumeratorInternal()
  {
    var current = First;
    while (current != null)
    {
      if (TryGetForWithDeletionCheck(current.Value) is { } eventRecord)
      {
        yield return new EventWithPointer(eventRecord, current.Value);
      }

      current = NextNotDeleted(current.Value);
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}