using Procfiler.Core.EventsCollection;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Filters.Core;

public interface IEventsFilterer
{
  void Filter(IEventsCollection eventsToFilter);
}

[AppComponent]
public class EventsFilterer(IEnumerable<IEventsFilter> filters) : IEventsFilterer
{
  public void Filter(IEventsCollection eventsToFilter)
  {
    if (eventsToFilter.Count == 0) return;
    
    foreach (var eventsFilter in filters)
    {
      eventsFilter.Filter(eventsToFilter);
    }
  }
}