﻿namespace ProcfilerBuildTasks;

using System.Diagnostics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

[MeansImplicitUse]
public class BuildCppProcfiler : Task
{
  private const string BuildFolderName = "build";
  
  [Required] public string CppProcfilerFolderPath { get; set; } = null!;
  
  
  public override bool Execute()
  {
    try
    {
      return ExecuteInternal();
    }
    catch (Exception ex)
    {
      Log.LogError($"Failed to execute {GetType().Name}");
      Log.LogError(ex.Message);
      return false;
    }
  }

  private bool ExecuteInternal()
  {
    Log.LogMessage("Started building cpp Procfiler");
    if (!Directory.Exists(CppProcfilerFolderPath))
    {
      Log.LogError($"The provided path for cpp Procfiler does not exist: {CppProcfilerFolderPath}");
      return false;
    }

    if (!InitializeCmakeProject())
    {
      Log.LogError("Failed to initialize cmake project");
      return false;
    }
    
    if (!BuildCmakeProject())
    {
      Log.LogError("Failed to build cmake project");
      return false;
    }

    return true;
  }

  private bool BuildCmakeProject()
  {
    var buildDirectory = CreateBuildDirectoryPath();
    if (!Directory.Exists(buildDirectory))
    {
      Log.LogError($"The build directory ({buildDirectory}) does not exist");
      return false;
    }
    
    const string Name = "BuildingCmakeProject";
    return LaunchProcessAndWaitForExit(CreateBuildCmakeProjectProcess(), Name);
  }

  private Process CreateBuildCmakeProjectProcess() => new()
  {
    StartInfo = new ProcessStartInfo
    {
      FileName = FindCmakeExecutable(),
      Arguments = $"--build {CreateBuildDirectoryPath()} --target Procfiler --config Release",
      RedirectStandardError = true,
      RedirectStandardOutput = true,
      UseShellExecute = false,
      CreateNoWindow = true,
      WorkingDirectory = CreateBuildDirectoryPath()
    }
  };

  private string FindCmakeExecutable() => "cmake";
  
  private bool InitializeCmakeProject()
  {
    var buildDirectory = CreateBuildDirectoryPath();
    if (Directory.Exists(buildDirectory))
    {
      Directory.Delete(buildDirectory, true);
    }
    
    Directory.CreateDirectory(buildDirectory);
    
    const string Name = "InitializingCmakeProject";
    return LaunchProcessAndWaitForExit(CreateInitializeCmakeProject(), Name);
  }

  private string CreateBuildDirectoryPath() => Path.Combine(CppProcfilerFolderPath, BuildFolderName);

  private Process CreateInitializeCmakeProject() => new()
  {
    StartInfo = new ProcessStartInfo
    {
      FileName = FindCmakeExecutable(),
      WorkingDirectory = CreateBuildDirectoryPath(),
      RedirectStandardError = true,
      RedirectStandardOutput = true,
      CreateNoWindow = true,
      UseShellExecute = false,
      Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) switch
      {
        true => $"-S {CppProcfilerFolderPath} -DCMAKE_BUILD_TYPE=Release -G \"Visual Studio 17 2022\"",
        false => throw new PlatformNotSupportedException("The current OS is not yet supported")
      }
    }
  };

  private bool LaunchProcessAndWaitForExit(Process process, string name)
  {
    var timeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;

    if (!process.Start())
    {
      Log.LogError($"Failed to start the process {name}");
      return false;
    }
    
    if (!process.WaitForExit(timeout))
    {
      process.Kill();
      Log.LogError($"The process {name} has not finished in {timeout}ms");
      LogProcessOutput(process, name);
      return false;
    }

    var exitCode = process.ExitCode;
    if (exitCode != 0)
    {
      Log.LogError($"Process {name} exited with exit code {exitCode}");
      LogProcessOutput(process, name);
      return false;
    }

    return true;
  }

  private void LogProcessOutput(Process process, string name)
  {
    try
    {
      Log.LogMessage($"The process {name} output:");
      Log.LogMessage(process.StandardOutput.ReadToEnd());
      Log.LogMessage($"The process {name} errors:");
      Log.LogMessage(process.StandardError.ReadToEnd());
    }
    catch (Exception ex)
    {
      Log.LogError("Failed to log process output", ex.Message);
    }
  }
}