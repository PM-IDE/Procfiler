using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities.Loader;

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class LoaderAppDomainLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy { get; }


  public LoaderAppDomainLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "LoaderAppDomain", new [] { TraceEventsConstants.LoaderAppDomainLoad }, new [] { TraceEventsConstants.LoaderAppDomainUnload })
  {
    IdCreationStrategy = new FromAttributesIdCreationStrategy("LoaderAppDomain", new[] { TraceEventsConstants.LoaderAppDomainName });
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class LoaderAssemblyLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy { get; }
  
  
  public LoaderAssemblyLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "LoaderAssembly", new [] { TraceEventsConstants.LoaderAssemblyLoad }, new [] { TraceEventsConstants.LoaderAssemblyUnload })
  {
    IdCreationStrategy = new FromAttributesIdCreationStrategy("LoaderAssembly", new[] { TraceEventsConstants.LoaderAssemblyName });
  }
}

[EventMutator(SingleEventMutatorsPasses.ActivityAttributesSetter)]
public class LoaderModuleLifecycleMutator : EventsLifecycleMutatorBase
{
  protected override IIdCreationStrategy IdCreationStrategy { get; }

  
  public LoaderModuleLifecycleMutator(IProcfilerLogger logger) 
    : base(logger, "LoaderModule", new [] { TraceEventsConstants.LoaderModuleLoad }, new [] { TraceEventsConstants.LoaderModuleUnload })
  {
    IdCreationStrategy = new FromAttributesIdCreationStrategy("LoaderModule", new[] { TraceEventsConstants.LoaderILFileName });
  }
}