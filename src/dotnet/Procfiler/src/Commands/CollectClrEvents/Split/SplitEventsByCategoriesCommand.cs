using Procfiler.Commands.CollectClrEvents.Base;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Core.Serialization;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Commands.CollectClrEvents.Split;

public interface ISplitEventsByCategoriesCommand : ICommandWithContext<CollectClrEventsContext>
{
}

[CommandLineCommand]
public class SplitEventsByCategoriesCommand : CollectAndSplitCommandBase<string>, ISplitEventsByCategoriesCommand
{
  public SplitEventsByCategoriesCommand(
    ICommandExecutorDependantOnContext commandExecutor,
    IUnitedEventsProcessor unitedEventsProcessor,
    IUndefinedThreadsEventsMerger eventsMerger,
    IStackTraceSerializer stackTraceSerializer,
    IDelegatingEventsSerializer delegatingEventsSerializer, 
    IProcfilerLogger logger) 
    : base(logger, commandExecutor, eventsMerger, unitedEventsProcessor, delegatingEventsSerializer, stackTraceSerializer)
  {
  }


  public override void Execute(CollectClrEventsContext context) => 
    ExecuteSimpleSplitCommand(context, SplitEventsHelper.EventClassKeyExtractor, CollectAndSplitContext.DoNothing);

  protected override Command CreateCommandInternal() => 
    new("split-by-names", "Split the events into different files based on managed thread ID");
}