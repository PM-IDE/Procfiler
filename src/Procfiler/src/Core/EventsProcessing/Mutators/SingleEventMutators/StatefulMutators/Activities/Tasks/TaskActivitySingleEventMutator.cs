using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Tasks;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class TaskActivitySingleEventMutator : EventsLifecycleMutatorBase
{
  private const string ActivityId = "TaskExecute";
  
  
  protected override IIdCreationStrategy IdCreationStrategy { get; }
  

  public TaskActivitySingleEventMutator(IProcfilerLogger logger) 
    : base(logger, ActivityId, new [] { TraceEventsConstants.TaskExecuteStart }, new [] { TraceEventsConstants.TaskExecuteStop }, TraceEventsConstants.TaskScheduledSend)
  {
    IdCreationStrategy = new FromAttributesIdCreationStrategy(ActivityId, new[]
    {
      TraceEventsConstants.TaskId
    });
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class TaskWaitBeginLifecycleMutator : EventsLifecycleMutatorBase
{
  private const string ActivityId = "TaskWaitBeginEnd";
  
  protected override IIdCreationStrategy IdCreationStrategy { get; }

  
  public TaskWaitBeginLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, ActivityId, new [] { TraceEventsConstants.TaskWaitSend }, new [] { TraceEventsConstants.TaskWaitStop })
  {
    IdCreationStrategy = new FromAttributesIdCreationStrategy(ActivityId, new[]
    {
      TraceEventsConstants.TaskId
    });
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class TaskContinuationWaitLifecycleMutator : EventsLifecycleMutatorBase
{
  private static readonly HashSet<string> ourStartEventClasses = new()
  {
    TraceEventsConstants.TaskWaitContinuationStarted,
    TraceEventsConstants.AwaitTaskContinuationScheduledSend
  };

  private const string ActivityId = "TaskContinuationWait";
  
  
  protected override IIdCreationStrategy IdCreationStrategy { get; }

  
  public TaskContinuationWaitLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, ActivityId, ourStartEventClasses, new [] { TraceEventsConstants.TaskWaitContinuationComplete })
  {
    IdCreationStrategy = new FromAttributesIdCreationStrategy(ActivityId, new[]
    {
      TraceEventsConstants.TaskId
    });
  }
}