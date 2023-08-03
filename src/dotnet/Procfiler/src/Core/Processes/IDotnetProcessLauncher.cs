using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.Processes.Build;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Processes;

public readonly struct DotnetProcessLauncherDto
{
  public required bool DefaultDiagnosticPortSuspend { get; init; }
  public required string PathToDotnetExecutable { get; init; }
  public required string Arguments { get; init; }
  public required bool RedirectOutput { get; init; }
  public required string BinaryStacksSavePath { get; init; }
  public required string CppProcfilerPath { get; init; }
  public required string? MethodsFilterRegex { get; init; }


  public static DotnetProcessLauncherDto CreateFrom(
    CollectingClrEventsCommonContext context,
    BuildResult buildResult,
    ICppProcfilerLocator locator,
    IBinaryStackSavePathCreator savePathCreator) => new()
  {
    DefaultDiagnosticPortSuspend = true,
    Arguments = context.Arguments,
    RedirectOutput = context.PrintProcessOutput,
    PathToDotnetExecutable = buildResult.BuiltDllPath,
    CppProcfilerPath = locator.FindCppProcfilerPath(),
    BinaryStacksSavePath = savePathCreator.CreateSavePath(buildResult),
    MethodsFilterRegex = context.CppProcfilerMethodsFilterRegex
  };
}

public interface IDotnetProcessLauncher
{
  Process? TryStartDotnetProcess(DotnetProcessLauncherDto launcherDto);
}

[AppComponent]
public class DotnetProcessLauncher(IProcfilerLogger logger) : IDotnetProcessLauncher
{
  public Process? TryStartDotnetProcess(DotnetProcessLauncherDto launcherDto)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      WorkingDirectory = Path.GetDirectoryName(launcherDto.PathToDotnetExecutable),
      RedirectStandardOutput = launcherDto.RedirectOutput,
      CreateNoWindow = true,
      Arguments = $"{launcherDto.PathToDotnetExecutable} {launcherDto.Arguments}",
      Environment =
      {
        ["DOTNET_DefaultDiagnosticPortSuspend"] = launcherDto.DefaultDiagnosticPortSuspend ? "1" : "0",
        ["CORECLR_ENABLE_PROFILING"] = "1",
        ["CORECLR_PROFILER"] = "{585022b6-31e9-4ddf-b35d-3c256d0a16f3}",
        ["CORECLR_PROFILER_PATH"] = launcherDto.CppProcfilerPath,
        ["PROCFILER_BINARY_SAVE_STACKS_PATH"] = launcherDto.BinaryStacksSavePath
      }
    };

    if (launcherDto.MethodsFilterRegex is { } methodsFilterRegex)
    {
      startInfo.Environment["PROCFILER_FILTER_METHODS_REGEX"] = methodsFilterRegex;
    }
    
    var process = new Process
    {
      StartInfo = startInfo,
    };

    if (!process.Start())
    {
      logger.LogError("Failed to start process {Path}", launcherDto.PathToDotnetExecutable);
      return null;
    }

    return process;
  }
}