using Procfiler.Core.EventRecord;
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

public interface IMutableEventsCollection
{
  void InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert);
  void InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert);
  void Remove(EventRecordWithMetadata eventRecord);
}

public interface IRamEventsCollection
{
  EventRecordWithMetadata? TryGetForWithDeletionCheck(EventPointer pointer);
  EventRecordWithMetadata GetFor(EventPointer pointer);
  EventPointer? NextNotDeleted(in EventPointer current);
  EventPointer? PrevNotDeleted(in EventPointer current);
}

public interface IEventsCollection : 
  IFreezableCollection, IMutableEventsCollection, IRamEventsCollection, IEnumerable<EventRecordWithMetadata>
{
  int Count { get; }
  EventPointer? First { get; }
  EventPointer? Last { get; }
  
  void ApplyNotPureActionForAllEvents(Func<EventPointer, EventRecordWithMetadata, bool> action);
}