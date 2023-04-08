using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Utils;

namespace Procfiler.Core.EventRecord;

public record StackTraceInfo(int StackTraceId, int ManagedThreadId, string[] Frames)
{
  protected virtual bool PrintMembers(StringBuilder builder)
  {
    builder
      .LogPrimitiveValue(nameof(StackTraceId), StackTraceId)
      .Append(StringBuilderExtensions.SerializeValue(Frames));

    return true;
  }

  public override int GetHashCode()
  {
    if (Frames.Length == 0) return ManagedThreadId;

    var hash = Frames[0].AsSpan().CalculateHash();

    for (var i = 1; i < Frames.Length; ++i)
    {
      hash = HashCode.Combine(hash, Frames[i].AsSpan().CalculateHash());
    }

    return HashCode.Combine(hash, ManagedThreadId);
  }
}

public class EventRecord
{
  public long Stamp { get; }
  public string EventClass { get; }
  public int ManagedThreadId { get; }
  public int StackTraceId { get; }
  public Guid ActivityId { get; }
  public string EventName { get; set; }
  public bool IsDeleted { get; set; }


  public EventRecord(long stamp, string eventClass, int managedThreadId, int stackTraceId, Guid activityId)
  {
    ActivityId = activityId;
    Stamp = stamp;
    EventClass = eventClass;
    ManagedThreadId = managedThreadId;
    StackTraceId = stackTraceId;
    EventName = EventClass;
    IsDeleted = false;
  }

  public EventRecord(TraceEvent @event, int managedThreadId, int stackTraceId)
    : this(@event.TimeStamp.ToUniversalTime().Ticks, @event.EventName, managedThreadId, stackTraceId, @event.ActivityID)
  {
  }
}

public class EventRecordWithMetadata : EventRecord
{
  public IEventMetadata Metadata { get; }

  
  public EventRecordWithMetadata(TraceEvent @event, int managedThreadId, int stackTraceId) 
    : base(@event, managedThreadId, stackTraceId)
  {
    Metadata = new EventMetadata(@event);
  }

  public EventRecordWithMetadata(
    long stamp, string eventClass, int managedThreadId, int stackTraceId, IEventMetadata metadata)
    : base(stamp, eventClass, managedThreadId, stackTraceId, Guid.Empty)
  {
    Metadata = metadata;
  }
}

public static class EventRecordExtensions
{
  public readonly record struct MethodStartEndEventInfo(string Frame, bool IsStart);


  public static bool IsMethodStartOrEndEvent(this EventRecordWithMetadata eventRecord) =>
    eventRecord.EventClass is TraceEventsConstants.ProcfilerMethodStart or TraceEventsConstants.ProcfilerMethodEnd;


  public static MethodStartEndEventInfo GetMethodStartEndEventInfo(this EventRecordWithMetadata eventRecord)
    => eventRecord.TryGetMethodStartEndEventInfo() ?? throw new ArgumentOutOfRangeException();
  
  public static MethodStartEndEventInfo? TryGetMethodStartEndEventInfo(this EventRecordWithMetadata eventRecord)
  {
    if (IsMethodStartOrEndEvent(eventRecord))
    {
      return new MethodStartEndEventInfo(
        Frame: eventRecord.Metadata[TraceEventsConstants.ProcfilerMethodName],
        IsStart: eventRecord.EventClass is TraceEventsConstants.ProcfilerMethodStart
      );
    }

    return null;
  }

  public static bool IsTaskRelatedEvent(this EventRecordWithMetadata eventRecord)
  {
    return eventRecord.EventClass.StartsWith(TraceEventsConstants.TaskCommonPrefix) ||
           eventRecord.EventClass.StartsWith(TraceEventsConstants.AwaitCommonPrefix);
  }

  public static bool IsTaskWaitSendOrStopEvent(this EventRecordWithMetadata eventRecord)
  {
    return eventRecord.EventClass is TraceEventsConstants.TaskWaitSend or TraceEventsConstants.TaskWaitStop;
  }
  
  public static bool IsTaskWaitStopEvent(this EventRecordWithMetadata eventRecord, out int waitedTaskId)
  {
    waitedTaskId = -1;
    
    if (eventRecord.EventClass is not TraceEventsConstants.TaskWaitStop) return false;

    waitedTaskId = ExtractTaskId(eventRecord);
    return true;
  }

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  private static int ExtractTaskId(EventRecordWithMetadata eventRecord)
  {
    return int.Parse(eventRecord.Metadata[TraceEventsConstants.TaskId]);
  }

  public static bool IsTaskWaitSendEvent(this EventRecordWithMetadata eventRecord, out int scheduledTaskId)
  {
    scheduledTaskId = -1;
    if (eventRecord.EventClass is not TraceEventsConstants.TaskWaitSend) return false;

    scheduledTaskId = ExtractTaskId(eventRecord);
    return true;
  }

  public static bool IsAwaitContinuationScheduled(this EventRecordWithMetadata eventRecord, out int scheduledTaskId)
  {
    scheduledTaskId = -1;
    if (eventRecord.EventClass is not TraceEventsConstants.AwaitTaskContinuationScheduledSend) return false;

    scheduledTaskId = int.Parse(eventRecord.Metadata[TraceEventsConstants.OriginatingTaskId]);
    return true;
  }

  public static bool IsGcSampledObjectAlloc(
    this EventRecordWithMetadata eventRecord, [NotNullWhen(true)] out string? typeName)
  {
    typeName = null;
    if (eventRecord.EventClass is not TraceEventsConstants.GcSampledObjectAllocation) return false;
    
    typeName = eventRecord.Metadata[TraceEventsConstants.CommonTypeName];
    return true;
  }

  public static bool IsMethodStartEndProvider(this EventRecordWithMetadata eventRecord)
  {
    return eventRecord.EventClass is not (TraceEventsConstants.GcSetGcHandle or TraceEventsConstants.GcDestroyGcHandle);
  }
}