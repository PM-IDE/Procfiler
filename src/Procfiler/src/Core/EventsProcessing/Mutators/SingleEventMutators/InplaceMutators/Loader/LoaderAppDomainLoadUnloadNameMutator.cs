using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Loader;

public abstract class LoaderAppDomainLoadUnloadNameMutatorBase : MetadataValueToNameAppenderBase
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; }

  
  protected LoaderAppDomainLoadUnloadNameMutatorBase(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForCamelCaseName(TraceEventsConstants.LoaderAppDomainName, EventClassKind.Zero),
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderAppDomainLoadNameMutator : LoaderAppDomainLoadUnloadNameMutatorBase
{
  public override string EventClass => TraceEventsConstants.LoaderAppDomainLoad;

  
  public LoaderAppDomainLoadNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderAppDomainUnloadNameMutator : LoaderAppDomainLoadUnloadNameMutatorBase
{
  public override string EventClass => TraceEventsConstants.LoaderAppDomainUnload;

  
  public LoaderAppDomainUnloadNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

