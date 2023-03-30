using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Exceptions;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class ExceptionStartStopLifecycleMutator : EventsLifecycleMutatorBase
{
  public ExceptionStartStopLifecycleMutator(IProcfilerLogger logger) : 
    base(logger, "ExceptionStartStop", new [] { TraceEventsConstants.ExceptionStart }, new [] { TraceEventsConstants.ExceptionStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class ExceptionCatchStartStopLifecycleMutator : EventsLifecycleMutatorBase
{
  public ExceptionCatchStartStopLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "ExceptionCatch", new [] { TraceEventsConstants.ExceptionCatchStart }, new [] { TraceEventsConstants.ExceptionCatchStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class ExceptionFilterStartStopLifecycleMutator : EventsLifecycleMutatorBase
{
  public ExceptionFilterStartStopLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "ExceptionFilter", new [] { TraceEventsConstants.ExceptionFilterStart }, new [] { TraceEventsConstants.ExceptionFilterStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class ExceptionFinallyStartStopLifecycleMutator : EventsLifecycleMutatorBase
{
  public ExceptionFinallyStartStopLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "ExceptionFinally", new [] { TraceEventsConstants.ExceptionFinallyStart }, new [] { TraceEventsConstants.ExceptionFinallyStop })
  {
  }
}