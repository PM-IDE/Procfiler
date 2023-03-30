using Procfiler.Core.EventRecord;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Serialization;

public interface IMethodTreeEventSerializer : IEventsSerializer
{
}

[AppComponent]
public class MethodTreeSerializer : IMethodTreeEventSerializer
{
  public async ValueTask SerializeEventsAsync(
    IEnumerable<EventRecordWithMetadata> events,
    Stream stream)
  {
    var indent = 0;
    var sb = new StringBuilder();
    
    foreach (var eventRecord in events)
    {
      sb.Clear();
      
      void AddIndent()
      {
        for (var i = 0; i < indent; i++)
        {
          sb.Append("--");
        }
      }
      
      if (eventRecord.TryGetMethodStartEndEventInfo() is var (_, isStart))
      {
        if (isStart)
        {
          AddIndent();
          ++indent;
        }
        else
        {
          --indent;
          AddIndent();
        }
      }
      else
      {
        AddIndent();
      }

      sb.Append(eventRecord.EventName);
      using (sb.AppendBraces())
      {
        foreach (var (key, value) in eventRecord.Metadata)
        {
          sb.Append(key).Append('=').Append(value).Append(',');
        }

        if (eventRecord.Metadata.Count > 0)
        {
          sb.Remove(sb.Length - 1, 1);
        }
      }

      sb.Append('\n');

      await stream.WriteAsync(Encoding.UTF8.GetBytes(sb.ToString()));
    }
  }
}