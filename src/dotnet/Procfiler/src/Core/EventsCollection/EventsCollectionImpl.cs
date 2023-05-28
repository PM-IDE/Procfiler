﻿using Procfiler.Core.EventRecord;
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

  public void ApplyNotPureActionForAllEvents(Func<EventPointer, EventRecordWithMetadata, bool> action)
  {
    foreach (var eventWithPtr in GetEnumeratorInternal())
    {
      var shouldStop = action(eventWithPtr.EventPointer, eventWithPtr.Event);
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
}