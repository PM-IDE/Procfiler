using Procfiler.Core.Collector;
using Procfiler.Utils;

namespace Procfiler.Core.Serialization.XES;

public class MergingTracesXesSerializer
{
  private readonly IXesEventsSerializer mySerializer;
  private readonly IProcfilerLogger myLogger;
  private readonly Dictionary<string, List<EventSessionInfo>> myDocuments;

  
  public MergingTracesXesSerializer(IXesEventsSerializer serializer, IProcfilerLogger logger)
  {
    mySerializer = serializer;
    myLogger = logger;
    myDocuments = new Dictionary<string, List<EventSessionInfo>>();
  }


  public void AddTrace(string path, EventSessionInfo sessionInfo)
  {
    myDocuments.GetOrCreate(path, static () => new List<EventSessionInfo>()).Add(sessionInfo);
  }

  public async ValueTask SerializeAll()
  {
    using var _ = new PerformanceCookie($"{GetType()}::{nameof(SerializeAll)}", myLogger);
    foreach (var (path, sessions) in myDocuments)
    {
      await using var fs = File.OpenWrite(path);
      await mySerializer.SerializeEventsAsync(sessions, fs);
    }
  }
}