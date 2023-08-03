using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Loader;

public abstract class LoaderAssemblyLoadUnloadNameMutatorBase(IProcfilerLogger logger) : MetadataValueToNameAppenderBase(logger)
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; } = new[]
  {
    MetadataKeysWithTransform.CreateForAssemblyName(TraceEventsConstants.LoaderAssemblyName, EventClassKind.Zero),
  };
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderAssemblyLoadNameMutator(IProcfilerLogger logger) : LoaderAssemblyLoadUnloadNameMutatorBase(logger)
{
  public override string EventType => TraceEventsConstants.LoaderAssemblyLoad;
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderAssemblyUnloadNameMutator(IProcfilerLogger logger) : LoaderAssemblyLoadUnloadNameMutatorBase(logger)
{
  public override string EventType => TraceEventsConstants.LoaderAssemblyUnload;
}