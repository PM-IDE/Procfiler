using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Methods;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class MethodR2REventLifecycleMutator : EventsLifecycleMutatorBase
{
  public MethodR2REventLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "R2REntryPoint", new [] { TraceEventsConstants.MethodR2RGetEntryPointStart }, new [] { TraceEventsConstants.MethodR2RGetEntryPoint })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class MethodLoadUnloadLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy { get; }

  
  public MethodLoadUnloadLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "MethodLoading", new [] { TraceEventsConstants.MethodLoadVerbose }, new [] { TraceEventsConstants.MethodUnloadVerbose })
  {
    IdCreationStrategy = new FromAttributesIdCreationStrategy("MethodLoadUnload", new[]
    {
      TraceEventsConstants.MethodNamespace,
      TraceEventsConstants.MethodName,
      TraceEventsConstants.MethodSignature,
    });
  }
}