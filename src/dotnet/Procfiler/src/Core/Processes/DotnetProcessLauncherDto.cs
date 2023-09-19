using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.Processes.Build;

namespace Procfiler.Core.Processes;

public readonly struct DotnetProcessLauncherDto
{
  public required bool DefaultDiagnosticPortSuspend { get; init; }
  public required string PathToDotnetExecutable { get; init; }
  public required string Arguments { get; init; }
  public required bool RedirectOutput { get; init; }
  public required string? BinaryStacksSavePath { get; init; }
  public required string CppProcfilerPath { get; init; }
  public required string? MethodsFilterRegex { get; init; }
  public required bool UseCppProfiler { get; init; }


  public static DotnetProcessLauncherDto CreateFrom(
    CollectingClrEventsCommonContext context,
    BuildResult buildResult,
    ICppProcfilerLocator locator,
    IBinaryStackSavePathCreator savePathCreator) => new()
  {
    DefaultDiagnosticPortSuspend = true,
    Arguments = $"{buildResult.BuiltDllPath} {context.Arguments}",
    RedirectOutput = context.PrintProcessOutput,
    PathToDotnetExecutable = "dotnet",
    CppProcfilerPath = locator.FindCppProcfilerPath(),
    BinaryStacksSavePath = context.UseCppProfiler ? savePathCreator.CreateSavePath(buildResult) : null,
    MethodsFilterRegex = context.CppProcfilerMethodsFilterRegex,
    UseCppProfiler = context.UseCppProfiler
  };

  public static DotnetProcessLauncherDto CreateFrom(
    CollectingClrEventsCommonContext context,
    string commandName,
    ICppProcfilerLocator locator,
    IBinaryStackSavePathCreator savePathCreator) => new()
  {
    DefaultDiagnosticPortSuspend = true,
    Arguments = context.Arguments,
    RedirectOutput = context.PrintProcessOutput,
    PathToDotnetExecutable = commandName,
    CppProcfilerPath = locator.FindCppProcfilerPath(),
    BinaryStacksSavePath = context.UseCppProfiler ? savePathCreator.CreateTempSavePath() : null,
    MethodsFilterRegex = context.CppProcfilerMethodsFilterRegex,
    UseCppProfiler = context.UseCppProfiler
  };
}