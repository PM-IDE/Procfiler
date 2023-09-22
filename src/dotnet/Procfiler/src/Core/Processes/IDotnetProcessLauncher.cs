using Procfiler.Core.CppProcfiler;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Processes;

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
      FileName = launcherDto.PathToDotnetExecutable,
      WorkingDirectory = Path.GetDirectoryName(launcherDto.PathToDotnetExecutable),
      RedirectStandardOutput = launcherDto.RedirectOutput,
      CreateNoWindow = true,
      Arguments = $"{launcherDto.Arguments}"
    };

    startInfo.Environment["DOTNET_DefaultDiagnosticPortSuspend"] = launcherDto.DefaultDiagnosticPortSuspend ? "1" : "0";

    if (launcherDto.CppProfilerMode.IsEnabled())
    {
      startInfo.Environment["CORECLR_ENABLE_PROFILING"] = "1";
      startInfo.Environment["CORECLR_PROFILER"] = "{90684E90-99CE-4C99-A95A-AFE3B9E09E85}";
      startInfo.Environment["CORECLR_PROFILER_PATH"] = launcherDto.CppProcfilerPath;

      if (launcherDto.BinaryStacksSavePath is null)
      {
        logger.LogError("BinaryStacksSavePath was null even when UseCppProfiler was true");
        return null;
      }
      
      logger.LogInformation("Binary stack save path {Path}", launcherDto.BinaryStacksSavePath);
      startInfo.Environment["PROCFILER_BINARY_SAVE_STACKS_PATH"] = launcherDto.BinaryStacksSavePath;

      if (launcherDto.CppProfilerMode == CppProfilerMode.PerThreadBinStacksFiles)
      {
        startInfo.Environment["PROCFILER_USE_SEPARATE_BINSTACKS_FILES"] = "1";
      }
      
      if (launcherDto.MethodsFilterRegex is { } methodsFilterRegex)
      {
        startInfo.Environment["PROCFILER_FILTER_METHODS_REGEX"] = methodsFilterRegex;

        if (launcherDto.UseDuringRuntimeFiltering)
        {
          startInfo.Environment["PROCFILER_FILTER_METHODS_DURING_RUNTIME"] = "1";
        }
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

    logger.LogInformation("Started process: {Id} {Path} {Arguments}", process.Id, launcherDto.PathToDotnetExecutable, startInfo.Arguments);

    return process;
  }
}