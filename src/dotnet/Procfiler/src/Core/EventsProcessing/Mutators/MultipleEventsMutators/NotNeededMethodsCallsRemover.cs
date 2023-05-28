using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.MultipleEventsMutators;

[EventMutator(MultipleEventMutatorsPasses.NotNeededMethodsRemove)]
public class NotNeededMethodsCallsRemover : IMultipleEventsMutator
{
  private static Regex[] PatternsToRemove { get; } = 
  {
    new(@"System\.Diagnostics\.Tracing\..*")
  };


  public IEnumerable<EventLogMutation> Mutations => EmptyCollections<EventLogMutation>.EmptyList;


  public void Process(IEventsCollection events, SessionGlobalData context)
  {
    foreach (var (ptr, eventRecord) in events)
    {
      if (eventRecord.TryGetMethodStartEndEventInfo() is var (frameName, _) && 
          ShouldSkipFrame(frameName))
      {
        events.Remove(ptr);
      }
    }
  }

  private static bool ShouldSkipFrame(string frameName)
  {
    foreach (var regex in PatternsToRemove)
    {
      if (regex.IsMatch(frameName)) return true;
    }

    return false;
  }
}