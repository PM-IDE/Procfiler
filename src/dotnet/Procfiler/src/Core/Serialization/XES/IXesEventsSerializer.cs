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
  Task SerializeEventsAsync(IEnumerable<EventSessionInfo> eventsTraces, Stream stream);
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


  public async Task SerializeEventsAsync(IEnumerable<EventSessionInfo> eventsTraces, Stream stream)
  {
    using var performanceCookie = new PerformanceCookie($"{GetType().Name}::{nameof(SerializeEventsAsync)}", myLogger);

    await using var writer = XmlWriter.Create(stream, new XmlWriterSettings
    {
      Indent = true,
      Async = true
    });

    await using var _ = await StartEndElementCookie.CreateStartElementAsync(writer, null, LogTagName, null);
    await WriteHeaderAsync(writer);

    var traceNum = 0;
    foreach (var sessionInfo in eventsTraces)
    {
      await WriteTrace(traceNum++, sessionInfo, writer);
    }
  }

  private static async Task WriteTrace(int traceNum, EventSessionInfo sessionInfo, XmlWriter writer)
  {
    await using var _ = await StartEndElementCookie.CreateStartElementAsync(writer, null, TraceTagName, null);
    await WriteStringValueTagAsync(writer, ConceptName, traceNum.ToString());

    foreach (var (_, currentEvent) in new OrderedEventsEnumerator(sessionInfo.Events))
    {
      await WriteEventNodeAsync(writer, currentEvent);
    }
  }

  private static async Task WriteEventNodeAsync(XmlWriter writer, EventRecordWithMetadata currentEvent)
  {
    await using var _ = await StartEndElementCookie.CreateStartElementAsync(writer, null, EventTag, null);
    
    await WriteDateTagAsync(writer, currentEvent.Stamp);
    await WriteStringValueTagAsync(writer, ConceptName, currentEvent.EventName);
    await WriteStringValueTagAsync(writer, EventId, (ourNextEventId++).ToString());
    await WriteStringValueTagAsync(writer, "ManagedThreadId", currentEvent.ManagedThreadId.ToString());
    
    await AddMetadataValueIfPresentAndRemoveFromMetadataAsync(
      writer, currentEvent, XesStandardLifecycleConstants.Transition, StandardLifecycleTransition);
    
    await AddMetadataValueIfPresentAndRemoveFromMetadataAsync(
      writer, currentEvent, XesStandardLifecycleConstants.ActivityId, ConceptInstanceId);
  }

  private static async Task AddMetadataValueIfPresentAndRemoveFromMetadataAsync(
    XmlWriter writer, EventRecordWithMetadata eventRecord, string metadataKey, string attributeName)
  {
    if (eventRecord.Metadata.TryGetValue(metadataKey, out var value))
    {
      await WriteStringValueTagAsync(writer, attributeName, value);
      eventRecord.Metadata.Remove(metadataKey);
    }
  }

  private static async Task WriteHeaderAsync(XmlWriter writer)
  {
    await writer.WriteAttributeStringAsync(null, XesVersion, null, "1849.2016");
    await writer.WriteAttributeStringAsync(null, XesFeatures, null, string.Empty);

    await WriteExtensionTagAsync(writer, "MetaData_Time", "meta_time", "http://www.xes-standard.org/meta_time.xesext");
    await WriteExtensionTagAsync(writer, "Lifecycle", "lifecycle", "http://www.xes-standard.org/lifecycle.xesext");
    await WriteExtensionTagAsync(writer, "MetaData_LifeCycle", "meta_life", "http://www.xes-standard.org/meta_life.xesext");
    await WriteExtensionTagAsync(writer, "Organizational", "org", "http://www.xes-standard.org/org.xesext");
    await WriteExtensionTagAsync(writer, "MetaData_Organization", "meta_org", "http://www.xes-standard.org/meta_org.xesext");
    await WriteExtensionTagAsync(writer, "Time", "time", "http://www.xes-standard.org/time.xesext");
    await WriteExtensionTagAsync(writer, "MetaData_Concept", "meta_concept", "http://www.xes-standard.org/meta_concept.xesext");
    await WriteExtensionTagAsync(writer, "MetaData_Completeness", "meta_completeness", "http://www.xes-standard.org/meta_completeness.xesext");
    await WriteExtensionTagAsync(writer, "MetaData_3TU", "meta_3TU", "http://www.xes-standard.org/meta_3TU.xesext");
    await WriteExtensionTagAsync(writer, "Concept", "concept", "http://www.xes-standard.org/concept.xesext");
    await WriteExtensionTagAsync(writer, "MetaData_General", "meta_general", "http://www.xes-standard.org/meta_general.xesext");
  }

  private static async Task WriteExtensionTagAsync(XmlWriter writer, string name, string prefix, string uri)
  {
    await using var _ = await StartEndElementCookie.CreateStartElementAsync(writer, null, Extension, null);
    await writer.WriteAttributeStringAsync(null, Name, null, name);
    await writer.WriteAttributeStringAsync(null, Prefix, null, prefix);
    await writer.WriteAttributeStringAsync(null, Uri, null, uri);
  }

  private static async Task WriteStringValueTagAsync(XmlWriter writer, string key, string value)
  {
    await using var _ = await StartEndElementCookie.CreateStartElementAsync(writer, null, StringTagName, null);
    await WriteKeyAttributeAsync(writer, key);
    await WriteAttributeAsync(writer, ValueAttr, value);
  }

  private static async Task WriteDateTagAsync(XmlWriter writer, long stamp)
  {
    await using var _ = await StartEndElementCookie.CreateStartElementAsync(writer, null, DateTag, null);
    await WriteKeyAttributeAsync(writer, DateTimeKey);
    
    var dateString = new DateTime(stamp).ToUniversalTime().ToString("O");
    await WriteAttributeAsync(writer, ValueAttr, dateString);
  }

  private static Task WriteAttributeAsync(XmlWriter writer, string name, string value)
  {
    return writer.WriteAttributeStringAsync(null, name, null, value);
  }

  private static Task WriteKeyAttributeAsync(XmlWriter writer, string value)
  {
    return writer.WriteAttributeStringAsync(null, Key, null, value);
  }
}