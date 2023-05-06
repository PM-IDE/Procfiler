using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.ArrayPools;

public abstract class BufferEventTypeNameMutator : SingleEventMutatorBase
{
  private const string Buffer = TraceEventsConstants.BufferEventType;


  protected BufferEventTypeNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }

  
  public override IEnumerable<EventLogMutation> Mutations =>
    new[] { new EventTypeNameMutation(EventType, $"{Buffer}/{EventType[Buffer.Length..]}") };

  protected override void ProcessInternal(EventRecordWithMetadata eventRecord, SessionGlobalData context)
  {
    Debug.Assert(eventRecord.EventClass.StartsWith(Buffer));
    Debug.Assert(eventRecord.EventName.StartsWith(Buffer));

    const string EventTypeSeparator = TraceEventsConstants.EventTypeSeparator;
    eventRecord.EventName = eventRecord.EventName.Insert(Buffer.Length, EventTypeSeparator);
    eventRecord.EventClass = eventRecord.EventClass.Insert(Buffer.Length, EventTypeSeparator);
  }
}

//todo: seems very bad, need to refactor
[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class BufferAllocatedEventTypeMutator : BufferEventTypeNameMutator
{
  public override string EventType => TraceEventsConstants.BufferAllocated;

  
  public BufferAllocatedEventTypeMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class BufferRentedEventTypeMutator : BufferEventTypeNameMutator
{
  public override string EventType => TraceEventsConstants.BufferRented;

  
  public BufferRentedEventTypeMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class BufferReturnedEventTypeMutator : BufferEventTypeNameMutator
{
  public override string EventType => TraceEventsConstants.BufferReturned;

  
  public BufferReturnedEventTypeMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class BufferTrimmedEventTypeMutator : BufferEventTypeNameMutator
{
  public override string EventType => TraceEventsConstants.BufferTrimmed;

  
  public BufferTrimmedEventTypeMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class BufferTrimPollTypeMutator : BufferEventTypeNameMutator
{
  public override string EventType => TraceEventsConstants.BufferTrimPoll;

  
  public BufferTrimPollTypeMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}
