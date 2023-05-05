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
    var resultNullable = TryBuildDotnetProjectInternal(projectBuildInfo);
    if (resultNullable is not { } result) return null;

    if (projectBuildInfo.InstrumentationKind is not InstrumentationKind.None)
    {
      var procfilerDirectory = Environment.CurrentDirectory;
      const string ProcfilerEventSourceDllName = $"{InstrumentalProfilerConstants.ProcfilerEventSource}.dll";
      
      var procfilerEventSourceDll = Directory
        .GetFiles(procfilerDirectory)
        .FirstOrDefault(f => f.EndsWith(ProcfilerEventSourceDllName));

      if (procfilerEventSourceDll is null)
      {
        throw new FileNotFoundException($"Failed to find {InstrumentalProfilerConstants.ProcfilerEventSource}.dll");
      }

      var from = Path.Combine(Environment.CurrentDirectory, procfilerEventSourceDll);
      var buildResultDirName = Path.GetDirectoryName(result.BuiltDllPath);
      Debug.Assert(buildResultDirName is { });
      
      var to = Path.Combine(buildResultDirName, ProcfilerEventSourceDllName);
      File.Copy(from, to, true);
      
      myDllMethodsPatcher.PatchMethodStartEndAsync(result.BuiltDllPath, projectBuildInfo.InstrumentationKind);
    }

    return result;
  }

  private BuildResult? TryBuildDotnetProjectInternal(ProjectBuildInfo projectBuildInfo)
  {
    var (pathToCsproj, tfm, configuration, _, removeTempPath, tempPath, selfContained) = projectBuildInfo;
    var projectName = Path.GetFileNameWithoutExtension(pathToCsproj);
    using var _ = new PerformanceCookie($"Building::{projectName}", myLogger);
    
    var projectDirectory = Path.GetDirectoryName(pathToCsproj);
    Debug.Assert(projectDirectory is { });

    var artifactsFolderCookie = tempPath switch
    {
      null => CreateTempArtifactsPath(),
      { } => new TempFolderCookie(myLogger, tempPath)
    };
    
    var buildConfig = BuildConfigurationExtensions.ToString(configuration);
    var startInfo = new ProcessStartInfo
    {
      FileName = "dotnet",
      WorkingDirectory = projectDirectory,
      RedirectStandardOutput = true,
      RedirectStandardError = true,
      CreateNoWindow = true,
      Environment =
      {
        ["DOTNET_DefaultDiagnosticPortSuspend"] = "0"
      },
      Arguments = $"build {pathToCsproj} -c {buildConfig} -f {tfm} -o {artifactsFolderCookie.FolderPath} --self-contained {selfContained}"
    };

    var process = new Process
    {
      StartInfo = startInfo
    };

    void RemoveArtifactsFolderIfNeeded()
    {
      if (removeTempPath)
      {
        artifactsFolderCookie.Dispose();
      }
    }
    
    try
    {
      if (!process.Start())
      {
        myLogger.LogError("Failed to start build process for {PathToCsproj}", pathToCsproj);
        RemoveArtifactsFolderIfNeeded();
        return null;
      }

      process.WaitForExit(TimeSpan.FromSeconds(5));

      if (process.ExitCode != 0)
      {
        var output = process.StandardOutput.ReadToEnd();
        myLogger.LogError("Failed to build project {Path}, {Output}", pathToCsproj, output);
        RemoveArtifactsFolderIfNeeded();
        return null;
      }
    }
    catch (Exception ex)
    {
      RemoveArtifactsFolderIfNeeded();
      var output = process.StandardOutput.ReadToEnd();
      myLogger.LogError(ex, "Failed to build project {Path}, {Output}", pathToCsproj, output);
      return null;
    }
    
    var pathToDll = Path.Combine(artifactsFolderCookie.FolderPath, projectName + ".dll");
    return new BuildResult(artifactsFolderCookie)
    {
      BuiltDllPath = pathToDll
    };
  }

  private TempFolderCookie CreateTempArtifactsPath() => new(myLogger);
}