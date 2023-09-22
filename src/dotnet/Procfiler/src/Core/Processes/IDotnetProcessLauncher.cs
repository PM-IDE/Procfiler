﻿using Procfiler.Core.CppProcfiler;
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

    startInfo.Environment[DotNetEnvs.DefaultDiagnosticPortSuspend] = launcherDto.DefaultDiagnosticPortSuspend ? "1" : "0";

    if (launcherDto.CppProfilerMode.IsEnabled())
    {
      startInfo.Environment[DotNetEnvs.CoreClrEnableProfiling] = "1";
      startInfo.Environment[DotNetEnvs.CoreClrProfiler] = "{90684E90-99CE-4C99-A95A-AFE3B9E09E85}";
      startInfo.Environment[DotNetEnvs.CoreClrProfilerPath] = launcherDto.CppProcfilerPath;

      if (launcherDto.BinaryStacksSavePath is null)
      {
        logger.LogError("BinaryStacksSavePath was null even when UseCppProfiler was true");
        return null;
      }
      
      logger.LogInformation("Binary stack save path {Path}", launcherDto.BinaryStacksSavePath);
      startInfo.Environment[CppProfilerEnvs.BinaryStacksSavePath] = launcherDto.BinaryStacksSavePath;

      if (launcherDto.CppProfilerMode.ToFileMode() == CppProfilerBinStacksFileMode.PerThreadFiles)
      {
        startInfo.Environment[CppProfilerEnvs.UseSeparateBinStacksFiles] = "1";
      }

      if (launcherDto.CppProfilerMode.IsOnlineSerialization())
      {
        startInfo.Environment[CppProfilerEnvs.OnlineSerialization] = "1";
      }
      
      if (launcherDto.MethodsFilterRegex is { } methodsFilterRegex)
      {
        startInfo.Environment[CppProfilerEnvs.MethodsFilterRegex] = methodsFilterRegex;

        if (launcherDto.UseDuringRuntimeFiltering)
        {
          startInfo.Environment[CppProfilerEnvs.MethodsFilteringDuringRuntime] = "1";
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