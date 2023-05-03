using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Loader;

public abstract class LoaderModuleLoadUnloadNameMutatorBase : MetadataValueToNameAppenderBase
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; }


  protected LoaderModuleLoadUnloadNameMutatorBase(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForModuleILFileName(TraceEventsConstants.LoaderILFileName, EventClassKind.Zero)
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderModuleLoadNameMutator : LoaderModuleLoadUnloadNameMutatorBase
{
  public override string EventType => TraceEventsConstants.LoaderModuleLoad;


  public LoaderModuleLoadNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class LoaderModuleUnloadNameMutator : LoaderModuleLoadUnloadNameMutatorBase
{
  public override string EventType => TraceEventsConstants.LoaderModuleUnload;


  public LoaderModuleUnloadNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}