using Procfiler.Commands.CollectClrEvents.Base;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core;
using Procfiler.Core.Collector;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Core.Serialization;
using Procfiler.Core.SplitByMethod;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Commands.CollectClrEvents.Split;

public interface ISplitEventsByManagedThreadIdCommand : ICommandWithContext<CollectClrEventsContext>
{
}

[CommandLineCommand]
public class SplitEventsByManagedThreadId : CollectAndSplitCommandBase<int>, ISplitEventsByManagedThreadIdCommand
{
  private readonly IManagedEventsFromUndefinedThreadExtractor myManagedEventsExtractor;

  
  public SplitEventsByManagedThreadId(
    IManagedEventsFromUndefinedThreadExtractor managedEventsExtractor,
    ICommandExecutorDependantOnContext commandExecutor,
    IUnitedEventsProcessor unitedEventsProcessor,
    IUndefinedThreadsEventsMerger eventsMerger,
    IStackTraceSerializer stackTraceSerializer,
    IDelegatingEventsSerializer delegatingEventsSerializer, 
    IProcfilerLogger logger) 
    : base(logger, commandExecutor, eventsMerger, unitedEventsProcessor, delegatingEventsSerializer, stackTraceSerializer)
  {
    myManagedEventsExtractor = managedEventsExtractor;
  }


  public override ValueTask ExecuteAsync(CollectClrEventsContext context)
  {
    var parseResult = context.CommonContext.CommandParseResult;
    var collectAndSplitContext = CollectAndSplitContext.DoEverything with
    {
      MergeFromUndefinedThread = parseResult.TryGetOptionValue(MergeFromUndefinedThread)
    };
    
    return ExecuteSimpleSplitCommand(context, SplitEventsHelper.ManagedThreadIdExtractor, collectAndSplitContext);
  }

  protected override KeyValuePair<int, IEventsCollection?>? FindEventsForUndefinedThread(
    Dictionary<int, IEventsCollection> eventsByKey)
  {
    var value = DictionaryExtensions.GetValueOrDefault(eventsByKey, -1);
    return new KeyValuePair<int, IEventsCollection?>(-1, value);
  }

  protected override IEventsCollection? AddNewManagedThreadsFromUndefined(
    IDictionary<int, IEventsCollection> events,
    SessionGlobalData globalData,
    IEventsCollection? undefinedEvents)
  {
    if (undefinedEvents is null) return undefinedEvents;
    
    UnitedEventsProcessor.ApplyMultipleMutators(undefinedEvents, globalData, EmptyCollections<Type>.EmptySet);
    return myManagedEventsExtractor.Extract(events, undefinedEvents);
  }

  protected override Command CreateCommandInternal() => 
    new("split-by-threads", "Split the events into different files based on managed thread ID");
}