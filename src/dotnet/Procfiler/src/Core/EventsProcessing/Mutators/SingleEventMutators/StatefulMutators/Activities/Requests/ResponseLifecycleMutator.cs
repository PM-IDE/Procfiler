using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Requests;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class ResponseHeaderLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy =>
    new FromEventActivityIdIdCreationStrategy(TraceEventsConstants.HttpRequestActivityBasePart);


  public ResponseHeaderLifecycleMutator(IProcfilerLogger logger)
    : base(logger, "ResponseHeaders", new [] { TraceEventsConstants.ResponseHeadersStart }, new [] { TraceEventsConstants.ResponseHeadersStop })
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class ResponseContentLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy => 
    new FromEventActivityIdIdCreationStrategy(TraceEventsConstants.HttpRequestActivityBasePart);


  public ResponseContentLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "ResponseContent", new [] { TraceEventsConstants.ResponseContentStart }, new [] { TraceEventsConstants.ResponseContentStop })
  {
  }
}