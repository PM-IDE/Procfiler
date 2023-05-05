using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.Exceptions;

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class ExceptionStartNameMutator : MetadataValueToNameAppenderBase
{
  public override string EventType => TraceEventsConstants.ExceptionStart;
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }
  
  
  public ExceptionStartNameMutator(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      MetadataKeysWithTransform.CreateForTypeLikeName(TraceEventsConstants.ExceptionType, EventClassKind.Zero)
    };
  }
}