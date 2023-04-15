using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Sockets;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class AcceptStartStopFailedLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy { get; } =
    new FromEventActivityIdIdCreationStrategy(TraceEventsConstants.SocketActivityBasePart);

  public AcceptStartStopFailedLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "SocketAccept", new [] { TraceEventsConstants.AcceptStart }, new[] { TraceEventsConstants.AcceptFailed, TraceEventsConstants.AcceptStop })
  {
  }
}