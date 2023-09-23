using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Utils;

namespace Procfiler.Core.SplitByMethod;

public class SplitterImplementation(
  IProcfilerEventsFactory eventsFactory,
  IEnumerable<EventRecordWithPointer> events,
  string filterPattern,
  InlineMode inlineMode
)
{
  private readonly Dictionary<string, IReadOnlyList<IReadOnlyList<EventRecordWithMetadata>>> myResult = new();


  public IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<EventRecordWithMetadata>>> Split()
  {
    new CallbackBasedSplitter<List<EventRecordWithMetadata>>(events, filterPattern, inlineMode,
      static () => new List<EventRecordWithMetadata>(),
      update =>
      {
        switch (update)
        {
          case MethodStartedUpdate<List<EventRecordWithMetadata>> methodStartedUpdate:
          {
            HandleMethodStartedUpdate(methodStartedUpdate);
            break;
          }
          case NormalEventUpdate<List<EventRecordWithMetadata>> normalEventUpdate:
          {
            HandleNormalUpdate(normalEventUpdate);
            return;
          }
          case MethodFinishedUpdate<List<EventRecordWithMetadata>> methodFinishedUpdate:
          {
            HandleMethodFinishedUpdate(methodFinishedUpdate);
            return;
          }
          case MethodExecutionUpdate<List<EventRecordWithMetadata>> methodExecutionUpdate:
          {
            HandleMethodExecutionUpdate(methodExecutionUpdate);
            return;
          }
          default:
            throw new ArgumentOutOfRangeException();
        }
      }).Split();

    return myResult;
  }

  private void HandleMethodStartedUpdate(MethodStartedUpdate<List<EventRecordWithMetadata>> methodStartedUpdate)
  {
    methodStartedUpdate.FrameInfo.State.Add(methodStartedUpdate.Event);
  }

  private void HandleNormalUpdate(NormalEventUpdate<List<EventRecordWithMetadata>> normalEventUpdate)
  {
    normalEventUpdate.FrameInfo.State.Add(normalEventUpdate.Event);
  }

  private void HandleMethodFinishedUpdate(MethodFinishedUpdate<List<EventRecordWithMetadata>> methodFinishedUpdate)
  {
    var events = methodFinishedUpdate.FrameInfo.State;

    if (events.Count <= 0) return;

    var existingValue = myResult.GetOrCreate(methodFinishedUpdate.FrameInfo.Frame, static () => new List<List<EventRecordWithMetadata>>());
    var listOfListOfEvents = (List<List<EventRecordWithMetadata>>)existingValue;
    listOfListOfEvents.Add(events);
  }

  private void HandleMethodExecutionUpdate(MethodExecutionUpdate<List<EventRecordWithMetadata>> methodExecutionUpdate)
  {
    var currentTopmost = methodExecutionUpdate.FrameInfo;
    var contextEvent = currentTopmost.State.Count switch
    {
      > 0 => currentTopmost.State[^1],
      _ => null
    };

    var startEventCtx = contextEvent switch
    {
      { } => EventsCreationContext.CreateWithUndefinedStackTrace(contextEvent),
      _ => new EventsCreationContext(currentTopmost.OriginalEventStamp, currentTopmost.OriginalEventThreadId)
    };

    currentTopmost.State.Add(eventsFactory.CreateMethodExecutionEvent(startEventCtx, methodExecutionUpdate.MethodName));
  }
}