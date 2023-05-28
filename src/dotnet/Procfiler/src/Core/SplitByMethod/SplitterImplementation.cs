using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Utils;

namespace Procfiler.Core.SplitByMethod;

public class SplitterImplementation
{
  private readonly record struct CurrentFrameInfo(
    string Frame,
    bool ShouldProcess,
    List<EventRecordWithMetadata> Events,
    EventRecordWithMetadata OriginalEvent
  );


  private readonly IProcfilerEventsFactory myEventsFactory;
  private readonly IEnumerable<EventRecordWithPointer> myEvents;
  private readonly bool myInlineEventsFromInnerMethods;
  private readonly Regex myFilterRegex;
  private readonly Dictionary<string, IReadOnlyList<IReadOnlyList<EventRecordWithMetadata>>> myResult;
  private readonly Stack<CurrentFrameInfo> myFramesStack;


  public SplitterImplementation(
    IProcfilerEventsFactory eventsFactory,
    IEnumerable<EventRecordWithPointer> events,
    string filterPattern,
    bool inlineEventsFromInnerMethods)
  {
    myEventsFactory = eventsFactory;
    myEvents = events;
    myInlineEventsFromInnerMethods = inlineEventsFromInnerMethods;
    myFilterRegex = new Regex(filterPattern);
    myResult = new Dictionary<string, IReadOnlyList<IReadOnlyList<EventRecordWithMetadata>>>();
    myFramesStack = new Stack<CurrentFrameInfo>();
  }


  public IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<EventRecordWithMetadata>>> Split()
  {
    foreach (var (_, eventRecord) in myEvents)
    {
      if (eventRecord.TryGetMethodStartEndEventInfo() is var (frame, isStartOfMethod))
      {
        if (isStartOfMethod)
        {
          ProcessStartOfMethod(frame, eventRecord);
          continue;
        }

        ProcessEndOfMethod(eventRecord);
        continue;
      }

      ProcessNormalEvent(eventRecord);
    }
    
    Debug.Assert(myFramesStack.Count == 0);
    return myResult;
  }

  private void ProcessStartOfMethod(string frame, EventRecordWithMetadata eventRecord)
  {
    var events = new List<EventRecordWithMetadata>();
    if (myInlineEventsFromInnerMethods)
    {
      events.Add(eventRecord);
      AddEventToAllFrames(eventRecord);
    }
    
    myFramesStack.Push(new CurrentFrameInfo(frame, ShouldProcess(frame), events, eventRecord));
  }

  private bool ShouldProcess(string frame) => myFilterRegex.IsMatch(frame);
  
  private void ProcessEndOfMethod(EventRecordWithMetadata methodEndEvent)
  {
    var topOfStack = myFramesStack.Pop();
    var (topmostFrame, shouldProcess, methodEvents, _) = topOfStack;
    if (!shouldProcess) return;
    
    if (methodEvents.Count > 0)
    {
      var existingValue = myResult.GetOrCreate(topmostFrame, static () => new List<List<EventRecordWithMetadata>>());
      var listOfListOfEvents = (List<List<EventRecordWithMetadata>>) existingValue;
      listOfListOfEvents.Add(methodEvents);
    }
    
    if (myInlineEventsFromInnerMethods)
    {
      topOfStack.Events.Add(methodEndEvent);
      AddEventToAllFrames(methodEndEvent);
      return;
    }

    if (myFramesStack.Count <= 0) return;
      
    var currentTopmost = myFramesStack.Peek();
    var contextEvent = currentTopmost.Events.Count switch
    {
      > 0 => currentTopmost.Events[^1],
      _ => currentTopmost.OriginalEvent
    };

    var startEventCtx = EventsCreationContext.CreateWithUndefinedStackTrace(contextEvent);
    currentTopmost.Events.Add(myEventsFactory.CreateMethodExecutionEvent(startEventCtx, topmostFrame));
  }

  private void AddEventToAllFrames(EventRecordWithMetadata eventRecord)
  {
    foreach (var (_, frameShouldProcess, frameEvents, _) in myFramesStack)
    {
      if (frameShouldProcess)
      {
        frameEvents.Add(eventRecord);
      }
    }
  }

  private void ProcessNormalEvent(EventRecordWithMetadata eventRecord)
  {
    if (myFramesStack.Count <= 0) return;
    
    if (myInlineEventsFromInnerMethods)
    {
      AddEventToAllFrames(eventRecord);
    }
    else
    {
      var topmostFrame = myFramesStack.Peek();
      if (topmostFrame.ShouldProcess)
      {
        topmostFrame.Events.Add(eventRecord);
      }
    }
  }
}