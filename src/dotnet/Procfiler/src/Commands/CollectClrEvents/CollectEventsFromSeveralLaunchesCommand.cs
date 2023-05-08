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

public interface ICollectEventsFromSeveralLaunchesCommand : ICommandWithContext<CollectClrEventsContext>
{
}

[CommandLineCommand]
public class CollectEventsFromSeveralLaunchesCommand : CollectCommandBase, ICollectEventsFromSeveralLaunchesCommand
{
  private readonly IUnitedEventsProcessor myUnitedEventsProcessor;
  private readonly IXesEventsSerializer myXesEventsSerializer;


  public CollectEventsFromSeveralLaunchesCommand(
    IProcfilerLogger logger,
    ICommandExecutorDependantOnContext commandExecutor,
    IUnitedEventsProcessor unitedEventsProcessor,
    IXesEventsSerializer xesEventsSerializer)
    : base(logger, commandExecutor)
  {
    myUnitedEventsProcessor = unitedEventsProcessor;
    myXesEventsSerializer = xesEventsSerializer;
  }
  
  
  public override async ValueTask ExecuteAsync(CollectClrEventsContext context)
  {
    var sessionInfos = new List<EventSessionInfo>();
    
    await ExecuteCommandAsync(context, collectedEvents =>
    {
      var (events, globalData) = collectedEvents;
      var processingContext = EventsProcessingContext.DoEverythingWithoutMethodStartEnd(events, globalData);
      myUnitedEventsProcessor.ProcessFullEventLog(processingContext);
      var eventsByThreadIds = SplitEventsHelper.SplitByKey(Logger, events, SplitEventsHelper.ManagedThreadIdExtractor);
      var sessionInfo = new EventSessionInfo(eventsByThreadIds.Values, globalData);
      sessionInfos.Add(sessionInfo);
      return ValueTask.CompletedTask;
    });
    
    var path = context.CommonContext.OutputPath;
    await using var fs = File.OpenWrite(path);
    await myXesEventsSerializer.SerializeEventsAsync(sessionInfos, fs);
  }

  protected override Command CreateCommandInternal()
  {
    var command = new Command("collect-to-xes", "Collect CLR events and serialize them to XES format");
    command.AddOption(RepeatOption);
    return command;
  }
}