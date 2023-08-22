using Procfiler.Core.EventRecord;

namespace Procfiler.Core.EventsCollection;

public abstract class EventsOwnerBase : IEventsOwner
{
  protected readonly EventPointersManager PointersManager;

  private bool myIsFrozen;


  public abstract long Count { get; }


  protected EventsOwnerBase(long initialEventsCount)
  {
    PointersManager = new EventPointersManager(initialEventsCount, new InsertedEvents(), this);
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
      if (PointersManager.TryGetInsertedEvent(current.Value) is { } insertedEvent)
      {
        if (!PointersManager.IsRemoved(current.Value))
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

        if (!PointersManager.IsRemoved(current.Value))
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

  public virtual EventPointer InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert) =>
    PointersManager.InsertAfter(pointer, eventToInsert);

  public virtual EventPointer InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert) =>
    PointersManager.InsertBefore(pointer, eventToInsert);


  public abstract bool Remove(EventPointer pointer);

  public void Freeze() => myIsFrozen = true;
  public void UnFreeze() => myIsFrozen = false;

  protected void AssertNotFrozen()
  {
    if (myIsFrozen) throw new CollectionIsFrozenException();
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  protected abstract IEnumerable<EventRecordWithMetadata> EnumerateInitialEvents();
}