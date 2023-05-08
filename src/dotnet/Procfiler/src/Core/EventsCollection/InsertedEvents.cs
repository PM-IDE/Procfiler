using Procfiler.Core.EventRecord;

namespace Procfiler.Core.EventsCollection;

public class InsertedEvents
{
  private readonly Dictionary<int, List<EventRecordWithMetadata>> myInsertedEvents;


  public List<EventRecordWithMetadata>? FirstEvents { get; private set; }


  public InsertedEvents()
  {
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
  }

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
  }
  
  private List<EventRecordWithMetadata> GetOrCreateInsertionList(int index)
  {
    if (myInsertedEvents.TryGetValue(index, out var events)) return events;

    var newList = new List<EventRecordWithMetadata>();
    myInsertedEvents[index] = newList;
    return newList;
  }
}