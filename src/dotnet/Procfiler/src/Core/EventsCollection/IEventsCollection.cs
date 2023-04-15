using System.Runtime.CompilerServices;
using Procfiler.Core.EventRecord;
using Procfiler.Core.Exceptions;
using Procfiler.Utils;

namespace Procfiler.Core.EventsCollection;

public interface IFreezableCollection
{
  void Freeze();
  void UnFreeze();
}

public class CollectionIsFrozenException : ProcfilerException
{
}

public interface IEventsCollection : IFreezableCollection, IEnumerable<EventRecordWithMetadata>
{
  int Count { get; }

  EventPointer? First { get; }
  EventPointer? Last { get; }
  
  EventRecordWithMetadata? TryGetForWithDeletionCheck(EventPointer pointer);
  EventRecordWithMetadata GetFor(EventPointer pointer);
  EventPointer? NextNotDeleted(in EventPointer current);
  EventPointer? PrevNotDeleted(in EventPointer current);
  
  void InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert);
  void InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert);
  void Remove(EventRecordWithMetadata eventRecord);
  void ApplyNotPureActionForAllEvents(Func<EventPointer, EventRecordWithMetadata, bool> action);
}

public class EventsCollectionImpl : IEventsCollection
{
  private readonly IProcfilerLogger myLogger;
  
  private readonly Dictionary<int, List<EventRecordWithMetadata>> myInsertedEvents;
  private readonly EventRecordWithMetadata[] myInitialEvents;

  private bool myIsFrozen;
  private List<EventRecordWithMetadata>? myFirstEvents;

  
  public int Count { get; private set; }

  
  public EventsCollectionImpl(EventRecordWithMetadata[] initialEvents, IProcfilerLogger logger)
  {
    if (initialEvents.Length == 0) throw new IndexOutOfRangeException();
    
    myInitialEvents = initialEvents;
    Count = myInitialEvents.Length;
    myLogger = logger;
    myInsertedEvents = new Dictionary<int, List<EventRecordWithMetadata>>();
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
    AssertStateOrThrow(pointer);
    return pointer.IsInInsertedMap switch
    {
      true => pointer.IsInFirstEvents switch
      {
        true => myFirstEvents![pointer.IndexInInsertionMap],
        false => myInsertedEvents[pointer.IndexInArray][pointer.IndexInInsertionMap]
      },
      _ => myInitialEvents[pointer.IndexInArray]
    };
  }
  
  public EventRecordWithMetadata? TryGetForWithDeletionCheck(EventPointer pointer)
  {
    var eventRecord = GetFor(pointer);
    return IsRemoved(eventRecord) ? null : eventRecord;
  }

  public EventPointer? First
  {
    get
    {
      EventPointer? startEvent = myFirstEvents switch
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
      EventPointer? startPtr = myInsertedEvents.TryGetValue(myInitialEvents.Length - 1, out var insertedEvents) 
        ? EventPointer.ForInsertionMap(myInitialEvents.Length - 1, insertedEvents.Count - 1) 
        : EventPointer.ForInitialArray(myInitialEvents.Length - 1);

      if (TryGetForWithDeletionCheck(startPtr.Value) is null)
      {
        startPtr = PrevNotDeleted(startPtr.Value);
      }

      return startPtr;
    }
  }

  public EventPointer? NextNotDeleted(in EventPointer current)
  {
    AssertStateOrThrow(current);
    var result = NextInternal(current);
    while (result is { } && TryGetForWithDeletionCheck(result.Value) is null)
    {
      result = NextInternal(result.Value);
    }

    return result;
  }

  private EventPointer? NextInternal(in EventPointer current)
  {
    if (current.IsInInsertedMap)
    {
      if (current.IsInFirstEvents)
      {
        Debug.Assert(myFirstEvents is { });
        if (current.IndexInInsertionMap == myFirstEvents.Count - 1)
        {
          if (myInitialEvents.Length == 0) return null;
          return EventPointer.ForInitialArray(0);
        }

        return EventPointer.ForFirstEvent(current.IndexInInsertionMap + 1);
      }

      var insertedEventsList = GetInsertedEventsListOrThrow(current);
      if (current.IndexInInsertionMap == insertedEventsList.Count - 1)
      {
        var nextInitialArrayIndex = current.IndexInArray + 1;
        if (nextInitialArrayIndex >= myInitialEvents.Length) return null;

        return EventPointer.ForInitialArray(nextInitialArrayIndex);
      }

      return EventPointer.ForInsertionMap(current.IndexInArray, current.IndexInInsertionMap + 1);
    }
    
    if (myInsertedEvents.ContainsKey(current.IndexInArray))
    {
      return EventPointer.ForInsertionMap(current.IndexInArray, 0);
    }

    var nextArrayIndex = current.IndexInArray + 1;
    if (nextArrayIndex >= myInitialEvents.Length) return null;
    return EventPointer.ForInitialArray(nextArrayIndex);
  }

