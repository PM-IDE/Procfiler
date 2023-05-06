﻿using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Processes;

public readonly struct DotnetProcessLauncherDto
{
  public required string PathToDotnetExecutable { get; init; }
  public required string Arguments { get; init; }
  public required bool RedirectOutput { get; init; }
}

public interface IDotnetProcessLauncher
{
  Process? TryStartDotnetProcess(DotnetProcessLauncherDto launcherDto);
}

[AppComponent]
public class DotnetProcessLauncher : IDotnetProcessLauncher
{
  private readonly IProcfilerLogger myLogger;

  
  public DotnetProcessLauncher(IProcfilerLogger logger)
  {
    myLogger = logger;
  }
  

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
        ["CORECLR_ENABLE_PROFILING"] = "1",
        ["CORECLR_PROFILER"] = "{585022b6-31e9-4ddf-b35d-3c256d0a16f3}",
        ["CORECLR_PROFILER_PATH"] = @"C:\Users\aeroo\Desktop\Programming\pmide\Procfiler\src\cpp\cmake-build-release-visual-studio\Release\Procfiler.dll",
        ["DOTNET_DefaultDiagnosticPortSuspend"] = "1",
        ["PROCFILER_ENABLE_CONSOLE_LOGGING"] = "1"
      }
    };

    var process = new Process
    {
      StartInfo = startInfo,
    };

    if (!process.Start())
    {
      myLogger.LogError("Failed to start process {Path}", launcherDto.PathToDotnetExecutable);
      return null;
    }

    return process;
  }
}