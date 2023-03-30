using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Gc;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class InitialBlockingMarkingLifecycleMutator : EventsLifecycleMutatorBase
{
  public InitialBlockingMarkingLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "InitialBlockingMarking", new [] { TraceEventsConstants.BgcStart }, new[] { TraceEventsConstants.Bgc1StNonCondStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class FinalBlockingMarkingLifecycleMutator : EventsLifecycleMutatorBase
{
  public FinalBlockingMarkingLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "FinalBlockingMarking", new[] { TraceEventsConstants.Bgc2NdNonConStart}, new[] { TraceEventsConstants.Bgc2NdNonConStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class ConcurrentSweepLifecycleMutator : EventsLifecycleMutatorBase
{
  public ConcurrentSweepLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "ConcurrentSweep" , new[] { TraceEventsConstants.Bgc2NdConStart }, new [] { TraceEventsConstants.Bgc2NdConStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class LohAllocationsSuppressionLifecycleMutator : EventsLifecycleMutatorBase
{
  public LohAllocationsSuppressionLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "LOHAllocationsSuppression", new[] { TraceEventsConstants.BgcAllocWaitStart }, new [] { TraceEventsConstants.BgcAllocWaitStop })
  {
  }
}