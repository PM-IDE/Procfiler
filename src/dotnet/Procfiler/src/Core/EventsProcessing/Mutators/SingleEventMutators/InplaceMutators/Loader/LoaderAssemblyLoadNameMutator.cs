using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Loader;

public abstract class LoaderAssemblyLoadUnloadNameMutatorBase : MetadataValueToNameAppenderBase
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; }


  protected LoaderAssemblyLoadUnloadNameMutatorBase(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForAssemblyName(TraceEventsConstants.LoaderAssemblyName, EventClassKind.Zero),
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderAssemblyLoadNameMutator : LoaderAssemblyLoadUnloadNameMutatorBase
{
  public override string EventType => TraceEventsConstants.LoaderAssemblyLoad;


  public LoaderAssemblyLoadNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderAssemblyUnloadNameMutator : LoaderAssemblyLoadUnloadNameMutatorBase
{
  public override string EventType => TraceEventsConstants.LoaderAssemblyUnload;


  public LoaderAssemblyUnloadNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}