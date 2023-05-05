using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.GC;

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class GcPinObjectAtGcTimeMutator : MetadataValuesRemover
{
  public override string EventType => TraceEventsConstants.GcPinObjectAtGcTime;
  protected override string[] MetadataKeys { get; } =
  {
    TraceEventsConstants.CommonObjectId,
    TraceEventsConstants.CommonHandleId
  };


  public GcPinObjectAtGcTimeMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class GcPinObjectAtGcTimeNameMutator : MetadataValueToNameAppenderBase
{
  public override string EventType => TraceEventsConstants.GcPinObjectAtGcTime;
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }
  
  
  public GcPinObjectAtGcTimeNameMutator(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForTypeLikeName(TraceEventsConstants.CommonTypeName, EventClassKind.Zero)
    };
  }
}