using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Utils.Container;

namespace Procfiler.Core.SplitByMethod;

public interface IEventsCollectionByMethodsSplitter
{
  IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<EventRecordWithMetadata>>> Split(
    IEventsCollection events,
    string filterPattern,
    InlineMode inlineEventsFromInnerMethods);
}

[AppComponent]
public class EventsCollectionByMethodsSplitterImpl : IEventsCollectionByMethodsSplitter
{
  private readonly IProcfilerEventsFactory myEventsFactory;

  
  public EventsCollectionByMethodsSplitterImpl(IProcfilerEventsFactory eventsFactory)
  {
    myEventsFactory = eventsFactory;
  }


  public IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<EventRecordWithMetadata>>> Split(
    IEventsCollection events,
    string filterPattern,
    InlineMode inlineEventsFromInnerMethods)
  {
    return new SplitterImplementation(myEventsFactory, events, filterPattern, inlineEventsFromInnerMethods).Split();
  }
}