using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core.Collector;
using Procfiler.Core.Processes;
using Procfiler.Core.Processes.Build;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Commands.CollectClrEvents;

public interface ICommandExecutorDependantOnContext
{
  ValueTask Execute(CollectClrEventsContext context, Func<CollectedEvents, ValueTask> func);
}

[AppComponent]
public class CommandExecutorImpl : ICommandExecutorDependantOnContext
{
  private readonly IProcfilerLogger myLogger;
  private readonly IClrEventsCollector myClrEventsCollector;
  private readonly IDotnetProjectBuilder myProjectBuilder;
  private readonly IDotnetProcessLauncher myDotnetProcessLauncher;


  public CommandExecutorImpl(
    IDotnetProcessLauncher dotnetProcessLauncher, 
    IClrEventsCollector clrEventsCollector,
    IDotnetProjectBuilder projectBuilder,
    IProcfilerLogger logger)
  {
    myDotnetProcessLauncher = dotnetProcessLauncher;
    myClrEventsCollector = clrEventsCollector;
    myProjectBuilder = projectBuilder;
    myLogger = logger;
  }


  public ValueTask Execute(CollectClrEventsContext context, Func<CollectedEvents, ValueTask> func) =>
    context switch
    {
      CollectClrEventsFromExeWithRepeatContext repeatContext => ExecuteCommandWithRetryExe(repeatContext, func),
      CollectClrEventsFromExeWithArguments argsContext => ExecuteCommandWithArgumentsList(argsContext, func),
      CollectClrEventsFromExeContext exeContext => ExecuteCommandWithLaunchingProcess(exeContext, func),
      CollectClrEventsFromRunningProcessContext runContext => ExecuteCommandWithRunningProcess(runContext, func),
      _ => throw new ArgumentOutOfRangeException(nameof(context), context, null)
    };

  private async ValueTask ExecuteCommandWithArgumentsList(
    CollectClrEventsFromExeWithArguments context, Func<CollectedEvents,ValueTask> func)
  {
    foreach (var currentArguments in context.Arguments)
    {
      var newContext = context with
      {
        CommonContext = context.CommonContext with
        {
          Arguments = currentArguments
        }
      };

      await ExecuteCommandWithLaunchingProcess(newContext, func);
    }
  }

  private async ValueTask ExecuteCommandWithRetryExe(
    CollectClrEventsFromExeWithRepeatContext context, Func<CollectedEvents, ValueTask> func)
  {
    for (var i = 0; i < context.RepeatCount; i++)
    {
      await ExecuteCommandWithLaunchingProcess(context, func);
    }
  }

  private async ValueTask ExecuteCommandWithRunningProcess(
    CollectClrEventsFromRunningProcessContext context,
    Func<CollectedEvents, ValueTask> func)
  {
    if (await CollectEventsFromProcess(context, context.ProcessId) is { } events)
    {
      await func(events);
    }
  }

  private ValueTask<CollectedEvents> CollectEventsFromProcess(CollectClrEventsContext context, int processId)
  {
    var (_, _, _, _, category, _, duration, timeout, _) = context.CommonContext;
    return myClrEventsCollector.CollectEventsAsync(processId, duration, timeout, category);
  }

  private async ValueTask ExecuteCommandWithLaunchingProcess(
    CollectClrEventsFromExeContext context,
    Func<CollectedEvents, ValueTask> func)
  {
    var (pathToCsproj, tfm, _, _, clearTemp, _, _) = context.ProjectBuildInfo;
    var buildResultNullable = myProjectBuilder.TryBuildDotnetProject(context.ProjectBuildInfo);
    if (!buildResultNullable.HasValue)
    {
      myLogger.LogError("Failed to build dotnet project {Tfm}, {Path}", tfm, pathToCsproj);
      return;
    }

    var buildResult = buildResultNullable.Value;

    try
    {
      var launcherDto = new DotnetProcessLauncherDto
      {
        Arguments = context.CommonContext.Arguments,
        RedirectOutput = context.CommonContext.PrintProcessOutput,
        PathToDotnetExecutable = buildResult.BuiltDllPath
      };
      
      if (myDotnetProcessLauncher.TryStartDotnetProcess(launcherDto) is not { } process)
      {
        myLogger.LogError("Failed to start or to find process");
        return;
      }

      myLogger.LogInformation("Started process: {Id} {Path}", process.Id, buildResult.BuiltDllPath);

      CollectedEvents? events = null;
      try
      {
        events = await CollectEventsFromProcess(context, process.Id);
      }
      catch (Exception ex)
      {
        myLogger.LogError(ex, "Failed to collect events from {ProcessId}", process.Id);
      }
      finally
      {
        process.Kill();
        await process.WaitForExitAsync();
      }

      if (context.CommonContext.PrintProcessOutput)
      {
        var output = await process.StandardOutput.ReadToEndAsync();
        myLogger.LogInformation("Process output:");
        myLogger.LogInformation(output);
      }

      if (!process.HasExited)
      {
        myLogger.LogError("The process {Id} somehow didn't exit", process.Id);
      }
      else
      {
        const string Message = "The process {Id} ({Path}) which was created by Procfiler exited";
        myLogger.LogInformation(Message, process.Id, pathToCsproj); 
      }
    
      if (events.HasValue)
      {
        await func(events.Value);
      }
    }
    finally
    {
      if (clearTemp)
      {
        buildResult.ClearUnderlyingFolder();
      }
    }
  }
}