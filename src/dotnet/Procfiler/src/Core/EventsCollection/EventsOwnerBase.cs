using Procfiler.Core.EventRecord;

namespace Procfiler.Core.EventsCollection;

public abstract class EventsOwnerBase : IEventsOwner, IEnumerable<EventRecordWithPointer>
{
  private readonly InsertedEvents myInsertedEvents;
  private readonly EventPointersManager myPointersManager;

  private bool myIsFrozen;
  
  
  public long Count { get; private set; }
  
  
  protected EventsOwnerBase(long initialEventsCount)
  {
    myInsertedEvents = new InsertedEvents();
    myPointersManager = new EventPointersManager(initialEventsCount, myInsertedEvents, this);
    Count = initialEventsCount;
  }

  
  protected IEnumerable<EventRecordWithPointer> GetEnumeratorInternal()
  {
    using var initialEventsEnumerator = EnumerateInitialEvents().GetEnumerator();
    initialEventsEnumerator.MoveNext();
    var currentIndex = 0;
    
    var current = myPointersManager.FirstNotDeleted;
    while (current is { })
    {
      if (myPointersManager.GetFor(current.Value) is { } insertedEvent)
      {
        yield return new EventRecordWithPointer
        {
          Event = insertedEvent,
          EventPointer = current.Value
        };
      }
      else
      {
        Debug.Assert(current.Value.IsInInitialArray);
        while (currentIndex != current.Value.IndexInArray)
        {
          initialEventsEnumerator.MoveNext();
          ++currentIndex;
        }
        
        yield return new EventRecordWithPointer
        {
          Event = initialEventsEnumerator.Current,
          EventPointer = current.Value
        };
      }

      current = myPointersManager.NextNotDeleted(current.Value);
    }
  }
  
  public virtual EventPointer InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    myPointersManager.AssertStateOrThrow(pointer);
    AssertNotFrozen();
    myInsertedEvents.InsertAfter(pointer, eventToInsert);
    IncreaseCount();
    
    var insertedEventPointer = myPointersManager.NextNotDeleted(pointer);
    Debug.Assert(insertedEventPointer is { });
    return insertedEventPointer.Value;
  }
  
  public virtual EventPointer InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    myPointersManager.AssertStateOrThrow(pointer);
    AssertNotFrozen();
    myInsertedEvents.InsertBefore(pointer, eventToInsert);
    IncreaseCount();
    
    var insertedEventPointer = myPointersManager.PrevInternal(pointer);
    Debug.Assert(insertedEventPointer is { });
    return insertedEventPointer.Value;
  }
  
  public virtual void Remove(EventPointer pointer)
  {
    AssertNotFrozen();
    myPointersManager.Remove(pointer);
    DecreaseCount();
  }
  
  private void IncreaseCount() => ++Count;
  private void DecreaseCount() => --Count;
  public void Freeze() => myIsFrozen = true;
  public void UnFreeze() => myIsFrozen = false;

  private void AssertNotFrozen()
  {
    if (myIsFrozen) throw new CollectionIsFrozenException();
  }

  public IEnumerator<EventRecordWithPointer> GetEnumerator() => GetEnumeratorInternal().GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
  
  protected abstract IEnumerable<EventRecordWithMetadata> EnumerateInitialEvents();
}