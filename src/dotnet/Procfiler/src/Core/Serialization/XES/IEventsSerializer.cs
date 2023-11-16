using Procfiler.Core.Collector;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Serialization.XES;

public interface IEventsSerializer
{
  void SerializeEvents(IEnumerable<EventSessionInfo> eventsTraces, string path, bool writeAllMetadata);
}

[AppComponent]
public class BxesEventsSerializer(IProcfilerLogger logger) : IBxesEventsSerializer
{
  public void SerializeEvents(IEnumerable<EventSessionInfo> eventsTraces, string path, bool writeAllMetadata)
  {
    var serializer = new NotStoringMergingTraceBxesSerializer(logger, writeAllMetadata);

    foreach (var sessionInfo in eventsTraces)
    {
      serializer.WriteTrace(path, sessionInfo);
    }
  }
}