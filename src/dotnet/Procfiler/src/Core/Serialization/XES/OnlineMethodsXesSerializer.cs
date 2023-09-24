using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.SplitByMethod;
using Procfiler.Utils;

namespace Procfiler.Core.Serialization.XES;

public class PathWriterStateWithLastEvent : PathWriteState
{
  public EventRecordWithMetadata? LastWrittenEvent { get; set; }
  public bool IsWritingTrace { get; set; }
}

public class OnlineMethodsXesSerializer(
  string outputDirectory,
  Regex? targetMethodsRegex,
  IXesEventsSerializer serializer, 
  IFullMethodNameBeautifier methodNameBeautifier,
  IProcfilerEventsFactory factory,
  IProcfilerLogger logger
) : IDisposable
{
  private readonly Dictionary<string, PathWriterStateWithLastEvent> myWriters = new();
  
  public void SerializeThreadEvents(
    IEnumerable<EventRecordWithPointer> events,
    string filterPattern,
    InlineMode inlineMode)
  {
    var splitter = new CallbackBasedSplitter<PathWriterStateWithLastEvent>(
      events, filterPattern, inlineMode, @event => CreateWriter(outputDirectory, @event), HandleUpdate);
    
    splitter.Split();
  }

  private PathWriterStateWithLastEvent? CreateWriter(string outputDirectory, EventRecordWithMetadata contextEvent)
  {
    var methodName = contextEvent.GetMethodStartEndEventInfo().Frame;
    if (targetMethodsRegex is { } && !targetMethodsRegex.IsMatch(methodName))
    {
      return null;
    }
    
    var filePath = Path.Join(outputDirectory, methodNameBeautifier.Beautify(methodName));

    return myWriters.GetOrCreate(filePath, () =>
    {
      var outputStream = File.OpenWrite(filePath);
      var writer = XmlWriter.Create(outputStream, new XmlWriterSettings
      {
        ConformanceLevel = ConformanceLevel.Document,
        Indent = true,
      });

      serializer.WriteHeader(writer);
      return new PathWriterStateWithLastEvent { Writer = writer };
    });
  }

  private void HandleUpdate(EventUpdateBase<PathWriterStateWithLastEvent> update)
  {
    if (update.FrameInfo.State is null) return;

    switch (update)
    {
      case MethodExecutionUpdate<PathWriterStateWithLastEvent> methodExecutionUpdate:
        HandleMethodExecutionEvent(methodExecutionUpdate);
        break;
      case MethodFinishedUpdate<PathWriterStateWithLastEvent> methodFinishedUpdate:
        HandleMethodFinishedEvent(methodFinishedUpdate);
        break;
      case MethodStartedUpdate<PathWriterStateWithLastEvent> methodStartedUpdate:
        HandleMethodStartEvent(methodStartedUpdate);
        break;
      case NormalEventUpdate<PathWriterStateWithLastEvent> normalEventUpdate:
        HandleNormalEvent(normalEventUpdate);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(update));
    }
  }

  private void HandleMethodStartEvent(MethodStartedUpdate<PathWriterStateWithLastEvent> methodStartedUpdate)
  {
    var state = methodStartedUpdate.FrameInfo.State!;

    if (!state.IsWritingTrace)
    {
      serializer.WriteTraceStart(state.Writer, state.TracesCount);
      state.TracesCount++;
      state.IsWritingTrace = true;
    }

    WriteEvent(state, methodStartedUpdate.Event);
  }

  private void WriteEvent(PathWriterStateWithLastEvent state, EventRecordWithMetadata eventRecord)
  {
    state.LastWrittenEvent = eventRecord;
    serializer.WriteEvent(eventRecord, state.Writer);
  }

  private void HandleMethodFinishedEvent(MethodFinishedUpdate<PathWriterStateWithLastEvent> methodFinishedUpdate)
  {
    var state = methodFinishedUpdate.FrameInfo.State!;

    if (state.IsWritingTrace)
    {
      state.Writer.WriteEndElement();
      state.IsWritingTrace = false;
    }
  }

  private void HandleMethodExecutionEvent(MethodExecutionUpdate<PathWriterStateWithLastEvent> methodExecutionUpdate)
  {
    var state = methodExecutionUpdate.FrameInfo.State;
    var executionEvent = CurrentFrameInfoUtil.CreateMethodExecutionEvent(
      methodExecutionUpdate.FrameInfo,
      factory,
      methodExecutionUpdate.MethodName,
      state!.LastWrittenEvent);
    
    WriteEvent(state, executionEvent);
  }

  private void HandleNormalEvent(NormalEventUpdate<PathWriterStateWithLastEvent> normalEventUpdate)
  {
    WriteEvent(normalEventUpdate.FrameInfo.State!, normalEventUpdate.Event);
  }

  public void Dispose()
  {
    XesSerializersUtil.DisposeWriters(myWriters.Select(pair => (pair.Key, pair.Value.Writer)), logger);
  }
}