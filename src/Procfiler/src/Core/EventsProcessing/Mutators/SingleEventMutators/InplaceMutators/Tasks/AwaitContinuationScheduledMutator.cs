using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Tasks;

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class AwaitContinuationScheduledMutator : AttributeRenamingMutatorBase
{
  public override string EventClass => TraceEventsConstants.AwaitTaskContinuationScheduledSend;

  
  public AwaitContinuationScheduledMutator(IProcfilerLogger logger) 
    : base(logger, TraceEventsConstants.ContinueWithTaskId, TraceEventsConstants.TaskId)
  {
  }
}