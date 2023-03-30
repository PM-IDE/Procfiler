using Procfiler.Core.Collector;
using Procfiler.Core.Constants.XesLifecycle;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsProcessing;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Serialization.XES;

public readonly record struct EmptyXesDocument(XmlDocument Document, XmlElement LogNode);

public interface IXesEventsSerializer
{
  void SerializeEvents(IEnumerable<EventSessionInfo> eventsTraces, Stream stream);
  EmptyXesDocument CreateEmptyDocument();
  XmlElement CreateTrace(int traceNum, EventSessionInfo sessionInfo, XmlDocument document);
}

[AppComponent]
public partial class XesEventsSerializer : IXesEventsSerializer
{
  [ThreadStatic]
  private static int ourNextEventId;

  private readonly IProcfilerLogger myLogger;


  public XesEventsSerializer(IUnitedEventsProcessor unitedEventsProcessor, IProcfilerLogger logger)
  {
    myLogger = logger;
  }


  public void SerializeEvents(IEnumerable<EventSessionInfo> eventsTraces, Stream stream)
  {
    using var performanceCookie = new PerformanceCookie($"{GetType().Name}::{nameof(SerializeEvents)}", myLogger);

    var (xmlDocument, root) = CreateEmptyDocument();
    AddLogAttributes(xmlDocument, root);

    var traceNum = 0;
    foreach (var sessionInfo in eventsTraces)
    {
      root.AppendChild(CreateTrace(traceNum++, sessionInfo, xmlDocument));
    }
    
    xmlDocument.Save(stream);
  }

  private static void AddLogAttributes(XmlDocument document, XmlElement root)
  {
    root.AppendChild(CreateStringValueTag(document, LifecycleModel, StandardLifecycleModel));
  }

  public XmlElement CreateTrace(int traceNum, EventSessionInfo sessionInfo, XmlDocument xmlDocument)
  {
    var currentTrace = xmlDocument.CreateNode(XmlNodeType.Element, TraceTagName, string.Empty);
    currentTrace.AppendChild(CreateStringValueTag(xmlDocument, ConceptName, traceNum.ToString()));

    foreach (var currentEvent in new OrderedEventsEnumerator(sessionInfo.Events))
    {
      currentTrace.AppendChild(CreateEventNode(xmlDocument, currentEvent));
    }

    return (XmlElement) currentTrace;
  }

  public EmptyXesDocument CreateEmptyDocument()
  {
    var xmlDocument = new XmlDocument();
    var root = xmlDocument.CreateNode(XmlNodeType.Element, LogTagName, string.Empty);
    WriteHeader(xmlDocument, root);
    xmlDocument.AppendChild(root);

    return new EmptyXesDocument(xmlDocument, (XmlElement)root);
  }

  private static XmlNode CreateEventNode(XmlDocument xmlDocument, EventRecordWithMetadata currentEvent)
  {
    var node = xmlDocument.CreateNode(XmlNodeType.Element, EventTag, string.Empty);

    node.AppendChild(CreateDateTag(xmlDocument, currentEvent.Stamp));
    node.AppendChild(CreateStringValueTag(xmlDocument, ConceptName, currentEvent.EventName));
    node.AppendChild(CreateStringValueTag(xmlDocument, EventId, (ourNextEventId++).ToString()));
    node.AppendChild(CreateStringValueTag(xmlDocument, "ManagedThreadId", currentEvent.ManagedThreadId.ToString()));
    node.AppendChild(CreateStringValueTag(xmlDocument, "StackTraceId", currentEvent.StackTraceId.ToString()));
    
    AddMetadataValueIfPresentAndRemoveFromMetadata(xmlDocument, node, currentEvent, XesStandardLifecycleConstants.Transition, StandardLifecycleTransition);
    AddMetadataValueIfPresentAndRemoveFromMetadata(xmlDocument, node, currentEvent, XesStandardLifecycleConstants.ActivityId, ConceptInstanceId);

    return node;
  }

  private static void AddMetadataValueIfPresentAndRemoveFromMetadata(
    XmlDocument document, 
    XmlNode node,
    EventRecordWithMetadata eventRecord,
    string metadataKey,
    string attributeName)
  {
    if (eventRecord.Metadata.TryGetValue(metadataKey, out var value))
    {
      node.AppendChild(CreateStringValueTag(document, attributeName, value));
      eventRecord.Metadata.Remove(metadataKey);
    }
  }

  private static void WriteHeader(XmlDocument document, XmlNode logRoot)
  {
    logRoot.Attributes!.Append(CreateAttribute(document, XesVersion, "1849.2016"));
    logRoot.Attributes!.Append(CreateAttribute(document, XesFeatures, ""));

    logRoot.AppendChild(CreateExtensionTag(document, "MetaData_Time", "meta_time", "http://www.xes-standard.org/meta_time.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "Lifecycle", "lifecycle", "http://www.xes-standard.org/lifecycle.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "MetaData_LifeCycle", "meta_life", "http://www.xes-standard.org/meta_life.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "Organizational", "org", "http://www.xes-standard.org/org.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "MetaData_Organization", "meta_org", "http://www.xes-standard.org/meta_org.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "Time", "time", "http://www.xes-standard.org/time.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "MetaData_Concept", "meta_concept", "http://www.xes-standard.org/meta_concept.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "MetaData_Completeness", "meta_completeness", "http://www.xes-standard.org/meta_completeness.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "MetaData_3TU", "meta_3TU", "http://www.xes-standard.org/meta_3TU.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "Concept", "concept", "http://www.xes-standard.org/concept.xesext"));
    logRoot.AppendChild(CreateExtensionTag(document, "MetaData_General", "meta_general", "http://www.xes-standard.org/meta_general.xesext"));
  }

  private static XmlNode CreateExtensionTag(XmlDocument document, string name, string prefix, string uri)
  {
    var extensionNode = document.CreateNode(XmlNodeType.Element, Extension, string.Empty);

    extensionNode.Attributes!.Append(CreateAttribute(document, Name, name));
    extensionNode.Attributes!.Append(CreateAttribute(document, Prefix, prefix));
    extensionNode.Attributes!.Append(CreateAttribute(document, Uri, uri));

    return extensionNode;
  }

  private static XmlNode CreateStringValueTag(XmlDocument document, string keyValue, string value)
  {
    var nameNode = document.CreateNode(XmlNodeType.Element, StringTagName, string.Empty);
    
    nameNode.Attributes!.Append(CreateKeyAttribute(document, keyValue));
    nameNode.Attributes!.Append(CreateAttribute(document, ValueAttr, value));

    return nameNode;
  }

  private static XmlNode CreateDateTag(XmlDocument document, long stamp)
  {
    var dateNode = document.CreateNode(XmlNodeType.Element, DateTag, string.Empty);
    
    dateNode.Attributes!.Append(CreateKeyAttribute(document, DateTimeKey));

    var dateString = new DateTime(stamp).ToUniversalTime().ToString("O");
    dateNode.Attributes!.Append(CreateAttribute(document, ValueAttr, dateString));
    
    return dateNode;
  }

  private static XmlAttribute CreateAttribute(XmlDocument document, string name, string value)
  {
    var attr = document.CreateAttribute(name);
    attr.Value = value;
    return attr;
  }

  private static XmlAttribute CreateKeyAttribute(XmlDocument document, string value) => 
    CreateAttribute(document, Key, value);
}