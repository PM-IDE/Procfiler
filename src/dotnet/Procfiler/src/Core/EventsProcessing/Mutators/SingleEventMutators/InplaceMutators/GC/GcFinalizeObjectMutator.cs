using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.GC;

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class GcFinalizeObjectMutator : MetadataValuesRemover
{
  protected override string[] MetadataKeys { get; } =
  {
    TraceEventsConstants.CommonTypeId,
    TraceEventsConstants.CommonObjectId
  };

  public override string EventType => TraceEventsConstants.GcFinalizeObject;
  

  public GcFinalizeObjectMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class GcFinalizeObjectNameMutator : MetadataValueToNameAppenderBase
{
  public override string EventType => TraceEventsConstants.GcFinalizeObject;
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }
  
  
  public GcFinalizeObjectNameMutator(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForTypeLikeName(TraceEventsConstants.CommonTypeName, EventClassKind.Zero)
    };
  }
}