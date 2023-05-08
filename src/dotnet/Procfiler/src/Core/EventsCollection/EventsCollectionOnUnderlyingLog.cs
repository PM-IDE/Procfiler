using Procfiler.Core.EventRecord;
using Procfiler.Utils;

namespace Procfiler.Core.EventsCollection;

public readonly struct EventRecordWithPointer
{
  public EventRecordWithMetadata Event { get; init; }
  public EventPointer EventPointer { get; init; }
}

public interface IEventsCollection2 : IMutableEventsCollection, IFreezableCollection
{
  IEnumerable<EventRecordWithPointer> Enumerate();
}

public class EventsCollectionOnUnderlyingLog : IEventsCollection2
{
  private readonly TraceLog myLog;
  private readonly IProcfilerLogger myLogger;
  private readonly InsertedEvents myInsertedEvents;
  private readonly BitArray myDeletedInitialEvents;

  private bool myIsFrozen;


  public EventsCollectionOnUnderlyingLog(TraceLog log, IProcfilerLogger logger)
  {
    myDeletedInitialEvents = new BitArray(log.EventCount);
    myInsertedEvents = new InsertedEvents();
    myLog = log;
    myLogger = logger;
  }


  public void Freeze() => myIsFrozen = true;

  public void UnFreeze() => myIsFrozen = false;
  public IEnumerable<EventRecordWithPointer> Enumerate() => throw new NotImplementedException();

  public void InsertAfter(EventPointer pointer, EventRecordWithMetadata eventToInsert) => throw new NotImplementedException();

  public void InsertBefore(EventPointer pointer, EventRecordWithMetadata eventToInsert) => throw new NotImplementedException();

  public void Remove(EventRecordWithMetadata eventRecord) => throw new NotImplementedException();
}