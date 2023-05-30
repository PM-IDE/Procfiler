﻿using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using Procfiler.Core.EventRecord;
using Procfiler.Utils;

namespace Procfiler.Core.EventsCollection;

public class InsertedEvents
{
  private readonly Dictionary<int, List<EventRecordWithMetadata>> myInsertedEvents;
  private readonly Dictionary<int, HashSet<int>> myRemovedInsertedEvents;
  private readonly HashSet<int> myRemovedFromFirstEvents;

  public List<EventRecordWithMetadata>? FirstEvents { get; private set; }

  public long Count { get; private set; }
  

  public InsertedEvents()
  {
    myRemovedInsertedEvents = new Dictionary<int, HashSet<int>>();
    myRemovedFromFirstEvents = new HashSet<int>();
    myInsertedEvents = new Dictionary<int, List<EventRecordWithMetadata>>();
  }


  public EventRecordWithMetadata GetOrThrow(EventPointer pointer)
  {
    return myInsertedEvents[pointer.IndexInArray][pointer.IndexInInsertionMap];
  }

  public List<EventRecordWithMetadata>? this[int index]
  {
    get => myInsertedEvents.TryGetValue(index, out var insertedEvents) ? insertedEvents : null;
    set => myInsertedEvents[index] = value ?? throw new ArgumentNullException(nameof(value));
  }

  public bool ContainsKey(int index) => myInsertedEvents.ContainsKey(index);
  
  private List<EventRecordWithMetadata> GetOrCreateInsertedEventsList(EventPointer pointer)
  {
    if (pointer.IsInFirstEvents)
    {
      return FirstEvents ??= new List<EventRecordWithMetadata>();
    }
    
    return GetOrCreateInsertionList(pointer.IndexInArray);
  }

  public void InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    if (pointer.IsInInsertedMap)
    {
      GetOrCreateInsertedEventsList(pointer).Insert(pointer.IndexInInsertionMap + 1, eventToInsert);
    }
    else if (pointer.IsInInitialArray)
    {
      Debug.Assert(!myInsertedEvents.ContainsKey(pointer.IndexInArray));
      var insertedEvents = new List<EventRecordWithMetadata> { eventToInsert };
      myInsertedEvents[pointer.IndexInArray] = insertedEvents;
    }

    IncreaseCount();
  }

  private void IncreaseCount() => ++Count;
  private void DecreaseCount() => --Count;

  public void InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    if (pointer.IsInInsertedMap)
    {
      GetOrCreateInsertedEventsList(pointer).Insert(pointer.IndexInInsertionMap, eventToInsert);
    }
    else if (pointer.IsInInitialArray)
    {
      if (pointer.IndexInArray == 0)
      {
        FirstEvents ??= new List<EventRecordWithMetadata>();
        FirstEvents.Add(eventToInsert);
      }
      else
      {
        GetOrCreateInsertionList(pointer.IndexInArray - 1).Add(eventToInsert);
      }
    }

    IncreaseCount();
  }
  
  private List<EventRecordWithMetadata> GetOrCreateInsertionList(int index)
  {
    if (myInsertedEvents.TryGetValue(index, out var events)) return events;

    var newList = new List<EventRecordWithMetadata>();
    myInsertedEvents[index] = newList;
    return newList;
  }

  public bool Remove(EventPointer pointer)
  {
    Debug.Assert(!pointer.IsInInitialArray);
    Debug.Assert(pointer.IsInFirstEvents || pointer.IsInInsertedMap);

    if (pointer.IsInFirstEvents)
    {
      if (myRemovedFromFirstEvents.Contains(pointer.IndexInInsertionMap)) return false;

      myRemovedFromFirstEvents.Add(pointer.IndexInInsertionMap);
      DecreaseCount();
      return true;
    }

    var removed = myRemovedInsertedEvents.GetOrCreate(pointer.IndexInArray, static () => new HashSet<int>());
    if (removed.Contains(pointer.IndexInInsertionMap))
    {
      return false;
    }

    removed.Add(pointer.IndexInInsertionMap);
    DecreaseCount();
    return true;
  }

  public bool IsRemoved(EventPointer pointer)
  {
    Debug.Assert(!pointer.IsInInitialArray);
    Debug.Assert(pointer.IsInFirstEvents || pointer.IsInInsertedMap);

    if (pointer.IsInFirstEvents)
    {
      return myRemovedFromFirstEvents.Contains(pointer.IndexInInsertionMap);
    }

    var removed = DictionaryExtensions.GetValueOrDefault(myRemovedInsertedEvents, pointer.IndexInArray);
    return removed?.Contains(pointer.IndexInInsertionMap) ?? false;
  }
}