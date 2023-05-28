using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection.ModificationSources;
using Procfiler.Core.Exceptions;

namespace Procfiler.Core.EventsCollection;

public interface IFreezableCollection
{
  void Freeze();
  void UnFreeze();
}

public class CollectionIsFrozenException : ProcfilerException
{
}

public interface IInsertableEventsCollection
{
  EventPointer InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert);
  EventPointer InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert);
}

public interface IRemovableEventsCollection
{
  bool Remove(EventPointer pointer);
}

public interface IMutableEventsCollection : IRemovableEventsCollection, IInsertableEventsCollection
{

}

public readonly struct EventRecordWithPointer
{
  public required EventRecordWithMetadata Event { get; init; }
  public required EventPointer EventPointer { get; init; }

  public void Deconstruct(out EventPointer pointer, out EventRecordWithMetadata eventRecord)
  {
    pointer = EventPointer;
    eventRecord = Event;
  }
}

public interface IEventsOwner
{
  long Count { get; }
}

public interface IEventsCollection : 
  IFreezableCollection, 
  IMutableEventsCollection, 
  ILazilyModifiableEventsCollection, 
  IEnumerable<EventRecordWithPointer>,
  IEventsOwner
{
  void ApplyNotPureActionForAllEvents(Func<EventPointer, EventRecordWithMetadata, bool> action);
}

public interface ILazilyModifiableEventsCollection
{
  void InjectModificationSource(IModificationSource modificationSource);
}