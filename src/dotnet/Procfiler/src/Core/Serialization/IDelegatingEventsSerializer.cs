using Procfiler.Core.EventRecord;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Serialization;

public interface IDelegatingEventsSerializer
{
  void SerializeEvents(IEnumerable<EventRecordWithMetadata> events, string path, FileFormat fileFormat);
}

public interface IEventsSerializer
{
  void SerializeEvents(IEnumerable<EventRecordWithMetadata> events, string path);
}

[AppComponent]
public class DelegatingEventsSerializer(
  ICsvEventsSerializer csvEventsSerializer,
  IMethodTreeEventSerializer treeEventSerializer
) : IDelegatingEventsSerializer
{
  public void SerializeEvents(
    IEnumerable<EventRecordWithMetadata> events, string path, FileFormat fileFormat)
  {
    switch (fileFormat)
    {
      case FileFormat.Csv:
        csvEventsSerializer.SerializeEvents(events, path);
        return;
      case FileFormat.MethodCallTree:
        treeEventSerializer.SerializeEvents(events, path);
        return;
      default:
        throw new ArgumentOutOfRangeException(nameof(fileFormat), fileFormat, null);
    }
  }
}