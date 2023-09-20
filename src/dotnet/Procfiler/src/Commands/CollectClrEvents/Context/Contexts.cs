using Procfiler.Core.Collector;
using Procfiler.Core.InstrumentalProfiler;
using Procfiler.Utils;

namespace Procfiler.Commands.CollectClrEvents.Context;

public readonly record struct SerializationContext(FileFormat OutputFormat);

public record struct CollectingClrEventsCommonContext(
  string OutputPath,
  SerializationContext SerializationContext,
  IParseResultInfoProvider CommandParseResult,
  string Arguments,
  ProvidersCategoryKind ProviderCategory,
  bool ClearPathBefore,
  int DurationMs,
  int MaxWaitForLogWriteTimeoutMs,
  bool PrintProcessOutput,
  string? CppProcfilerMethodsFilterRegex,
  int ProcessWaitTimeoutMs,
  bool UseCppProfiler,
  bool UseDuringRuntimeFiltering
);

public record struct ProjectBuildInfo(
  string CsprojPath,
  string Tfm,
  BuildConfiguration Configuration,
  InstrumentationKind InstrumentationKind,
  bool RemoveTempPath,
  string? TempPath,
  bool SelfContained,
  string? AdditionalBuildArgs
);

public record CollectClrEventsContext(CollectingClrEventsCommonContext CommonContext);

public record CollectClrEventsFromExeContext(
  ProjectBuildInfo ProjectBuildInfo,
  CollectingClrEventsCommonContext CommonContext
) : CollectClrEventsContext(CommonContext);

public record CollectClrEventsFromExeWithRepeatContext(
  ProjectBuildInfo ProjectBuildInfo,
  int RepeatCount,
  CollectingClrEventsCommonContext CommonContext
) : CollectClrEventsFromExeContext(ProjectBuildInfo, CommonContext);

public record CollectClrEventsFromExeWithArguments(
  ProjectBuildInfo ProjectBuildInfo,
  CollectingClrEventsCommonContext CommonContext,
  IReadOnlyList<string> Arguments
) : CollectClrEventsFromExeContext(ProjectBuildInfo, CommonContext);

public record CollectClrEventsFromRunningProcessContext(
  int ProcessId,
  CollectingClrEventsCommonContext CommonContext
) : CollectClrEventsContext(CommonContext);

public record CollectClrEventsFromCommandContext(
  string CommandName,
  IReadOnlyList<string>? Arguments,
  CollectingClrEventsCommonContext CommonContext
) : CollectClrEventsContext(CommonContext);
