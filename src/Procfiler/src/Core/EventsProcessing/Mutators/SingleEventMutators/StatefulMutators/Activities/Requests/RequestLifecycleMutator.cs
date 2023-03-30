using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Requests;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class RequestStartStopLifecycleMutator : EventsLifecycleMutatorBase
{
  private static readonly string[] ourCompleteEvents =
  {
    TraceEventsConstants.RequestStop, TraceEventsConstants.RequestFailed
  };


  protected override IIdCreationStrategy IdCreationStrategy => 
    new FromEventActivityIdIdCreationStrategy(TraceEventsConstants.HttpRequestActivityBasePart);


  public RequestStartStopLifecycleMutator(IProcfilerLogger logger)
    : base(logger, "Request", new [] { TraceEventsConstants.RequestStart }, ourCompleteEvents, TraceEventsConstants.RequestLeftQueue)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class RequestContentLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy => 
    new FromEventActivityIdIdCreationStrategy(TraceEventsConstants.HttpRequestActivityBasePart);


  public RequestContentLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "RequestContent", new [] { TraceEventsConstants.RequestContentStart }, new [] { TraceEventsConstants.RequestContentStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class RequestHeaderLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy =>
    new FromEventActivityIdIdCreationStrategy(TraceEventsConstants.HttpRequestActivityBasePart);


  public RequestHeaderLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "RequestHeaders", new [] { TraceEventsConstants.RequestHeadersStart }, new [] { TraceEventsConstants.RequestHeadersStop })
  {
  }
}