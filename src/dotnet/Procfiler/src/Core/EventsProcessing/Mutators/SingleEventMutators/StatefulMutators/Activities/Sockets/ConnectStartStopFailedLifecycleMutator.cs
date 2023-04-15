using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Sockets;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class ConnectStartStopFailedLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy { get; } = 
    new FromEventActivityIdIdCreationStrategy(TraceEventsConstants.SocketActivityBasePart);

  public ConnectStartStopFailedLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "SocketConnect", new [] { TraceEventsConstants.ConnectStart }, new[] { TraceEventsConstants.ConnectStop, TraceEventsConstants.ConnectFailed })
  {
  }
}