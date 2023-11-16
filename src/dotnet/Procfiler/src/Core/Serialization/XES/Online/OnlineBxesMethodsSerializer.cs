using Bxes.Models;
using Bxes.Writer.Stream;
using Procfiler.Core.EventRecord;
using Procfiler.Core.SplitByMethod;
using Procfiler.Utils;

namespace Procfiler.Core.Serialization.XES.Online;

public class BxesEvent : IEvent
{
  public long Timestamp { get; }
  public string Name { get; }
  public IEventLifecycle Lifecycle { get; }
  public IEventAttributes Attributes { get; }


  public BxesEvent(EventRecordWithMetadata eventRecord, bool writeAllEventMetadata)
  {
    Timestamp = eventRecord.Stamp;
    Name = eventRecord.EventName;
    Lifecycle = new BrafLifecycle(BrafLifecycleValues.Unspecified);
    
    var attributes = new EventAttributesImpl();

    if (writeAllEventMetadata)
    {
      foreach (var (key, value) in eventRecord.Metadata)
      {
        attributes[new BXesStringValue(key)] = new BXesStringValue(value);
      } 
    }

    Attributes = attributes;
  }

  public bool Equals(IEvent? other)
  {
    return other is { } &&
           Timestamp == other.Timestamp &&
           Name == other.Name &&
           Lifecycle.Equals(other.Lifecycle) &&
           Attributes.Equals(other.Attributes);
  }
}

public class BxesWriteState
{
  public EventRecordWithMetadata? LastWrittenEvent { get; set; }
  public SingleFileBxesStreamWriterImpl<BxesEvent> Writer { get; init; }
}

public class OnlineBxesMethodsSerializer : OnlineMethodsSerializerBase<BxesWriteState>
{
  private const string BxesExtesnsion = ".bxes";
  
  public OnlineBxesMethodsSerializer(
    string outputDirectory, 
    Regex? targetMethodsRegex, 
    IFullMethodNameBeautifier methodNameBeautifier, 
    IProcfilerEventsFactory factory, 
    IProcfilerLogger logger,
    bool writeAllEventMetadata) 
    : base(outputDirectory, targetMethodsRegex, methodNameBeautifier, factory, logger, writeAllEventMetadata)
  {
  }

  protected override BxesWriteState? TryCreateStateInternal(EventRecordWithMetadata contextEvent)
  {
    var methodName = contextEvent.GetMethodStartEndEventInfo().Frame;
    var name = FullMethodNameBeautifier.Beautify(methodName);
    if (!name.EndsWith(BxesExtesnsion))
    {
      name += BxesExtesnsion;
    }
    
    var filePath = Path.Join(OutputDirectory, name);

    return States.GetOrCreate(
      filePath, () => new BxesWriteState { Writer = new SingleFileBxesStreamWriterImpl<BxesEvent>(filePath, 0) });
  }

  protected override void HandleUpdate(EventUpdateBase<BxesWriteState> update)
  {
    if (update.FrameInfo.State is null) return;

    switch (update)
    {
      case MethodExecutionUpdate<BxesWriteState> methodExecutionUpdate:
        var state = update.FrameInfo.State;
        var executionEvent = CurrentFrameInfoUtil.CreateMethodExecutionEvent(
          methodExecutionUpdate.FrameInfo,
          Factory,
          methodExecutionUpdate.MethodName,
          update.FrameInfo.State!.LastWrittenEvent
        );
        
        WriteEvent(state, executionEvent);
        break;
      case MethodFinishedUpdate<BxesWriteState>:
        break;
      case MethodStartedUpdate<BxesWriteState>:
        update.FrameInfo.State.Writer.HandleEvent(new BxesTraceVariantStartEvent(1));
        break;
      case NormalEventUpdate<BxesWriteState> normalEventUpdate:
        WriteEvent(update.FrameInfo.State, normalEventUpdate.Event);
        break;
      default:
        throw new ArgumentOutOfRangeException(nameof(update));
    }
  }
  
  private void WriteEvent(BxesWriteState state, EventRecordWithMetadata eventRecord)
  {
    state.LastWrittenEvent = eventRecord;
    state.Writer.HandleEvent(new BxesEventEvent<BxesEvent>(new BxesEvent(eventRecord, WriteAllEventMetadata)));
  }

  public override void Dispose()
  {
    SerializersUtil.DisposeWriters(States.Select(pair => (pair.Key, pair.Value.Writer)), Logger, _ => {});
  }
}