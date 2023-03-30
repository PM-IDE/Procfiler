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

  
  public abstract ValueTask ExecuteAsync(CollectClrEventsContext context);

  protected ValueTask ExecuteCommandAsync(CollectClrEventsContext context, Func<CollectedEvents, ValueTask> func)
  {
    using var performanceCookie = new PerformanceCookie($"{GetType().Name}::{nameof(ExecuteCommandAsync)}", Logger);
    ClearPathBeforeProfilingIfNeeded(context.CommonContext);
    
    return myCommandExecutor.Execute(context, func);
  }

  private void ClearPathBeforeProfilingIfNeeded(CollectingClrEventsCommonContext commonContext)
  {
    if (!commonContext.ClearPathBefore) return;
    
    PathUtils.ClearPath(commonContext.OutputPath, Logger);
  }
}