  public EventPointer? PrevNotDeleted(in EventPointer current)
  {
    AssertStateOrThrow(current);
    var result = PrevInternal(current);
    while (result is { } && TryGetForWithDeletionCheck(result.Value) is null)
    {
      result = PrevInternal(result.Value);
    }

    return result;
  }

  private EventPointer? PrevInternal(in EventPointer current)
  {
    if (current.IsInInsertedMap)
    {
      if (current.IsInFirstEvents)
      {
        Debug.Assert(myFirstEvents is { });
        if (current.IndexInInsertionMap == 0) return null;
        return EventPointer.ForFirstEvent(current.IndexInInsertionMap - 1);
      }

      if (current.IndexInInsertionMap == 0)
      {
        return EventPointer.ForInitialArray(current.IndexInArray);
      }

      return EventPointer.ForInsertionMap(current.IndexInArray, current.IndexInInsertionMap - 1);
    }

    if (current.IndexInArray == 0)
    {
      if (myFirstEvents is null || myFirstEvents.Count == 0) return null;
      return EventPointer.ForFirstEvent(myFirstEvents.Count - 1);
    }
    
    if (myInsertedEvents.TryGetValue(current.IndexInArray - 1, out var insertionList))
    {
      return EventPointer.ForInsertionMap(current.IndexInArray - 1, insertionList.Count - 1);
    }
    
    return EventPointer.ForInitialArray(current.IndexInArray - 1);
  }

  public void InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    AssertStateOrThrow(pointer);
    AssertNotFrozen();
    
    if (pointer.IsInInsertedMap)
    {
      var insertedList = GetOrCreateInsertedEventsList(pointer);
      insertedList.Insert(pointer.IndexInInsertionMap + 1, eventToInsert);
    }
    else if (pointer.IsInInitialArray)
    {
      Debug.Assert(!myInsertedEvents.ContainsKey(pointer.IndexInArray));
      var insertedEvents = new List<EventRecordWithMetadata> { eventToInsert };
      myInsertedEvents[pointer.IndexInArray] = insertedEvents;
    }

    IncreaseCount();
  }

  private List<EventRecordWithMetadata> GetOrCreateInsertedEventsList(EventPointer pointer)
  {
    if (pointer.IsInFirstEvents)
    {
      return myFirstEvents ??= new List<EventRecordWithMetadata>();
    }
    
    return myInsertedEvents.GetOrCreate(pointer.IndexInArray, static () => new List<EventRecordWithMetadata>());
  }

  private List<EventRecordWithMetadata> GetInsertedEventsListOrThrow(EventPointer pointer)
  {
    Debug.Assert(pointer.IsInInsertedMap);
    if (pointer.IsInFirstEvents)
    {
      Debug.Assert(myFirstEvents is { });
      Debug.Assert(pointer.IndexInInsertionMap >= 0 && pointer.IndexInInsertionMap < myFirstEvents.Count);
      return myFirstEvents;
    }

    Debug.Assert(pointer.IndexInArray >= 0 && pointer.IndexInArray < myInitialEvents.Length);
    Debug.Assert(myInsertedEvents.ContainsKey(pointer.IndexInArray));
    return myInsertedEvents[pointer.IndexInArray];
  }
  
  [Conditional("DEBUG")]
  private void AssertStateOrThrow(EventPointer pointer)
  {
    pointer.AssertStateOrThrow();
    if (pointer.IsInInsertedMap)
    {
      if (pointer.IsInFirstEvents)
      {
        Debug.Assert(pointer.IndexInArray == -1);
      }
      else
      {
        Debug.Assert(pointer.IndexInArray >= 0 && pointer.IndexInArray < myInitialEvents.Length);
        Debug.Assert(pointer.IndexInInsertionMap >= 0);
      }
    }
    else
    {
      Debug.Assert(pointer.IsInInitialArray);
      Debug.Assert(pointer.IndexInArray >= 0 && pointer.IndexInArray < myInitialEvents.Length);
      Debug.Assert(pointer.IndexInInsertionMap == -1);
    }
  }

  public void InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    AssertStateOrThrow(pointer);
    AssertNotFrozen();

    if (pointer.IsInInsertedMap)
    {
      var insertedList = GetOrCreateInsertedEventsList(pointer);
      insertedList.Insert(pointer.IndexInInsertionMap, eventToInsert);
    }
    else if (pointer.IsInInitialArray)
    {
      if (pointer.IndexInArray == 0)
      {
        myFirstEvents ??= new List<EventRecordWithMetadata>();
        myFirstEvents.Add(eventToInsert);
      }
      else
      {
        var list = myInsertedEvents.GetOrCreate(pointer.IndexInArray - 1, static () => new List<EventRecordWithMetadata>());
        list.Add(eventToInsert);
      }
    }
    
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