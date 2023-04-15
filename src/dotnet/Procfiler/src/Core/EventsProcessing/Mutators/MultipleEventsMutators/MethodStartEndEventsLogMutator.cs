using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.MultipleEventsMutators;

public interface IMethodStartEndEventsLogMutator : IMultipleEventsMutator
{
}

[EventMutator(MultipleEventMutatorsPasses.MethodStartEndInserter)]
public class MethodStartEndEventsLogMutator : IMethodStartEndEventsLogMutator
{
  private record StackFrameInfo(string Frame, EventRecordWithMetadata StartNode)
  {
    public string Frame { get; private set; } = Frame;


    public void UpdateFrame(string newFrame)
    {
      Frame = newFrame;
      StartNode.Metadata[TraceEventsConstants.ProcfilerMethodName] = newFrame;
    }
  }

  
  private readonly IProcfilerEventsFactory myEventsFactory;
  private readonly IProcfilerLogger myLogger;


  public IEnumerable<EventLogMutation> Mutations { get; }

  
  public MethodStartEndEventsLogMutator(IProcfilerEventsFactory eventsFactory, IProcfilerLogger logger)
  {
    myEventsFactory = eventsFactory;
    myLogger = logger;
    Mutations = new[]
    {
      new AddEventMutation(TraceEventsConstants.ProcfilerMethodStart),
      new AddEventMutation(TraceEventsConstants.ProcfilerMethodEnd),
    };
  }


  public void Process(IEventsCollection events, SessionGlobalData context)
  {
    if (events.Count == 0) return;
    
    var currentFrames = new List<StackFrameInfo>();
    events.ApplyNotPureActionForAllEvents((ptr, eventRecord) =>
    {
      if (!eventRecord.IsMethodStartEndProvider()) return false;
      if (!context.Stacks.TryGetValue(eventRecord.StackTraceId, out var stack)) return false;
      
      if (currentFrames.Count == 0)
      {
        InitializeCurrentStack(currentFrames, stack, events, ptr, eventRecord);
        return false;
      }

      var indexOfFirstDifferentFrame = GetIndexWhereStacksDiffer(currentFrames, stack);
      switch (indexOfFirstDifferentFrame)
      {
        case -1 when currentFrames.Count == stack.Frames.Length:
          break;

        case -1 when currentFrames.Count > stack.Frames.Length:
          PopEventsFromCurrentStack(currentFrames, events, ptr, eventRecord, indexOfFirstDifferentFrame);
          break;

        case -1:
          AddFramesFromEventToCurrentStack(currentFrames, stack, events, ptr, eventRecord);
          break;

        default:
          PopEventsFromCurrentStack(currentFrames, events, ptr, eventRecord, indexOfFirstDifferentFrame);
          AddFramesFromEventToCurrentStack(currentFrames, stack, events, ptr, eventRecord);
          break;
      }

      return false;
    });

    while (currentFrames.Count != 0)
    {
      var info = currentFrames[^1];
      currentFrames.RemoveAt(currentFrames.Count - 1);
      var lastPointer = events.Last;
      if (lastPointer is null)
      {
        myLogger.LogWarning("The last event somehow was null");
        return;
      }
      
      var lastEvent = events.TryGetForWithDeletionCheck(lastPointer.Value);
      Debug.Assert(lastEvent is { });

      events.InsertAfter(lastPointer.Value, CreateMethodEndEvent(lastEvent, info.Frame));
    }
  }

  private static void TryFixExistingStackTrace(Stack<StackFrameInfo> currentFrames, string[] eventFrames)
  {
    Debug.Assert(currentFrames.Count == eventFrames.Length);
    
    var index = 0;
    foreach (var stackFrameInfo in currentFrames)
    {
      var eventFrame = eventFrames[index++];
      if (UpdateMethodStartFrameIfNeeded(stackFrameInfo, eventFrame))
      {
        continue;
      }

      Debug.Assert(StackFramesEqual(stackFrameInfo.Frame, eventFrame));
    }
  }

  private static bool UpdateMethodStartFrameIfNeeded(StackFrameInfo methodStart, string candidateFrame)
  {
    if (methodStart.Frame is TraceEventsConstants.UndefinedMethod &&
        candidateFrame is not TraceEventsConstants.UndefinedMethod)
    {
      methodStart.UpdateFrame(candidateFrame);
      return true;
    }

    return false;
  }

  private void InitializeCurrentStack(
    List<StackFrameInfo> currentFrames,
    StackTraceInfo stack, 
    IEventsCollection events,
    EventPointer pointer,
    EventRecordWithMetadata current)
  {
    for (var i = stack.Frames.Length - 1; i >= 0; i--)
    {
      var frame = stack.Frames[i];
      var methodStartEvent = CreateMethodStartEvent(current, frame);
      currentFrames.Add(new StackFrameInfo(frame, methodStartEvent));

      events.InsertBefore(pointer, methodStartEvent);
    }
  }

  private static int GetIndexWhereStacksDiffer(List<StackFrameInfo> currentFrames, StackTraceInfo stack)
  {
    var indexOfFirstDifferentFrame = -1;
    var index = 0;
    foreach (var currentFrame in currentFrames)
    {
      if (index >= stack.Frames.Length)
      {
        indexOfFirstDifferentFrame = index;
        break;
      }

      if (!StackFramesEqual(currentFrame.Frame, stack.Frames[stack.Frames.Length - 1 - index]))
      {
        indexOfFirstDifferentFrame = index;
        break;
      }

      ++index;
    }

    return indexOfFirstDifferentFrame;
  }

  private static bool StackFramesEqual(string firstFrame, string secondFrame)
  {
    if (firstFrame is TraceEventsConstants.UndefinedMethod ||
        secondFrame is TraceEventsConstants.UndefinedMethod)
    {
      return true;
    }
      
    return firstFrame == secondFrame;
  }

  private void PopEventsFromCurrentStack(
    List<StackFrameInfo> currentFrames,
    IEventsCollection events,
    EventPointer current,
    EventRecordWithMetadata eventRecord,
    int indexOfFirstDifferentFrame)
  {
    while (currentFrames.Count != indexOfFirstDifferentFrame)
    {
      var lastFrame = currentFrames[^1];
      currentFrames.RemoveAt(currentFrames.Count - 1);
      events.InsertBefore(current, CreateMethodEndEvent(eventRecord, lastFrame.Frame));
    }
  }

  private EventRecordWithMetadata CreateMethodEndEvent(EventRecordWithMetadata @event, string frame)
  {
    return myEventsFactory.CreateMethodEndEvent(CreateContext(@event), frame);
  }
  
  private static EventsCreationContext CreateContext(EventRecord.EventRecord @event) => 
    new(@event.Stamp, @event.ManagedThreadId, -1);
  
  private EventRecordWithMetadata CreateMethodStartEvent(EventRecordWithMetadata @event, string frame)
  {
    return myEventsFactory.CreateMethodStartEvent(CreateContext(@event), frame);
  }

  private void AddFramesFromEventToCurrentStack(
    List<StackFrameInfo> currentFrames, 
    StackTraceInfo stack, 
    IEventsCollection events,
    EventPointer current,
    EventRecordWithMetadata contextEvent)
  {
    for (var index = stack.Frames.Length - 1 - currentFrames.Count; index >= 0; --index)
    {
      var frameToAdd = stack.Frames[index];
      var methodStartEvent = CreateMethodStartEvent(contextEvent, frameToAdd);
      currentFrames.Add(new StackFrameInfo(frameToAdd, methodStartEvent));
      
      events.InsertBefore(current, methodStartEvent);
    }
  }
}