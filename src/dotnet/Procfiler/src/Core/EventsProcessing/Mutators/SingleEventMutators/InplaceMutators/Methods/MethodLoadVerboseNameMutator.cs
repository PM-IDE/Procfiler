using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Methods;

public abstract class MethodLoadUnloadNameMutatorBase : MetadataValueToNameAppenderBase
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; }


  protected MethodLoadUnloadNameMutatorBase(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForTypeLikeName(TraceEventsConstants.MethodNamespace, EventClassKind.Zero),
      MetadataKeysWithTransform.CreateForTypeLikeName(TraceEventsConstants.MethodName, EventClassKind.Zero),
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class MethodLoadVerboseNameMutator : MethodLoadUnloadNameMutatorBase
{
  public override string EventType => TraceEventsConstants.MethodLoadVerbose;


  public MethodLoadVerboseNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class MethodUnloadVerboseNameMutator : MethodLoadUnloadNameMutatorBase
{
  public override string EventType => TraceEventsConstants.MethodUnloadVerbose;

  
  public MethodUnloadVerboseNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}