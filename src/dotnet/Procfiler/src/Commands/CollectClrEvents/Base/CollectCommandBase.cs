using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core;
using Procfiler.Core.Collector;
using Procfiler.Utils;

namespace Procfiler.Commands.CollectClrEvents.Base;

public abstract partial class CollectCommandBase : ICommandWithContext<CollectClrEventsContext>
{
  private readonly ICommandExecutorDependantOnContext myCommandExecutor;

  protected readonly IProcfilerLogger Logger;
  

  protected CollectCommandBase(IProcfilerLogger logger, ICommandExecutorDependantOnContext commandExecutor)
  {
    Logger = logger;
    myCommandExecutor = commandExecutor;
  }

  
  public abstract void Execute(CollectClrEventsContext context);

  protected void ExecuteCommand(CollectClrEventsContext context, Action<CollectedEvents> commandAction)
  {
    using var performanceCookie = new PerformanceCookie($"{GetType().Name}::{nameof(ExecuteCommand)}", Logger);
    ClearPathBeforeProfilingIfNeeded(context.CommonContext);
    
    myCommandExecutor.Execute(context, commandAction);
  }

  private void ClearPathBeforeProfilingIfNeeded(CollectingClrEventsCommonContext commonContext)
  {
    if (!commonContext.ClearPathBefore) return;
    
    PathUtils.ClearPath(commonContext.OutputPath, Logger);
  }
}