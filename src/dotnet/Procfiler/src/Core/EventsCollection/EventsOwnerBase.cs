using Procfiler.Core.EventRecord;

namespace Procfiler.Core.EventsCollection;

public abstract class EventsOwnerBase : IEventsOwner, IEnumerable<EventRecordWithPointer>
{
  private readonly InsertedEvents myInsertedEvents;
  protected readonly EventPointersManager PointersManager;

  private bool myIsFrozen;
  
  
  public long Count { get; private set; }
  
  
  protected EventsOwnerBase(long initialEventsCount)
  {
    myInsertedEvents = new InsertedEvents();
    PointersManager = new EventPointersManager(initialEventsCount, myInsertedEvents, this);
    Count = initialEventsCount;
  }

  
  public virtual IEnumerator<EventRecordWithPointer> GetEnumerator() => EnumerateInternal().GetEnumerator();

  protected IEnumerable<EventRecordWithPointer> EnumerateInternal()
  {
    using var initialEventsEnumerator = EnumerateInitialEvents().GetEnumerator();
    initialEventsEnumerator.MoveNext();
    var currentIndex = 0;
    
    var current = PointersManager.First;
    while (current is { })
    {
      if (PointersManager.GetFor(current.Value) is { } insertedEvent)
      {
        if (!insertedEvent.IsRemoved)
        {
          yield return new EventRecordWithPointer
          {
            Event = insertedEvent,
            EventPointer = current.Value
          };
        }
      }
      else
      {
        Debug.Assert(current.Value.IsInInitialArray);
        while (currentIndex != current.Value.IndexInArray)
        {
          initialEventsEnumerator.MoveNext();
          ++currentIndex;
        }

        if (!initialEventsEnumerator.Current.IsRemoved)
        {
          yield return new EventRecordWithPointer
          {
            Event = initialEventsEnumerator.Current,
            EventPointer = current.Value
          }; 
        }
      }

      current = PointersManager.Next(current.Value);
    }
  }
  
  public virtual EventPointer InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    PointersManager.AssertStateOrThrow(pointer);
    AssertNotFrozen();
    myInsertedEvents.InsertAfter(pointer, eventToInsert);
    IncreaseCount();
    
    var insertedEventPointer = PointersManager.Next(pointer);
    Debug.Assert(insertedEventPointer is { });
    return insertedEventPointer.Value;
  }
  
  public virtual EventPointer InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    PointersManager.AssertStateOrThrow(pointer);
    AssertNotFrozen();
    myInsertedEvents.InsertBefore(pointer, eventToInsert);
    IncreaseCount();
    
    var insertedEventPointer = PointersManager.PrevInternal(pointer);
    Debug.Assert(insertedEventPointer is { });
    return insertedEventPointer.Value;
  }

  public abstract bool Remove(EventPointer pointer);

  protected void IncreaseCount() => ++Count;
  protected void DecreaseCount() => --Count;
  
  public void Freeze() => myIsFrozen = true;
  public void UnFreeze() => myIsFrozen = false;

  protected void AssertNotFrozen()
  {
    if (myIsFrozen) throw new CollectionIsFrozenException();
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  
  protected abstract IEnumerable<EventRecordWithMetadata> EnumerateInitialEvents();
}