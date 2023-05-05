using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Processes;

public interface IDotnetProcessLauncher
{
  Process? TryStartDotnetProcess(string pathToExecutable, string arguments);
}

[AppComponent]
public class DotnetDotnetProcessLauncher : IDotnetProcessLauncher
{
  private readonly IProcfilerLogger myLogger;

  
  public DotnetDotnetProcessLauncher(IProcfilerLogger logger)
  {
    myLogger = logger;
  }
  

  public Process? TryStartDotnetProcess(string pathToExecutable, string arguments)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      WorkingDirectory = Path.GetDirectoryName(pathToExecutable),
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true,
      Arguments = $"{pathToExecutable} {arguments}",
      Environment =
      {
        ["DOTNET_DefaultDiagnosticPortSuspend"] = "1"
      }
    };

    var process = new Process
    {
      StartInfo = startInfo,
    };

    if (!process.Start())
    {
      myLogger.LogError("Failed to start process {Path}", pathToExecutable);
      return null;
    }

    return process;
  }
}