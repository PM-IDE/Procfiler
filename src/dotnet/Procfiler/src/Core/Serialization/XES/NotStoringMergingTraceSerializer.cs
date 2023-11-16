using Procfiler.Core.Collector;
using Procfiler.Utils;

namespace Procfiler.Core.Serialization.XES;

public class PathWriteState
{
  public required XmlWriter Writer { get; init; }
  public int TracesCount { get; set; }
}

public class NotStoringMergingTraceSerializer(IXesEventsSerializer serializer, IProcfilerLogger logger, bool writeAllEventData) : IDisposable
{
  private readonly Dictionary<string, PathWriteState> myPathsToWriters = new();

  public void WriteTrace(string path, EventSessionInfo sessionInfo)
  {
    using var _ = new PerformanceCookie($"{GetType()}::{nameof(WriteTrace)}", logger, LogLevel.Trace);

    if (!myPathsToWriters.TryGetValue(path, out var existingState))
    {
      var newWriter = XmlWriter.Create(File.OpenWrite(path), new XmlWriterSettings
      {
        ConformanceLevel = ConformanceLevel.Document,
        Indent = true,
        CloseOutput = true
      });

      serializer.WriteHeader(newWriter);
      var newState = new PathWriteState { Writer = newWriter };
      myPathsToWriters[path] = newState;
      existingState = newState;
    }

    serializer.AppendTrace(sessionInfo, existingState.Writer, existingState.TracesCount, writeAllEventData);
    existingState.TracesCount += 1;
    existingState.Writer.Flush();
  }

  public void Dispose()
  {
    SerializersUtil.DisposeXesWriters(myPathsToWriters.Select(pair => (pair.Key, pair.Value.Writer)), logger);
  }
}