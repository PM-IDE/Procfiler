﻿using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Gc;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class InitialBlockingMarkingLifecycleMutator(IProcfilerLogger logger)
  : EventsLifecycleMutatorBase(
    logger,
    "InitialBlockingMarking",
    new[] { TraceEventsConstants.BgcStart },
    new[] { TraceEventsConstants.Bgc1StNonCondStop }
  );

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class FinalBlockingMarkingLifecycleMutator(IProcfilerLogger logger)
  : EventsLifecycleMutatorBase(
    logger,
    "FinalBlockingMarking",
    new[] { TraceEventsConstants.Bgc2NdNonConStart },
    new[] { TraceEventsConstants.Bgc2NdNonConStop }
  );

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class ConcurrentSweepLifecycleMutator(IProcfilerLogger logger)
  : EventsLifecycleMutatorBase(
    logger,
    "ConcurrentSweep",
    new[] { TraceEventsConstants.Bgc2NdConStart },
    new[] { TraceEventsConstants.Bgc2NdConStop }
  );

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class LohAllocationsSuppressionLifecycleMutator(IProcfilerLogger logger)
  : EventsLifecycleMutatorBase(
    logger,
    "LOHAllocationsSuppression",
    new[] { TraceEventsConstants.BgcAllocWaitStart },
    new[] { TraceEventsConstants.BgcAllocWaitStop }
  );