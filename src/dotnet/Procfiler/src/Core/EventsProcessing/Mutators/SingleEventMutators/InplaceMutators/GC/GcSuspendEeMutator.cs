using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.InplaceMutators.GC;

[EventMutator(SingleEventMutatorsPasses.SingleEventsMutators)]
public class GcSuspendEeMutator : MetadataValueToNameAppenderBase
{
  public override string EventClass => TraceEventsConstants.GcSuspendEeStart;
  protected override IEnumerable<MetadataKeysWithTransform> Transformations { get; }


  public GcSuspendEeMutator(IProcfilerLogger logger) : base(logger)
  {
    Transformations = new[]
    {
      new MetadataKeysWithTransform(
        TraceEventsConstants.GcSuspendEeStartReason, GenerateNameForReason, EventClassKind.Zero)
    };
  }
  

  private string GenerateNameForReason(string reason) => reason switch
  {
    "SuspendOther" => "OTHER",
    "SuspendForGC" => "GC",
    "SuspendForAppDomainShutdown" => "APP_DOMAIN_SHUTDOWN",
    "SuspendForCodePitching" => "CODE_PITCHING",
    "SuspendForShutdown" => "SHUTDOWN",
    "SuspendForDebugger" => "DEBUGGER",
    "SuspendForGCPrep" => "GC_PREP",
    "SuspendForDebuggerSweep" => "DEBUGGER_SWEEP",
    _ => MutatorsUtil.CreateUnknownEventNamePartAndLog(reason, Logger)
  };
}