using Procfiler.Core.EventRecord;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Serialization;

public interface IDelegatingEventsSerializer
{
  ValueTask SerializeEventsAsync(IEnumerable<EventRecordWithMetadata> events, Stream stream, FileFormat fileFormat);
}

public interface IEventsSerializer
{
  ValueTask SerializeEventsAsync(IEnumerable<EventRecordWithMetadata> events, Stream stream);
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


  public ValueTask SerializeEventsAsync(
    IEnumerable<EventRecordWithMetadata> events,
    Stream stream,
    FileFormat fileFormat) => fileFormat switch
  {
    FileFormat.Csv => myCsvEventsSerializer.SerializeEventsAsync(events, stream),
    FileFormat.MethodCallTree => myTreeEventSerializer.SerializeEventsAsync(events, stream),
    _ => throw new ArgumentOutOfRangeException(nameof(fileFormat), fileFormat, null)
  };
}