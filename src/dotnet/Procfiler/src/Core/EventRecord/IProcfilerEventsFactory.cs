using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventRecord;

public readonly record struct EventsCreationContext(long Stamp, long ManagedThreadId)
{
  public static EventsCreationContext CreateWithUndefinedStackTrace(Core.EventRecord.EventRecord record) =>
    new(record.Stamp, record.ManagedThreadId);
}

public readonly struct FromFrameInfoCreationContext
{
  public required FrameInfo FrameInfo { get; init; }
  public required SessionGlobalData GlobalData { get; init; }
  public required long ManagedThreadId { get; init; }
}

public interface IProcfilerEventsFactory
{
  EventRecordWithMetadata CreateMethodStartEvent(EventsCreationContext context, string methodName);
  EventRecordWithMetadata CreateMethodEndEvent(EventsCreationContext context, string methodName);
  EventRecordWithMetadata CreateMethodExecutionEvent(EventsCreationContext context, string methodName);
  EventRecordWithMetadata? TryCreateMethodEvent(FromFrameInfoCreationContext context);
}

[AppComponent]
public class ProcfilerEventsFactory : IProcfilerEventsFactory
{
  private readonly IProcfilerLogger myLogger;

  
  public ProcfilerEventsFactory(IProcfilerLogger logger)
  {
    myLogger = logger;
  }


  public EventRecordWithMetadata CreateMethodStartEvent(EventsCreationContext context, string methodName)
  {
    return CreateMethodStartOrEndEvent(context, TraceEventsConstants.ProcfilerMethodStart, methodName);
  }

  private static EventRecordWithMetadata CreateMethodStartOrEndEvent(
    EventsCreationContext context, string eventName, string methodName)
  {
    var (stamp, managedThreadId) = context;
    var metadata = CreateMetadataForMethodName(methodName);
    
    return new EventRecordWithMetadata(stamp, eventName, managedThreadId, metadata)
    {
      EventName = eventName + "_{" + MutatorsUtil.TransformMethodLikeNameForEventNameConcatenation(methodName) + "}"
    };
  }
  
  private static EventMetadata CreateMetadataForMethodName(string methodName) => new()
  {
    [TraceEventsConstants.ProcfilerMethodName] = methodName
  };

  public EventRecordWithMetadata CreateMethodEndEvent(EventsCreationContext context, string methodName)
  {
    return CreateMethodStartOrEndEvent(context, TraceEventsConstants.ProcfilerMethodEnd, methodName);
  }

  public EventRecordWithMetadata CreateMethodExecutionEvent(EventsCreationContext context, string methodName)
  {
    var (stamp, managedThreadId) = context;
    var metadata = CreateMetadataForMethodName(methodName);
    var name = $"{TraceEventsConstants.ProcfilerMethodExecution}_{methodName}";
    return new EventRecordWithMetadata(stamp, name, managedThreadId, metadata);
  }
  
  public EventRecordWithMetadata? TryCreateMethodEvent(FromFrameInfoCreationContext context)
  {
    var methodId = context.FrameInfo.FunctionId;
    if (!context.GlobalData.MethodIdToFqn.TryGetValue(methodId, out var fqn))
    {
      myLogger.LogWarning("Failed to get fqn for {FunctionId}", methodId);
      fqn = methodId.ToString();
    }

    var creationContext = new EventsCreationContext(context.FrameInfo.TimeStamp, context.ManagedThreadId);
    return context.FrameInfo.IsStart switch
    {
      true => CreateMethodStartEvent(creationContext, fqn),
      false => CreateMethodEndEvent(creationContext, fqn)
    };
  }
}