using Procfiler.Core.EventRecord;

namespace Procfiler.Core.EventsCollection;

public class EventPointersManager
{
  private readonly InsertedEvents myInsertedEvents;
  private readonly IEventsOwner myOwner;
  private readonly long myInitialCount;

  
  public EventPointersManager(long initialCount, InsertedEvents insertedEvents, IEventsOwner owner)
  {
    myInsertedEvents = insertedEvents;
    myOwner = owner;
    myInitialCount = initialCount;
  }

  
  public EventPointer? First
  {
    get
    {
      EventPointer? startPointer = myInsertedEvents.FirstEvents switch
      {
        { } => EventPointer.ForFirstEvent(0, myOwner),
        _ => EventPointer.ForInitialArray(0, myOwner)
      };

      return startPointer;
    }
  }

  public bool Remove(EventPointer pointer)
  {
    Debug.Assert(!pointer.IsInInitialArray);
    Debug.Assert(pointer.IsInFirstEvents || pointer.IsInInsertedMap);

    EventRecordWithMetadata eventToRemove;
    if (pointer.IsInFirstEvents)
    {
      Debug.Assert(myInsertedEvents.FirstEvents is { });
      eventToRemove = myInsertedEvents.FirstEvents[pointer.IndexInInsertionMap];
    }
    else
    {
      eventToRemove = GetInsertedEventsListOrThrow(pointer)[pointer.IndexInInsertionMap];
    }

    if (eventToRemove.IsRemoved)
    {
      return false;
    }

    eventToRemove.IsRemoved = true;
    return true;
  }

  public EventRecordWithMetadata? GetFor(EventPointer pointer)
  {
    AssertStateOrThrow(pointer);
    return pointer.IsInInsertedMap switch
    {
      true => pointer.IsInFirstEvents switch
      {
        true => myInsertedEvents.FirstEvents![pointer.IndexInInsertionMap],
        false => myInsertedEvents.GetOrThrow(pointer)
      },
      _ => null
    };
  }
  
  public EventPointer? Next(in EventPointer current)
  {
    AssertStateOrThrow(current);
    return NextInternal(current);
  }

  private EventPointer? NextInternal(in EventPointer current)
  {
    if (current.IsInInsertedMap)
    {
      if (current.IsInFirstEvents)
      {
        Debug.Assert(myInsertedEvents.FirstEvents is { });
        if (current.IndexInInsertionMap == myInsertedEvents.FirstEvents.Count - 1)
        {
          if (myInitialCount == 0) return null;
          return EventPointer.ForInitialArray(0, myOwner);
        }

        return EventPointer.ForFirstEvent(current.IndexInInsertionMap + 1, myOwner);
      }

      var insertedEventsList = GetInsertedEventsListOrThrow(current);
      if (current.IndexInInsertionMap == insertedEventsList.Count - 1)
      {
        var nextInitialArrayIndex = current.IndexInArray + 1;
        if (nextInitialArrayIndex >= myInitialCount) return null;

        return EventPointer.ForInitialArray(nextInitialArrayIndex, myOwner);
      }

      return EventPointer.ForInsertionMap(current.IndexInArray, current.IndexInInsertionMap + 1, myOwner);
    }
    
    if (myInsertedEvents.ContainsKey(current.IndexInArray))
    {
      return EventPointer.ForInsertionMap(current.IndexInArray, 0, myOwner);
    }

    var nextArrayIndex = current.IndexInArray + 1;
    if (nextArrayIndex >= myInitialCount) return null;
    return EventPointer.ForInitialArray(nextArrayIndex, myOwner);
  }

  public EventPointer? PrevInternal(in EventPointer current)
  {
    if (current.IsInInsertedMap)
    {
      if (current.IsInFirstEvents)
      {
        Debug.Assert(myInsertedEvents.FirstEvents is { });
        if (current.IndexInInsertionMap == 0) return null;
        return EventPointer.ForFirstEvent(current.IndexInInsertionMap - 1, myOwner);
      }

      if (current.IndexInInsertionMap == 0)
      {
        return EventPointer.ForInitialArray(current.IndexInArray, myOwner);
      }

      return EventPointer.ForInsertionMap(current.IndexInArray, current.IndexInInsertionMap - 1, myOwner);
    }

    if (current.IndexInArray == 0)
    {
      if (myInsertedEvents.FirstEvents is null || myInsertedEvents.FirstEvents.Count == 0) return null;
      return EventPointer.ForFirstEvent(myInsertedEvents.FirstEvents.Count - 1, myOwner);
    }
    
    if (myInsertedEvents[current.IndexInArray - 1] is { } insertionList)
    {
      return EventPointer.ForInsertionMap(current.IndexInArray - 1, insertionList.Count - 1, myOwner);
    }
    
    return EventPointer.ForInitialArray(current.IndexInArray - 1, myOwner);
  }

  private List<EventRecordWithMetadata> GetInsertedEventsListOrThrow(EventPointer pointer)
  {
    Debug.Assert(pointer.IsInInsertedMap);
    if (pointer.IsInFirstEvents)
    {
      Debug.Assert(myInsertedEvents.FirstEvents is { });
      Debug.Assert(pointer.IndexInInsertionMap >= 0 && 
                   pointer.IndexInInsertionMap < myInsertedEvents.FirstEvents.Count);
      
      return myInsertedEvents.FirstEvents;
    }

    Debug.Assert(pointer.IndexInArray >= 0 && pointer.IndexInArray < myInitialCount);
    Debug.Assert(myInsertedEvents.ContainsKey(pointer.IndexInArray));
    return myInsertedEvents[pointer.IndexInArray]!;
  }
  
  [Conditional("DEBUG")]
  public void AssertStateOrThrow(EventPointer pointer)
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
        Debug.Assert(pointer.IndexInArray >= 0 && pointer.IndexInArray < myInitialCount);
        Debug.Assert(pointer.IndexInInsertionMap >= 0);
      }
    }
    else
    {
      Debug.Assert(pointer.IsInInitialArray);
      Debug.Assert(pointer.IndexInArray >= 0 && pointer.IndexInArray < myInitialCount);
      Debug.Assert(pointer.IndexInInsertionMap == -1);
    }
  }
}