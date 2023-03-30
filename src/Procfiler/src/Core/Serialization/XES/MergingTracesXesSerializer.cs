using Procfiler.Core.Collector;
using Procfiler.Utils;

namespace Procfiler.Core.Serialization.XES;

public class MergingTracesXesSerializer
{
  private record XmlDocumentState(XmlDocument Document, XmlElement LogElement)
  {
    public int TraceNum { get; set; }
  }
  
  
  private readonly IXesEventsSerializer mySerializer;
  private readonly IProcfilerLogger myLogger;
  private readonly Dictionary<string, XmlDocumentState> myDocuments;

  
  public MergingTracesXesSerializer(IXesEventsSerializer serializer, IProcfilerLogger logger)
  {
    mySerializer = serializer;
    myLogger = logger;
    myDocuments = new Dictionary<string, XmlDocumentState>();
  }


  public void AddTrace(string path, EventSessionInfo sessionInfo)
  {
    var state = GetOrCreateXmlDocumentState(path);
    var (xmlDocument, logNode) = state;
    logNode.AppendChild(mySerializer.CreateTrace(state.TraceNum++, sessionInfo, xmlDocument));
  }
  
  private XmlDocumentState GetOrCreateXmlDocumentState(string path)
  {
    if (myDocuments.TryGetValue(path, out var existingState))
    {
      return existingState;
    }

    var (document, logNode) = mySerializer.CreateEmptyDocument();
    XmlDocumentState newState = new(document, logNode);
    myDocuments[path] = newState;
    return newState;
  }
  
  public async ValueTask SerializeAll()
  {
    using var _ = new PerformanceCookie($"{GetType()}::{nameof(SerializeAll)}", myLogger);
    foreach (var (path, (document, _)) in myDocuments)
    {
      await using var fs = File.OpenWrite(path);
      document.Save(fs);
    }
  }
}