using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.GC;

[EventMutator(MultipleEventMutatorsPasses.LastMultipleMutators)]
public class GcSampledObjectAllocationMutator : MetadataValueToNameAppenderBase
{
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }

  
  public override string EventClass => TraceEventsConstants.GcSampledObjectAllocation;

  
  public GcSampledObjectAllocationMutator(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForTypeLikeName(
        TraceEventsConstants.GcSampledObjectAllocationTypeName, EventClassKind.Zero)
    };
  }
}