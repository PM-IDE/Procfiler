using Procfiler.Commands.CollectClrEvents.Base;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.Collector;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.Serialization.XES;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Commands.CollectClrEvents;

[CommandLineCommand]
public class SerializeUndefinedThreadEventsToXesCommand(
  IProcfilerLogger logger,
  IXesEventsSerializer xesSerializer,
  IUnitedEventsProcessor unitedEventsProcessor,
  ICommandExecutorDependantOnContext commandExecutor
) : CollectCommandBase(logger, commandExecutor)
{
  public override void Execute(CollectClrEventsContext context)
  {
    var serializer = CreateSerializer(context);
    var outputPath = Path.Combine(context.CommonContext.OutputPath, "UndefinedEvents.xes");

    ExecuteCommand(context, collectedEvents =>
    {
      var (events, globalData) = collectedEvents;
      var extractor = SplitEventsHelper.ManagedThreadIdExtractor;
      var eventsByThreads = SplitEventsHelper.SplitByKey(Logger, events, extractor);
      var undefinedThreadEvents = eventsByThreads[-1];
      var processingContext = EventsProcessingContext.DoEverythingWithoutMethodStartEnd(undefinedThreadEvents, globalData);

      unitedEventsProcessor.ProcessFullEventLog(processingContext);
      var sessionInfo = new EventSessionInfo(new[] { undefinedThreadEvents }, globalData);

      serializer.WriteTrace(outputPath, sessionInfo);
    });
  }

  private INotStoringMergingTraceSerializer CreateSerializer(CollectClrEventsContext context)
  {
    var writeAllMetadata = context.CommonContext.WriteAllEventMetadata;

    return context.CommonContext.LogSerializationFormat switch
    {
      LogFormat.Xes => new NotStoringMergingTraceXesSerializer(xesSerializer, Logger, writeAllMetadata),
      LogFormat.Bxes => new NotStoringMergingTraceBxesSerializer(Logger, writeAllMetadata),
      _ => throw new ArgumentOutOfRangeException()
    };
  }

  protected override Command CreateCommandInternal()
  {
    const string Description = "Collect CLR events from undefined thread and serialize them to XES format";
    var command = new Command("undefined-events-to-xes", Description);
    command.AddOption(RepeatOption);
    return command;
  }
}