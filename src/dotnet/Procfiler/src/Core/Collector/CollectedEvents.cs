using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;

namespace Procfiler.Core.Collector;

public readonly record struct CollectedEvents(
  IEventsCollection Events,
  SessionGlobalData GlobalData
);

public readonly record struct TypeIdToName(long Id, string Name);
public readonly record struct MethodIdToFqn(long Id, string Fqn);

public readonly record struct EventWithGlobalDataUpdate(
  EventRecordWithMetadata Event,
  TypeIdToName? TypeIdToName, 
  MethodIdToFqn? MethodIdToFqn
);

public readonly record struct CreatingEventContext(MutableTraceEventStackSource Source, TraceLog Log);
