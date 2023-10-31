using Procfiler.Commands.CollectClrEvents.Base;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core;
using Procfiler.Core.Collector;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.Serialization.XES;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Commands.CollectClrEvents;

public interface ICollectEventsFromSeveralLaunchesCommand : ICommandWithContext<CollectClrEventsContext>;

[CommandLineCommand]
public class CollectEventsFromSeveralLaunchesCommand(
  IProcfilerLogger logger,
  ICommandExecutorDependantOnContext commandExecutor,
  IUnitedEventsProcessor unitedEventsProcessor,
  IXesEventsSerializer xesEventsSerializer
) : CollectCommandBase(logger, commandExecutor), ICollectEventsFromSeveralLaunchesCommand
{
  public override void Execute(CollectClrEventsContext context)
  {
    var sessionInfos = new List<EventSessionInfo>();

    ExecuteCommand(context, collectedEvents =>
    {
      var (events, globalData) = collectedEvents;
      var processingContext = EventsProcessingContext.DoEverythingWithoutMethodStartEnd(events, globalData);
      unitedEventsProcessor.ProcessFullEventLog(processingContext);
      var eventsByThreadIds = SplitEventsHelper.SplitByKey(Logger, events, SplitEventsHelper.ManagedThreadIdExtractor);
      var sessionInfo = new EventSessionInfo(eventsByThreadIds.Values, globalData);
      sessionInfos.Add(sessionInfo);
    });

    var path = context.CommonContext.OutputPath;
    using var fs = File.OpenWrite(path);

    var writeAllEventMetadata = context.CommonContext.WriteAllEventMetadata;
    xesEventsSerializer.SerializeEvents(sessionInfos, fs, writeAllEventMetadata);
  }

  protected override Command CreateCommandInternal()
  {
    var command = new Command("collect-to-xes", "Collect CLR events and serialize them to XES format");
    command.AddOption(RepeatOption);
    return command;
  }
}