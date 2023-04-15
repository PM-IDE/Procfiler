using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Utils;

namespace Procfiler.Core.Processes.Build;

public readonly struct BuildResult
{
  private readonly TempFolderCookie myTempFolderCookie;
  
  public required string BuiltDllPath { get; init; }

  
  public BuildResult(TempFolderCookie tempFolderCookie)
  {
    myTempFolderCookie = tempFolderCookie;
  }
  
  
  public void ClearUnderlyingFolder() => myTempFolderCookie.Dispose();
}

public interface IDotnetProjectBuilder
{
  BuildResult? TryBuildDotnetProject(ProjectBuildInfo projectBuildInfo);
}