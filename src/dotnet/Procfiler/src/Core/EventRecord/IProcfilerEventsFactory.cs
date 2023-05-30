using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventRecord;

public readonly record struct EventsCreationContext(long Stamp, long ManagedThreadId)
{
  public static EventsCreationContext CreateWithUndefinedStackTrace(EventRecord record) =>
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
  EventRecordWithMetadata CreateMethodEvent(FromFrameInfoCreationContext context);

  void FillExistingEventWith(FromFrameInfoCreationContext context, EventRecordWithMetadata existingEvent);
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
    EventsCreationContext context, string eventClass, string methodName)
  {
    var (stamp, managedThreadId) = context;
    var metadata = CreateMethodEventMetadata(methodName);

    return new EventRecordWithMetadata(stamp, eventClass, managedThreadId, metadata)
    {
      EventName = CreateMethodStartOrEndEventName(eventClass, methodName)
    };
  }

  private static string CreateMethodStartOrEndEventName(string eventName, string fqn)
  {
    return eventName + "_{" + MutatorsUtil.TransformMethodLikeNameForEventNameConcatenation(fqn) + "}";
  }

  private static void SetMethodNameInMetadata(IEventMetadata metadata, string fqn)
  {
    metadata[TraceEventsConstants.ProcfilerMethodName] = fqn;
  }

  private static IEventMetadata CreateMethodEventMetadata(string fqn)
  {
    var metadata = new EventMetadata();
    SetMethodNameInMetadata(metadata, fqn);
    return metadata;
  }

  public EventRecordWithMetadata CreateMethodEndEvent(EventsCreationContext context, string methodName)
  {
    return CreateMethodStartOrEndEvent(context, TraceEventsConstants.ProcfilerMethodEnd, methodName);
  }

  public EventRecordWithMetadata CreateMethodExecutionEvent(EventsCreationContext context, string methodName)
  {
    var (stamp, managedThreadId) = context;
    var metadata = CreateMethodEventMetadata(methodName);
    var name = CreateEventNameForMethodExecutionEvent(methodName);
    return new EventRecordWithMetadata(stamp, name, managedThreadId, metadata);
  }

  private static string CreateEventNameForMethodExecutionEvent(string fqn) => 
    $"{TraceEventsConstants.ProcfilerMethodExecution}_{fqn}";

  public EventRecordWithMetadata CreateMethodEvent(FromFrameInfoCreationContext context)
  {
    var fqn = ExtractMethodName(context);
    var creationContext = new EventsCreationContext(context.FrameInfo.TimeStamp, context.ManagedThreadId);
    return context.FrameInfo.IsStart switch
    {
      true => CreateMethodStartEvent(creationContext, fqn),
      false => CreateMethodEndEvent(creationContext, fqn)
    };
  }

  private string ExtractMethodName(FromFrameInfoCreationContext context)
  {
    var methodId = context.FrameInfo.FunctionId;
    if (!context.GlobalData.MethodIdToFqn.TryGetValue(methodId, out var fqn))
    {
      myLogger.LogWarning("Failed to get fqn for {FunctionId}", methodId);
      fqn = $"System.Undefined.{methodId}[instance.void..()]";
    }

    return fqn;
  }

  public void FillExistingEventWith(FromFrameInfoCreationContext context, EventRecordWithMetadata existingEvent)
  {
    existingEvent.UpdateWith(context);
    var fqn = ExtractMethodName(context);
    existingEvent.EventName = CreateMethodStartOrEndEventName(existingEvent.EventClass, fqn);
    SetMethodNameInMetadata(existingEvent.Metadata, fqn);
  }
}