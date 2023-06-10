using Procfiler.Commands.CollectClrEvents.Base;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core;
using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Core.Serialization.XES;
using Procfiler.Core.SplitByMethod;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Commands.CollectClrEvents.Split;

public interface ISplitEventsByMethodCommand : ICommandWithContext<CollectClrEventsContext>
{
}

public enum InlineMode
{
  NotInline,
  
  OnlyEvents,
  EventsAndMethodsEvents,
  EventsAndMethodsEventsWithFilter
}

[CommandLineCommand]
public class SplitEventsByMethodCommand : CollectCommandBase, ISplitEventsByMethodCommand
{
  private readonly IUnitedEventsProcessor myUnitedEventsProcessor;
  private readonly IXesEventsSerializer myXesEventsSerializer;
  private readonly IByMethodsSplitter mySplitter;
  private readonly IFullMethodNameBeautifier myMethodNameBeautifier;

  private Option<bool> GroupAsyncMethods { get; } =
    new("--group-async-methods", static () => true, "Group events from async methods");

  private Option<string> FilterOption { get; } = new("--filter", static () => string.Empty, "Regex to filter");

  private Option<InlineMode> InlineInnerMethodsCalls { get; } = 
    new("--inline", static () => InlineMode.NotInline, "Should we inline inner methods calls to all previous traces");


  public SplitEventsByMethodCommand(
    ICommandExecutorDependantOnContext commandExecutor,
    IUndefinedThreadsEventsMerger undefinedThreadsEventsMerger,
    IUnitedEventsProcessor unitedEventsProcessor,
    IXesEventsSerializer xesEventsSerializer,
    IByMethodsSplitter splitter,
    IFullMethodNameBeautifier methodNameBeautifier,
    IProcfilerLogger logger, 
    IManagedEventsFromUndefinedThreadExtractor managedEventsExtractor, 
    IAsyncMethodsGrouper asyncMethodsGrouper)
    : base(logger, commandExecutor)
  {
    myUnitedEventsProcessor = unitedEventsProcessor;
    myXesEventsSerializer = xesEventsSerializer;
    mySplitter = splitter;
    myMethodNameBeautifier = methodNameBeautifier;
  }


  public override async ValueTask ExecuteAsync(CollectClrEventsContext context)
  {
    using var _ = new PerformanceCookie("SplittingEventsByMethods", Logger);

    var directory = context.CommonContext.OutputPath;
    var xesSerializer = new MergingTracesXesSerializer(myXesEventsSerializer, Logger);
    var parseResult = context.CommonContext.CommandParseResult;
    var mergeUndefinedThreadEvents = parseResult.TryGetOptionValue(MergeFromUndefinedThread);

    await ExecuteCommandAsync(context, (events, lifetime) =>
    {
      var (allEvents, globalData) = events;
      var processingContext = EventsProcessingContext.DoEverything(allEvents, globalData);
      myUnitedEventsProcessor.ProcessFullEventLog(processingContext);
      
      var filterPattern = GetFilterPattern(context.CommonContext);
      var inlineInnerCalls = parseResult.TryGetOptionValue(InlineInnerMethodsCalls);
      var addAsyncMethods = parseResult.TryGetOptionValue(GroupAsyncMethods);
      
      var tracesByMethods = mySplitter.Split(
        events, lifetime, filterPattern, inlineInnerCalls, mergeUndefinedThreadEvents, addAsyncMethods);

      foreach (var (methodName, traces) in tracesByMethods)
      {
        if (methodName is TraceEventsConstants.UndefinedMethod) continue;

        var eventsByMethodsInvocation = PrepareEventSessionInfo(traces, globalData);
        var filePath = GetFileNameForMethod(directory, methodName);
        foreach (var (_, sessionInfo) in eventsByMethodsInvocation)
        {
          xesSerializer.AddTrace(filePath, sessionInfo);
        }
      }

      return ValueTask.CompletedTask;
    });
    
    xesSerializer.SerializeAll();
  }

  private string GetFilterPattern(CollectingClrEventsCommonContext context)
  {
    if (context.CommandParseResult.TryGetOptionValue(FilterOption) is { } pattern) return pattern;
    
    return string.Empty;
  }
  
  private string GetFileNameForMethod(string directory, string methodName)
  {
    var fileName = myMethodNameBeautifier.Beautify(methodName);
    return Path.Combine(directory, $"{fileName}.xes");
  }

  private Dictionary<int, EventSessionInfo> PrepareEventSessionInfo(
    IEnumerable<IReadOnlyList<EventRecordWithMetadata>> traces, 
    SessionGlobalData mergedGlobalData)
  {
    var index = 0;
    return traces.ToDictionary(
      _ => index++,
      values =>
      {
        var collection = new EventsCollectionImpl(values.ToArray(), Logger);
        return new EventSessionInfo(new [] { collection }, mergedGlobalData);
      });
  }

  protected override Command CreateCommandInternal()
  {
    const string CommandName = "split-by-methods";
    const string CommandDescription = "Splits the events by methods, in which they occured, and serializes to XES"; 
    
    var splitByMethodsCommand = new Command(CommandName, CommandDescription);
    
    splitByMethodsCommand.AddOption(RepeatOption);
    splitByMethodsCommand.AddOption(FilterOption);
    splitByMethodsCommand.AddOption(InlineInnerMethodsCalls);
    splitByMethodsCommand.AddOption(GroupAsyncMethods);
    
    return splitByMethodsCommand;
  }
}