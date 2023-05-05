using Procfiler.Utils;
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
        ["DOTNET_DefaultDiagnosticPortSuspend"] = "1",
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