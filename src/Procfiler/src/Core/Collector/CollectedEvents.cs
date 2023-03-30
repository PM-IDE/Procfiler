using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;

namespace Procfiler.Core.Collector;

public readonly record struct CollectedEvents(
  IEventsCollection Events,
  SessionGlobalData GlobalData
);

public readonly record struct TypeIdToName(string Id, string Name);
public readonly record struct MethodIdToFqn(string Id, string Fqn);

public readonly record struct EventWithGlobalDataUpdate(
  EventRecordWithMetadata Event,
  StackTraceInfo? StackTrace,
  TypeIdToName? TypeIdToName, 
  MethodIdToFqn? MethodIdToFqn
);

public readonly record struct CreatingEventContext(
  MutableTraceEventStackSource Source,
  TraceLog Log,
  Dictionary<int, StackTraceInfo> StackTraces)
{
  private readonly Dictionary<int, int> myStacksHashCodesToIds = new();

  public StackTraceInfo? GetsStackTraceInfo(TraceEvent @event)
  {
    var id = @event.CallStackIndex();
    if (id == CallStackIndex.Invalid) return null;

    var intId = (int) id;
    if (StackTraces.TryGetValue(intId, out var existing)) return existing;

    var info = @event.CreateEventStackTraceInfoOrThrow(Source);
    var infoHash = info.GetHashCode();
    if (myStacksHashCodesToIds.TryGetValue(infoHash, out var existingId))
    {
      return StackTraces[existingId];
    }

    myStacksHashCodesToIds[infoHash] = intId;
    StackTraces[intId] = info;
    return info;
  }
}
