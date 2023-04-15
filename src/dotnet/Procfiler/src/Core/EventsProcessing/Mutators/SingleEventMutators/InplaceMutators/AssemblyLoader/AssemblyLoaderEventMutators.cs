using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.AssemblyLoader;

public abstract class AssemblyLoaderStartStopNameMutatorBase : MetadataValueToNameAppenderBase
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; }


  protected AssemblyLoaderStartStopNameMutatorBase(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForAssemblyName(TraceEventsConstants.AssemblyName, EventClassKind.Zero),
      MetadataKeysWithTransform.CreateForModuleILPath(TraceEventsConstants.AssemblyPath, EventClassKind.Zero),
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class AssemblyLoaderStartNameMutator : AssemblyLoaderStartStopNameMutatorBase
{
  public override string EventClass => TraceEventsConstants.AssemblyLoaderStart;


  public AssemblyLoaderStartNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class AssemblyLoaderStopNameMutator : AssemblyLoaderStartStopNameMutatorBase
{
  public override string EventClass => TraceEventsConstants.AssemblyLoaderStop;


  public AssemblyLoaderStopNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class AssemblyLoaderKnownPathProbedNameMutator : MetadataValueToNameAppenderBase
{
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }

  public override string EventClass => TraceEventsConstants.AssemblyLoaderKnownPathProbed;


  public AssemblyLoaderKnownPathProbedNameMutator(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForModuleILPath(TraceEventsConstants.FilePath, EventClassKind.Zero)
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class AssemblyLoaderResolutionAttemptedNameMutator : MetadataValueToNameAppenderBase
{
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }
  
  
  public override string EventClass => TraceEventsConstants.AssemblyLoaderResolutionAttempted;


  public AssemblyLoaderResolutionAttemptedNameMutator(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForAssemblyName(TraceEventsConstants.AssemblyName, EventClassKind.Zero),
      MetadataKeysWithTransform.CreateIdenticalTransform(TraceEventsConstants.Stage, EventClassKind.NotEventClass),
      MetadataKeysWithTransform.CreateIdenticalTransform(TraceEventsConstants.Result, EventClassKind.NotEventClass)
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class AssemblyLoaderAppDomainAssemblyResolveHandlerInvokeNameMutator : MetadataValueToNameAppenderBase
{ 
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }

  
  public override string EventClass => TraceEventsConstants.AssemblyLoaderAppDomainAssemblyResolveHandlerInvoked;


  public AssemblyLoaderAppDomainAssemblyResolveHandlerInvokeNameMutator(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForAssemblyName(TraceEventsConstants.AssemblyName, EventClassKind.Zero),
      MetadataKeysWithTransform.CreateIdenticalTransform(TraceEventsConstants.HandlerName, EventClassKind.Zero)
    };
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class AssemblyLoaderLoadContextResolvingHandlerInvokedNameMutator : MetadataValueToNameAppenderBase
{
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }

  
  public override string EventClass => TraceEventsConstants.AssemblyLoaderAssemblyLoadFromResolveHandlerInvoked;
  
  
  public AssemblyLoaderLoadContextResolvingHandlerInvokedNameMutator(IProcfilerLogger logger, bool removeProperties = false) : base(logger, removeProperties)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForAssemblyName(TraceEventsConstants.AssemblyName, EventClassKind.Zero),
      MetadataKeysWithTransform.CreateIdenticalTransform(TraceEventsConstants.HandlerName, EventClassKind.Zero),
      MetadataKeysWithTransform.CreateIdenticalTransform(TraceEventsConstants.AssemblyLoadContext, EventClassKind.Zero)
    };
  }
}