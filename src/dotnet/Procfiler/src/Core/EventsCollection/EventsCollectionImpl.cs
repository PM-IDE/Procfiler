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
  

  public void InjectModificationSource(IModificationSource modificationSource) => 
    myModificationSources.Add(modificationSource);

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

  protected override IEnumerable<EventRecordWithMetadata> EnumerateInitialEvents() => myInitialEvents;

  public override IEnumerator<EventRecordWithPointer> GetEnumerator()
  {
    var enumerators = new List<IEnumerable<EventRecordWithPointer>>
    {
      EnumerateInternal()
    };
    
    enumerators.AddRange(myModificationSources);
    return new OrderedEventsEnumerator(enumerators);
  }
}