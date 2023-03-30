using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core.InstrumentalProfiler;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Processes.Build;

[AppComponent]
public class DotnetProjectBuilderImpl : IDotnetProjectBuilder
{
  private readonly IProcfilerLogger myLogger;
  private readonly IDllMethodsPatcher myDllMethodsPatcher;


  public DotnetProjectBuilderImpl(IProcfilerLogger logger, IDllMethodsPatcher dllMethodsPatcher)
  {
    myLogger = logger;
    myDllMethodsPatcher = dllMethodsPatcher;
  }


  public BuildResult? TryBuildDotnetProject(ProjectBuildInfo projectBuildInfo)
  {
    var (pathToCsproj, tfm, configuration, instrumentationKind) = projectBuildInfo;
    var projectName = Path.GetFileNameWithoutExtension(pathToCsproj);
    using var _ = new PerformanceCookie($"Building::{projectName}", myLogger);
    
    var projectDirectory = Path.GetDirectoryName(pathToCsproj);
    Debug.Assert(projectDirectory is { });

    var artifactsFolderCookie = CreateTempArtifactsPath();
    var buildConfig = BuildConfigurationExtensions.ToString(configuration);
    var startInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      WorkingDirectory = projectDirectory,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true,
      Arguments = $"build {pathToCsproj} -c {buildConfig} -f {tfm} -o {artifactsFolderCookie.FolderPath} --self-contained true"
    };

    var process = new Process
    {
      StartInfo = startInfo
    };

    try
    {
      if (!process.Start())
      {
        myLogger.LogError("Failed to start build process for {PathToCsproj}", pathToCsproj);
        artifactsFolderCookie.Dispose();
        return null;
      }

      process.WaitForExit();

      if (process.ExitCode != 0)
      {
        var output = process.StandardOutput.ReadToEnd();
        myLogger.LogError("Failed to build project {Path}, {Output}", pathToCsproj, output);
        artifactsFolderCookie.Dispose();
        return null;
      }
    }
    catch (Exception ex)
    {
      artifactsFolderCookie.Dispose();
      myLogger.LogError(ex, "Failed to build project {CsprojPath}", pathToCsproj);
      return null;
    }
    
    var pathToDll = Path.Combine(artifactsFolderCookie.FolderPath, projectName + ".dll");
    
    myDllMethodsPatcher.PatchMethodStartEnd(pathToDll, instrumentationKind);
    
    return new BuildResult(artifactsFolderCookie)
    {
      BuiltDllPath = pathToDll
    };
  }

  private TempFolderCookie CreateTempArtifactsPath() => new(myLogger);
}