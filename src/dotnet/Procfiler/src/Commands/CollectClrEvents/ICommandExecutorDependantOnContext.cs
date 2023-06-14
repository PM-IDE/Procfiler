using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.Processes;
using Procfiler.Core.Processes.Build;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Commands.CollectClrEvents;

public interface ICommandExecutorDependantOnContext
{
  void Execute(CollectClrEventsContext context, Action<CollectedEvents> commandAction);
}

[AppComponent]
public class CommandExecutorImpl : ICommandExecutorDependantOnContext
{
  private readonly IProcfilerLogger myLogger;
  private readonly IClrEventsCollector myClrEventsCollector;
  private readonly IDotnetProjectBuilder myProjectBuilder;
  private readonly IDotnetProcessLauncher myDotnetProcessLauncher;
  private readonly ICppProcfilerLocator myCppProcfilerLocator;
  private readonly IBinaryStackSavePathCreator myBinaryStackSavePathCreator;


  public CommandExecutorImpl(
    IDotnetProcessLauncher dotnetProcessLauncher, 
    IClrEventsCollector clrEventsCollector,
    IDotnetProjectBuilder projectBuilder,
    IProcfilerLogger logger, 
    ICppProcfilerLocator cppProcfilerLocator, 
    IBinaryStackSavePathCreator binaryStackSavePathCreator)
  {
    myDotnetProcessLauncher = dotnetProcessLauncher;
    myClrEventsCollector = clrEventsCollector;
    myProjectBuilder = projectBuilder;
    myLogger = logger;
    myCppProcfilerLocator = cppProcfilerLocator;
    myBinaryStackSavePathCreator = binaryStackSavePathCreator;
  }


  public void Execute(CollectClrEventsContext context, Action<CollectedEvents> commandAction)
  {
    switch (context)
    {
      case CollectClrEventsFromExeWithRepeatContext repeatContext:
        ExecuteCommandWithRetryExe(repeatContext, commandAction);
        return;
      case CollectClrEventsFromExeWithArguments argsContext:
        ExecuteCommandWithArgumentsList(argsContext, commandAction);
        return;
      case CollectClrEventsFromExeContext exeContext:
        ExecuteCommandWithLaunchingProcess(exeContext, commandAction);
        return;
      case CollectClrEventsFromRunningProcessContext runContext:
        ExecuteCommandWithRunningProcess(runContext, commandAction);
        return;
      default:
        throw new ArgumentOutOfRangeException(nameof(context), context, null);
    }
  }

  private void ExecuteCommandWithArgumentsList(
    CollectClrEventsFromExeWithArguments context, Action<CollectedEvents> commandAction)
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

      ExecuteCommandWithLaunchingProcess(newContext, commandAction);
    }
  }

  private void ExecuteCommandWithRetryExe(
    CollectClrEventsFromExeWithRepeatContext context, Action<CollectedEvents> commandAction)
  {
    for (var i = 0; i < context.RepeatCount; i++)
    {
      ExecuteCommandWithLaunchingProcess(context, commandAction);
    }
  }

  private void ExecuteCommandWithRunningProcess(
    CollectClrEventsFromRunningProcessContext context,
    Action<CollectedEvents> commandAction)
  {
    if (CollectEventsFromProcess(context, context.ProcessId, null) is var events)
    {
      commandAction(events);
    }
  }

  private CollectedEvents CollectEventsFromProcess(
    CollectClrEventsContext context, int processId, string? binaryStacksPath)
  {
    var (_, _, _, _, category, _, duration, timeout, _, _) = context.CommonContext;
    var collectionContext = new ClrEventsCollectionContextWithBinaryStacks(
      processId, duration, timeout, category, binaryStacksPath);

    return myClrEventsCollector.CollectEvents(collectionContext);
  }

  private void ExecuteCommandWithLaunchingProcess(
    CollectClrEventsFromExeContext context,
    Action<CollectedEvents> commandAction)
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
      var launcherDto = DotnetProcessLauncherDto.CreateFrom(
        context.CommonContext, buildResult, myCppProcfilerLocator, myBinaryStackSavePathCreator);
      
      if (myDotnetProcessLauncher.TryStartDotnetProcess(launcherDto) is not { } process)
      {
        myLogger.LogError("Failed to start or to find process");
        return;
      }

      myLogger.LogInformation("Started process: {Id} {Path}", process.Id, buildResult.BuiltDllPath);

      CollectedEvents? events = null;
      try
      {
        events = CollectEventsFromProcess(context, process.Id, launcherDto.BinaryStacksSavePath);
      }
      catch (Exception ex)
      {
        myLogger.LogError(ex, "Failed to collect events from {ProcessId}", process.Id);
      }
      finally
      {
        process.Kill();
        process.WaitForExit();
      }

      if (context.CommonContext.PrintProcessOutput)
      {
        var output = process.StandardOutput.ReadToEnd();
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
        commandAction(events.Value);
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