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
  IXesEventsSerializer xesEventsSerializer,
  IBxesEventsSerializer bxesEventsSerializer
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
    var writeAllEventMetadata = context.CommonContext.WriteAllEventMetadata;
    GetSerializer(context).SerializeEvents(sessionInfos, path, writeAllEventMetadata);
  }

  private IEventsSerializer GetSerializer(CollectClrEventsContext context)
  {
    return context.CommonContext.LogSerializationFormat switch
    {
      LogFormat.Xes => xesEventsSerializer,
      LogFormat.Bxes => bxesEventsSerializer,
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  protected override Command CreateCommandInternal()
  {
    var command = new Command("collect-to-xes", "Collect CLR events and serialize them to XES format");
    command.AddOption(RepeatOption);
    return command;
  }
}