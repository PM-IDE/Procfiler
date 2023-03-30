using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Processes;

public interface IProcessLauncher
{
  Process? TryStartDotnetProcess(string pathToExecutable);
}

[AppComponent]
public class ProcessLauncher : IProcessLauncher
{
  private readonly IProcfilerLogger myLogger;

  
  public ProcessLauncher(IProcfilerLogger logger)
  {
    myLogger = logger;
  }
  

  public Process? TryStartDotnetProcess(string pathToExecutable)
  {
    var startInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      WorkingDirectory = Path.GetDirectoryName(pathToExecutable),
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true,
      Arguments = pathToExecutable,
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
      myLogger.LogError("Failed to start process {Path}", pathToExecutable);
      return null;
    }

    return process;
  }
}