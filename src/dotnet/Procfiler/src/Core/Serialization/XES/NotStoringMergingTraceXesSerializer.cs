using Bxes.Writer.Stream;
using Procfiler.Core.Collector;
using Procfiler.Core.Serialization.XES.Online;
using Procfiler.Utils;

namespace Procfiler.Core.Serialization.XES;

public class PathWriteState
{
  public required XmlWriter Writer { get; init; }
  public int TracesCount { get; set; }
}

public interface INotStoringMergingTraceSerializer : IDisposable
{
  void WriteTrace(string path, EventSessionInfo sessionInfo);
}

public abstract class NotStoringMergingTraceSerializerBase<TState>(IProcfilerLogger logger, bool writeAllEventData) : INotStoringMergingTraceSerializer
{
  protected readonly IProcfilerLogger Logger = logger;
  protected readonly bool WriteAllEventData = writeAllEventData;
  protected readonly Dictionary<string, TState> States = new();

  public abstract void WriteTrace(string path, EventSessionInfo sessionInfo);
  public abstract void Dispose();
}

public class NotStoringMergingTraceXesSerializer(
  IXesEventsSerializer serializer, 
  IProcfilerLogger logger, 
  bool writeAllEventData
) : NotStoringMergingTraceSerializerBase<PathWriteState>(logger, writeAllEventData)
{
  public override void WriteTrace(string path, EventSessionInfo sessionInfo)
  {
    using var _ = new PerformanceCookie($"{GetType()}::{nameof(WriteTrace)}", Logger, LogLevel.Trace);

    if (!States.TryGetValue(path, out var existingState))
    {
      var newWriter = XmlWriter.Create(File.OpenWrite(path), new XmlWriterSettings
      {
        ConformanceLevel = ConformanceLevel.Document,
        Indent = true,
        CloseOutput = true
      });

      serializer.WriteHeader(newWriter);
      var newState = new PathWriteState { Writer = newWriter };
      States[path] = newState;
      existingState = newState;
    }

    serializer.AppendTrace(sessionInfo, existingState.Writer, existingState.TracesCount, WriteAllEventData);
    existingState.TracesCount += 1;
    existingState.Writer.Flush();
  }

  public override void Dispose()
  {
    SerializersUtil.DisposeXesWriters(States.Select(pair => (pair.Key, pair.Value.Writer)), Logger);
  }
}

public class BxesWriteState
{
  public SingleFileBxesStreamWriterImpl<BxesEvent> Writer { get; init; }
}

public class NotStoringMergingTraceBxesSerializer(
  IProcfilerLogger logger, 
  bool writeAllEventData
) : NotStoringMergingTraceSerializerBase<BxesWriteState>(logger, writeAllEventData)
{
  public override void WriteTrace(string path, EventSessionInfo sessionInfo)
  {
    var writer = States.GetOrCreate(path, () => new BxesWriteState
    {
      Writer = new SingleFileBxesStreamWriterImpl<BxesEvent>(path, 0)
    });

    writer.Writer.HandleEvent(new BxesTraceVariantStartEvent(1));
    foreach (var (_, @event) in new OrderedEventsEnumerator(sessionInfo.Events))
    {
      writer.Writer.HandleEvent(new BxesEventEvent<BxesEvent>(new BxesEvent(@event, WriteAllEventData)));
    }
  }

  public override void Dispose()
  {
    SerializersUtil.DisposeWriters(States.Select(pair => (pair.Key, pair.Value.Writer)), Logger, _ => { });
  }
}