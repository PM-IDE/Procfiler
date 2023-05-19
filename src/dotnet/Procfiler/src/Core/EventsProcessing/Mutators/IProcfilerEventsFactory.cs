using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventRecord;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators;

public readonly record struct EventsCreationContext(long Stamp, int ManagedThreadId)
{
  public static EventsCreationContext CreateWithUndefinedStackTrace(EventRecord.EventRecord record) =>
    new(record.Stamp, record.ManagedThreadId);
}

public interface IProcfilerEventsFactory
{
  EventRecordWithMetadata CreateMethodStartEvent(EventsCreationContext context, string methodName);
  EventRecordWithMetadata CreateMethodEndEvent(EventsCreationContext context, string methodName);
  EventRecordWithMetadata CreateMethodExecutionEvent(EventsCreationContext context, string methodName);
}

[AppComponent]
public class ProcfilerEventsFactory : IProcfilerEventsFactory
{
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
}