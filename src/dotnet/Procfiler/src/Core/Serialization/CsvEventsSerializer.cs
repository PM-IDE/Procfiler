using Procfiler.Core.EventRecord;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Serialization;


public interface ICsvEventsSerializer : IEventsSerializer
{
}

[AppComponent]
public class CsvEventsSerializer : ICsvEventsSerializer
{
  private const char Delimiter = '\t'; 

  
  public async ValueTask SerializeEventsAsync(IEnumerable<EventRecordWithMetadata> events, Stream stream)
  {
    using var enumerator = events.GetEnumerator();
    if (!enumerator.MoveNext()) return;
    
    var firstEvent = enumerator.Current;
    await stream.WriteAsync(SerializeCsvHeader(firstEvent));
    await stream.WriteAsync(SerializeEventToCsvBytes(firstEvent));

    while (enumerator.MoveNext())
    {
      await stream.WriteAsync(SerializeEventToCsvBytes(enumerator.Current));
    }
  }
  
  private static ReadOnlyMemory<byte> SerializeCsvHeader(EventRecordWithMetadata firstEvent)
  {
    var sb = new StringBuilder();

    sb.Append(nameof(firstEvent.Stamp)).Append(Delimiter)
      .Append(nameof(firstEvent.EventName)).Append(Delimiter)
      .Append(nameof(firstEvent.ManagedThreadId)).Append(Delimiter)
      .Append(nameof(firstEvent.StackTraceId)).Append(Delimiter)
      .Append(nameof(firstEvent.ActivityId)).Append(Delimiter);

    foreach (var (name, _) in firstEvent.Metadata)
    {
      sb.Append(name).Append(Delimiter);
    }

    sb.AppendNewLine();
    return Encoding.UTF8.GetBytes(sb.ToString()).AsMemory();
  }
  
  private static ReadOnlyMemory<byte> SerializeEventToCsvBytes(EventRecordWithMetadata @event)
  {
    var sb = new StringBuilder();

    sb.Append(@event.Stamp).Append(Delimiter)
      .Append(@event.EventName).Append(Delimiter)
      .Append(@event.ManagedThreadId).Append(Delimiter)
      .Append(@event.StackTraceId).Append(Delimiter)
      .Append(@event.ActivityId).Append(Delimiter);
    
    foreach (var (_, value) in @event.Metadata)
    {
      sb.Append(StringBuilderExtensions.SerializeValue(value)).Append(Delimiter);
    }
    
    sb.Append("\n");
    return Encoding.UTF8.GetBytes(sb.ToString()).AsMemory();
  }
}