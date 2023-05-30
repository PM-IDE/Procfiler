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
public class DelegatingEventsSerializer : IDelegatingEventsSerializer
{
  private readonly ICsvEventsSerializer myCsvEventsSerializer;
  private readonly IMethodTreeEventSerializer myTreeEventSerializer;


  public DelegatingEventsSerializer(
    ICsvEventsSerializer csvEventsSerializer, IMethodTreeEventSerializer treeEventSerializer)
  {
    myCsvEventsSerializer = csvEventsSerializer;
    myTreeEventSerializer = treeEventSerializer;
  }


  public void SerializeEvents(
    IEnumerable<EventRecordWithMetadata> events, string path, FileFormat fileFormat)
  {
    switch (fileFormat)
    {
      case FileFormat.Csv:
        myCsvEventsSerializer.SerializeEvents(events, path);
        return;
      case FileFormat.MethodCallTree:
        myTreeEventSerializer.SerializeEvents(events, path);
        return;
      default:
        throw new ArgumentOutOfRangeException(nameof(fileFormat), fileFormat, null);
    }
  }
}