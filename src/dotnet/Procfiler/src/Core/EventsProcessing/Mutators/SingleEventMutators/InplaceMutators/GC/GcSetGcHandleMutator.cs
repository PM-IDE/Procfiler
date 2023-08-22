using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.GC;

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class GcSetGcHandleMutator : MetadataValueToNameAppenderBase
{
  public override string EventType => TraceEventsConstants.GcSetGcHandle;
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }


  public GcSetGcHandleMutator(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      new MetadataKeysWithTransform(TraceEventsConstants.CommonKind, TransformHandleKind, EventClassKind.Zero)
    };
  }


  private string TransformHandleKind(string kind) => kind switch
  {
    "WeakShort" => "WEAK_SHORT",
    "WeakLong" => "WEAK_LONG",
    "Strong" => "STRONG",
    "Pinned" => "PINNED",
    "Variable" => "VARIABLE",
    "RefCounted" => "REF_COUNTED",
    "Dependent" => "DEPENDANT",
    "AsyncPinned" => "ASYNC_PINNED",
    "SizedRef" => "SIZED_REF",
    "DependendAsyncPinned" => "DEPENDEND_ASYNC_PINNED",
    _ => MutatorsUtil.CreateUnknownEventNamePartAndLog(kind, Logger)
  };
}