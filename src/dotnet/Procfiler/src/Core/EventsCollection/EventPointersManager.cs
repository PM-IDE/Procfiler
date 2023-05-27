using Procfiler.Core.EventRecord;

namespace Procfiler.Core.EventsCollection;

public class EventPointersManager
{
  private readonly InsertedEvents myInsertedEvents;
  private readonly int myInitialCount;

  
  public EventPointersManager(int initialCount, InsertedEvents insertedEvents)
  {
    myInsertedEvents = insertedEvents;
    myInitialCount = initialCount;
  }


  public EventPointer? NextInternal(in EventPointer current)
  {
    if (current.IsInInsertedMap)
    {
      if (current.IsInFirstEvents)
      {
        Debug.Assert(myInsertedEvents.FirstEvents is { });
        if (current.IndexInInsertionMap == myInsertedEvents.FirstEvents.Count - 1)
        {
          if (myInitialCount == 0) return null;
          return EventPointer.ForInitialArray(0);
        }

        return EventPointer.ForFirstEvent(current.IndexInInsertionMap + 1);
      }

      var insertedEventsList = GetInsertedEventsListOrThrow(current);
      if (current.IndexInInsertionMap == insertedEventsList.Count - 1)
      {
        var nextInitialArrayIndex = current.IndexInArray + 1;
        if (nextInitialArrayIndex >= myInitialCount) return null;

        return EventPointer.ForInitialArray(nextInitialArrayIndex);
      }

      return EventPointer.ForInsertionMap(current.IndexInArray, current.IndexInInsertionMap + 1);
    }
    
    if (myInsertedEvents.ContainsKey(current.IndexInArray))
    {
      return EventPointer.ForInsertionMap(current.IndexInArray, 0);
    }

    var nextArrayIndex = current.IndexInArray + 1;
    if (nextArrayIndex >= myInitialCount) return null;
    return EventPointer.ForInitialArray(nextArrayIndex);
  }

  public EventPointer? PrevInternal(in EventPointer current)
  {
    if (current.IsInInsertedMap)
    {
      if (current.IsInFirstEvents)
      {
        Debug.Assert(myInsertedEvents.FirstEvents is { });
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
      if (myInsertedEvents.FirstEvents is null || myInsertedEvents.FirstEvents.Count == 0) return null;
      return EventPointer.ForFirstEvent(myInsertedEvents.FirstEvents.Count - 1);
    }
    
    if (myInsertedEvents[current.IndexInArray - 1] is { } insertionList)
    {
      return EventPointer.ForInsertionMap(current.IndexInArray - 1, insertionList.Count - 1);
    }
    
    return EventPointer.ForInitialArray(current.IndexInArray - 1);
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