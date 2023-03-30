using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Methods;

public abstract class MethodTailCallNameMutatorBase : MetadataValueToNameAppenderBase
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; }


  protected MethodTailCallNameMutatorBase(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForTypeLikeName(
        TraceEventsConstants.MethodBeingCompiledNamespace, EventClassKind.Zero),
      MetadataKeysWithTransform.CreateForTypeLikeName(TraceEventsConstants.MethodBeingCompiledName, EventClassKind.Zero)
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class MethodTailCallSucceededNameMutator : MethodTailCallNameMutatorBase
{
  public override string EventClass => TraceEventsConstants.MethodTailCallSucceeded;


  public MethodTailCallSucceededNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class MethodTailCallFailedNameMutator : MethodTailCallNameMutatorBase
{
  public override string EventClass => TraceEventsConstants.MethodTailCallFailed;


  public MethodTailCallFailedNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}