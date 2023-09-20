using Procfiler.Core.Collector;
using Procfiler.Utils;

namespace Procfiler.Core.Serialization.XES;

public class MergingTracesXesSerializer(IXesEventsSerializer serializer, IProcfilerLogger logger)
{
  private readonly Dictionary<string, List<EventSessionInfo>> myDocuments = new();


  public void AddTrace(string path, EventSessionInfo sessionInfo)
  {
    myDocuments.GetOrCreate(path, static () => new List<EventSessionInfo>()).Add(sessionInfo);
  }

  public void SerializeAll()
  {
    using var _ = new PerformanceCookie($"{GetType()}::{nameof(SerializeAll)}", logger);
    foreach (var (path, sessions) in myDocuments)
    {
      using var fs = File.OpenWrite(path);
      serializer.SerializeEvents(sessions, fs);
    }
  }
}

public class NotStoringMergingTraceSerializer(IXesEventsSerializer serializer, IProcfilerLogger logger) : IDisposable
{
  private class PathWriteState
  {
    public required XmlWriter Writer { get; init; }
    public int TracesCount { get; set; }
  }
  
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
      });

      serializer.WriteHeader(newWriter);
      var newState = new PathWriteState { Writer = newWriter };
      myPathsToWriters[path] = newState;
      existingState = newState;
    }

    serializer.AppendTrace(sessionInfo, existingState.Writer, existingState.TracesCount);
    existingState.TracesCount += 1;
    existingState.Writer.Flush();
  }

  public void Dispose()
  {
    foreach (var (path, writer) in myPathsToWriters)
    {
      try
      {
        writer.Writer.WriteEndElement();
        writer.Writer.Dispose();
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Failed to dispose writer for path {Path}", path);
      }
    }
  }
}