using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Contention;

public abstract class ContentionStartStopNameMutatorBase : MetadataValueToNameAppenderBase
{
  protected sealed override IEnumerable<MetadataKeysWithTransform> Transformations { get; }

  
  protected ContentionStartStopNameMutatorBase(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      new MetadataKeysWithTransform(TraceEventsConstants.ContentionFlags, TransformContentionKind, EventClassKind.Zero)
    };
  }


  private string TransformContentionKind(string kind) => kind switch
  {
    "Managed" => "MANAGED",
    "Native" => "NATIVE",
    _ => MutatorsUtil.CreateUnknownEventNamePartAndLog(kind, Logger)
  };
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class ContentionStartNameMutator : ContentionStartStopNameMutatorBase
{
  public override string EventType => TraceEventsConstants.ContentionStart;


  public ContentionStartNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class ContentionEndNameMutator : ContentionStartStopNameMutatorBase
{
  public override string EventType => TraceEventsConstants.ContentionStop;


  public ContentionEndNameMutator(IProcfilerLogger logger) : base(logger)
  {
  }
}