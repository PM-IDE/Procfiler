using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Methods;

public abstract class MethodInliningNameMutatorBase : MetadataValueToNameAppenderBase
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; }


  protected MethodInliningNameMutatorBase(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForTypeLikeName(
        TraceEventsConstants.MethodInliningSucceededInlineeNamespace, EventClassKind.Zero),
      MetadataKeysWithTransform.CreateForTypeLikeName(
        TraceEventsConstants.MethodInliningSucceededInlineeName, EventClassKind.Zero),
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class MethodInliningFailedNameMutator : MethodInliningNameMutatorBase
{
  public override string EventType => TraceEventsConstants.MethodInliningFailed;


  public MethodInliningFailedNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class MethodInliningSucceededNameMutator : MethodInliningNameMutatorBase
{
  public override string EventType => TraceEventsConstants.MethodInliningSucceeded;


  public MethodInliningSucceededNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}