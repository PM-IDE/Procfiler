using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.EventsCollection;

namespace Procfiler.Core.Serialization.XES.Online;

public interface IOnlineMethodsSerializer
{
  IReadOnlyList<string> AllMethodNames { get; }

  void SerializeThreadEvents(
    IEnumerable<EventRecordWithPointer> events,
    string filterPattern,
    InlineMode inlineMode);
}