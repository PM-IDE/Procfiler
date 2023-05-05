using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Loader;

public abstract class LoaderDomainModuleLoadUnloadNameMutatorBase : MetadataValueToNameAppenderBase
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; }


  protected LoaderDomainModuleLoadUnloadNameMutatorBase(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForModuleILPath(TraceEventsConstants.LoaderDomainModueFilePath, EventClassKind.Zero)
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderDomainModuleLoadNameMutator : LoaderDomainModuleLoadUnloadNameMutatorBase
{
  public override string EventType => TraceEventsConstants.LoaderDomainModuleLoad;


  public LoaderDomainModuleLoadNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderDomainModuleUnloadNameMutator : LoaderDomainModuleLoadUnloadNameMutatorBase
{
  public override string EventType => TraceEventsConstants.LoaderDomainModuleUnload;


  public LoaderDomainModuleUnloadNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}