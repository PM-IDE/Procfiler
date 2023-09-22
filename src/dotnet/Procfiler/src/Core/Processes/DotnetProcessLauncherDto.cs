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
  public required CppProfilerMode CppProfilerMode { get; init; }
  public required bool UseDuringRuntimeFiltering { get; init; }
  public required bool CppProfilerUseConsoleLogging { get; init; }


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
    MethodsFilterRegex = context.CppProcfilerMethodsFilterRegex,
    CppProfilerMode = context.CppProfilerMode,
    UseDuringRuntimeFiltering = context.UseDuringRuntimeFiltering,
    CppProfilerUseConsoleLogging = context.CppProfilerUseConsoleLogging,
    BinaryStacksSavePath = context.CppProfilerMode.IsEnabled() switch
    {
      true => savePathCreator.CreateSavePath(buildResult, context.CppProfilerMode),
      false => null
    }
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
    MethodsFilterRegex = context.CppProcfilerMethodsFilterRegex,
    CppProfilerMode = context.CppProfilerMode,
    UseDuringRuntimeFiltering = context.UseDuringRuntimeFiltering,
    CppProfilerUseConsoleLogging = context.CppProfilerUseConsoleLogging,
    BinaryStacksSavePath = context.CppProfilerMode.IsEnabled() switch
    {
      true => savePathCreator.CreateTempSavePath(context.CppProfilerMode),
      false => null
    }
  };
}