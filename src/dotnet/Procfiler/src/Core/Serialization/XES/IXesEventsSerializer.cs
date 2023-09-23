using Procfiler.Core.Collector;
using Procfiler.Core.Constants.XesLifecycle;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsProcessing;
using Procfiler.Utils;
using Procfiler.Utils.Container;
using Procfiler.Utils.Xml;

namespace Procfiler.Core.Serialization.XES;

public interface IXesEventsSerializer
{
  void SerializeEvents(IEnumerable<EventSessionInfo> eventsTraces, Stream stream);
  void AppendTrace(EventSessionInfo session, XmlWriter writer, int traceNum);
  void WriteHeader(XmlWriter writer);
  void WriteEvent(EventRecordWithMetadata eventRecord, XmlWriter writer);
  void WriteTraceStart(XmlWriter writer, int traceNum);
}

[AppComponent]
public partial class XesEventsSerializer(
  IUnitedEventsProcessor unitedEventsProcessor,
  IProcfilerLogger logger
) : IXesEventsSerializer
{
  [ThreadStatic] private static int ourNextEventId;


  public void SerializeEvents(IEnumerable<EventSessionInfo> eventsTraces, Stream stream)
  {
    using var performanceCookie = new PerformanceCookie($"{GetType().Name}::{nameof(SerializeEvents)}", logger);

    using var writer = XmlWriter.Create(stream, new XmlWriterSettings
    {
      Indent = true,
    });

    WriteHeader(writer);

    var traceNum = 0;
    foreach (var sessionInfo in eventsTraces)
    {
      WriteTrace(traceNum++, sessionInfo, writer);
    }

    writer.WriteEndElement();
  }

  public void AppendTrace(EventSessionInfo session, XmlWriter writer, int traceNum) => WriteTrace(traceNum, session, writer);

  public void WriteHeader(XmlWriter writer)
  {
    writer.WriteStartElement(null, LogTagName, null);
    DoWriteHeader(writer);
  }

  public void WriteEvent(EventRecordWithMetadata eventRecord, XmlWriter writer) => WriteEventNode(writer, eventRecord);
  
  public void WriteTraceStart(XmlWriter writer, int traceNum)
  {
    writer.WriteStartElement(null, TraceTagName, null);
    WriteStringValueTag(writer, ConceptName, traceNum.ToString());
  }

  private void WriteTrace(int traceNum, EventSessionInfo sessionInfo, XmlWriter writer)
  {
    WriteTraceStart(writer, traceNum);
    
    foreach (var (_, currentEvent) in new OrderedEventsEnumerator(sessionInfo.Events))
    {
      WriteEventNode(writer, currentEvent);
    }
    
    writer.WriteEndElement();
  }
  
  private static void WriteEventNode(XmlWriter writer, EventRecordWithMetadata currentEvent)
  {
    using var _ = StartEndElementCookie.CreateStartEndElement(writer, null, EventTag, null);

    WriteDateTag(writer, currentEvent.Stamp);
    WriteStringValueTag(writer, ConceptName, currentEvent.EventName);
    WriteStringValueTag(writer, EventId, (ourNextEventId++).ToString());
    WriteStringValueTag(writer, "ManagedThreadId", currentEvent.ManagedThreadId.ToString());

    AddMetadataValueIfPresentAndRemoveFromMetadata(
      writer, currentEvent, XesStandardLifecycleConstants.Transition, StandardLifecycleTransition);

    AddMetadataValueIfPresentAndRemoveFromMetadata(
      writer, currentEvent, XesStandardLifecycleConstants.ActivityId, ConceptInstanceId);
  }

  private static void AddMetadataValueIfPresentAndRemoveFromMetadata(
    XmlWriter writer, EventRecordWithMetadata eventRecord, string metadataKey, string attributeName)
  {
    if (eventRecord.Metadata.TryGetValue(metadataKey, out var value))
    {
      WriteStringValueTag(writer, attributeName, value);
      eventRecord.Metadata.Remove(metadataKey);
    }
  }

  private static void DoWriteHeader(XmlWriter writer)
  {
    writer.WriteAttributeString(null, XesVersion, null, "1849.2016");
    writer.WriteAttributeString(null, XesFeatures, null, string.Empty);

    WriteExtensionTag(writer, "MetaData_Time", "meta_time", "http://www.xes-standard.org/meta_time.xesext");
    WriteExtensionTag(writer, "Lifecycle", "lifecycle", "http://www.xes-standard.org/lifecycle.xesext");
    WriteExtensionTag(writer, "MetaData_LifeCycle", "meta_life", "http://www.xes-standard.org/meta_life.xesext");
    WriteExtensionTag(writer, "Organizational", "org", "http://www.xes-standard.org/org.xesext");
    WriteExtensionTag(writer, "MetaData_Organization", "meta_org", "http://www.xes-standard.org/meta_org.xesext");
    WriteExtensionTag(writer, "Time", "time", "http://www.xes-standard.org/time.xesext");
    WriteExtensionTag(writer, "MetaData_Concept", "meta_concept", "http://www.xes-standard.org/meta_concept.xesext");
    WriteExtensionTag(writer, "MetaData_Completeness", "meta_completeness", "http://www.xes-standard.org/meta_completeness.xesext");
    WriteExtensionTag(writer, "MetaData_3TU", "meta_3TU", "http://www.xes-standard.org/meta_3TU.xesext");
    WriteExtensionTag(writer, "Concept", "concept", "http://www.xes-standard.org/concept.xesext");
    WriteExtensionTag(writer, "MetaData_General", "meta_general", "http://www.xes-standard.org/meta_general.xesext");
  }

  private static void WriteExtensionTag(XmlWriter writer, string name, string prefix, string uri)
  {
    using var _ = StartEndElementCookie.CreateStartEndElement(writer, null, Extension, null);
    writer.WriteAttributeString(null, Name, null, name);
    writer.WriteAttributeString(null, Prefix, null, prefix);
    writer.WriteAttributeString(null, Uri, null, uri);
  }

  private static void WriteStringValueTag(XmlWriter writer, string key, string value)
  {
    using var _ = StartEndElementCookie.CreateStartEndElement(writer, null, StringTagName, null);
    WriteKeyAttribute(writer, key);
    WriteAttribute(writer, ValueAttr, value);
  }

  private static void WriteDateTag(XmlWriter writer, long stamp)
  {
    using var _ = StartEndElementCookie.CreateStartEndElement(writer, null, DateTag, null);
    WriteKeyAttribute(writer, DateTimeKey);

    var dateString = new DateTime(stamp).ToUniversalTime().ToString("O");
    WriteAttribute(writer, ValueAttr, dateString);
  }

  private static void WriteAttribute(XmlWriter writer, string name, string value)
  {
    writer.WriteAttributeString(null, name, null, value);
  }

  private static void WriteKeyAttribute(XmlWriter writer, string value)
  {
    writer.WriteAttributeString(null, Key, null, value);
  }
}