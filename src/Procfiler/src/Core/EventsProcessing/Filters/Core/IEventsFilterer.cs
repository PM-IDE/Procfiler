using Procfiler.Core.EventsCollection;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Filters.Core;

public interface IEventsFilterer
{
  void Filter(IEventsCollection eventsToFilter);
}

[AppComponent]
public class EventsFilterer : IEventsFilterer
{
  private readonly IEnumerable<IEventsFilter> myFilters;


  public EventsFilterer(IEnumerable<IEventsFilter> filters)
  {
    myFilters = filters;
  }


  public void Filter(IEventsCollection eventsToFilter)
  {
    if (eventsToFilter.Count == 0) return;
    
    foreach (var eventsFilter in myFilters)
    {
      eventsFilter.Filter(eventsToFilter);
    }
  }
}