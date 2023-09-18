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
    Arguments = context.Arguments,
    RedirectOutput = context.PrintProcessOutput,
    PathToDotnetExecutable = buildResult.BuiltDllPath,
    CppProcfilerPath = locator.FindCppProcfilerPath(),
    BinaryStacksSavePath = context.UseCppProfiler ? savePathCreator.CreateSavePath(buildResult) : null,
    MethodsFilterRegex = context.CppProcfilerMethodsFilterRegex,
    UseCppProfiler = context.UseCppProfiler
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
    };

    startInfo.Environment["DOTNET_DefaultDiagnosticPortSuspend"] = launcherDto.DefaultDiagnosticPortSuspend ? "1" : "0";

    if (launcherDto.UseCppProfiler)
    {
      startInfo.Environment["CORECLR_ENABLE_PROFILING"] = "1";
      startInfo.Environment["CORECLR_PROFILER"] = "{90684E90-99CE-4C99-A95A-AFE3B9E09E85}";
      startInfo.Environment["CORECLR_PROFILER_PATH"] = launcherDto.CppProcfilerPath;

      if (launcherDto.BinaryStacksSavePath is null)
      {
        logger.LogError("BinaryStacksSavePath was null even when UseCppProfiler was true");
        return null;
      }
      
      startInfo.Environment["PROCFILER_BINARY_SAVE_STACKS_PATH"] = launcherDto.BinaryStacksSavePath;
      
      if (launcherDto.MethodsFilterRegex is { } methodsFilterRegex)
      {
        startInfo.Environment["PROCFILER_FILTER_METHODS_REGEX"] = methodsFilterRegex;
        startInfo.Environment["PROCFILER_FILTER_METHODS_DURING_RUNTIME"] = "1";
      }
    }

    var process = new Process
    {
      StartInfo = startInfo
    };

    if (!process.Start())
    {
      logger.LogError("Failed to start process {Path}", launcherDto.PathToDotnetExecutable);
      return null;
    }

    return process;
  }
}