using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection.ModificationSources;
using Procfiler.Utils;

namespace Procfiler.Core.EventsCollection;

public class EventsCollectionImpl : EventsOwnerBase, IEventsCollection
{
  private readonly IProcfilerLogger myLogger;
  private readonly EventRecordWithMetadata[] myInitialEvents;
  private readonly List<IModificationSource> myModificationSources;
  

  public EventsCollectionImpl(EventRecordWithMetadata[] initialEvents, IProcfilerLogger logger)
    : base(initialEvents.Length)
  {
    if (initialEvents.Length == 0) throw new IndexOutOfRangeException();

    myModificationSources = new List<IModificationSource>();
    myInitialEvents = initialEvents;
    myLogger = logger;
  }
  

  public void InjectModificationSource(IModificationSource modificationSource)
  {
    myModificationSources.Add(modificationSource);
    IncreaseCount(modificationSource.Count);
  }

  public void ApplyNotPureActionForAllEvents(Func<EventRecordWithPointer, bool> action)
  {
    foreach (var eventWithPtr in this)
    {
      var shouldStop = action(eventWithPtr);
      if (shouldStop) return;
    }
  }

  public override bool Remove(EventPointer pointer)
  {
    AssertNotFrozen();
    if (!ReferenceEquals(pointer.Owner, this))
    {
      if (TryFindModificationSourceForOwner(pointer) is { } modificationSource)
      {
        return modificationSource.Remove(pointer);
      }

      return false;
    }
    
    if (pointer.IsInInitialArray)
    {
      if (myInitialEvents[pointer.IndexInArray].IsRemoved)
      {
        return false;
      }

      myInitialEvents[pointer.IndexInArray].IsRemoved = true;
      DecreaseCount();
      return true;
    }

    if (PointersManager.Remove(pointer))
    {
      DecreaseCount();
      return true;
    }

    return false;
  }

  private IModificationSource? TryFindModificationSourceForOwner(EventPointer pointer)
  {
    foreach (var modificationSource in myModificationSources)
    {
      if (ReferenceEquals(modificationSource, pointer.Owner))
      {
        return modificationSource;
      }
    }
    
    myLogger.LogError("Failed to find modification source for {Owner}, skipping remove", pointer.Owner.GetType().Name);
    return null;
  }
  
  public override EventPointer InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    if (!ReferenceEquals(pointer.Owner, this))
    {
      if (TryFindModificationSourceForOwner(pointer) is { } modificationSource)
      {
        return modificationSource.InsertAfter(pointer, eventToInsert);
      }

      throw new ArgumentOutOfRangeException();
    }
    
    return base.InsertAfter(pointer, eventToInsert);
  }

  public override EventPointer InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert)
  {
    if (!ReferenceEquals(pointer.Owner, this))
    {
      if (TryFindModificationSourceForOwner(pointer) is { } modificationSource)
      {
        return modificationSource.InsertBefore(pointer, eventToInsert);
      }

      throw new ArgumentOutOfRangeException();
    }
    
    return base.InsertBefore(pointer, eventToInsert);
  }

  protected override IEnumerable<EventRecordWithMetadata> EnumerateInitialEvents() => myInitialEvents;

  public override IEnumerator<EventRecordWithPointer> GetEnumerator()
  {
    var enumerators = new List<IEnumerable<EventRecordWithPointer>>
    {
      EnumerateInternal()
    };
    
    foreach (var modificationSource in myModificationSources)
    {
      enumerators.Add(modificationSource);
    }

    return new OrderedEventsEnumerator(enumerators);
  }

  public void Dispose()
  {
    foreach (var modificationSource in myModificationSources)
    {
      modificationSource.Dispose();
    }
  }
}