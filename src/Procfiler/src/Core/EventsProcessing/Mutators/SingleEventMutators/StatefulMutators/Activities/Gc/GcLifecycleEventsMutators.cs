using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Gc;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class GcFinalizersStartStopLifecycleMutator : EventsLifecycleMutatorBase
{
  public GcFinalizersStartStopLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "Finalizers", new [] { TraceEventsConstants.GcFinalizersStart }, new [] { TraceEventsConstants.GcFinalizersStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class GcRestartEeLifecycleMutator : EventsLifecycleMutatorBase
{
  public GcRestartEeLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "RestartEE", new [] { TraceEventsConstants.GcRestartEeStart }, new [] { TraceEventsConstants.GcRestartEeStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class GcSuspendEeStartStopLifecycleMutator : EventsLifecycleMutatorBase
{
  public GcSuspendEeStartStopLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "SuspendEE", new [] { TraceEventsConstants.GcSuspendEeStart }, new [] { TraceEventsConstants.GcSuspendEeStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class GcProcessLifecycleEventsMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy { get; }

  
  public GcProcessLifecycleEventsMutator(IProcfilerLogger logger) 
    : base(logger, "GC", new [] { TraceEventsConstants.GcStart }, new [] { TraceEventsConstants.GcStop })
  {
    IdCreationStrategy = new FromAttributesIdCreationStrategy("GC", new List<string> { TraceEventsConstants.GcCount });
  }
}