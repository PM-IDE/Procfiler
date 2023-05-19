using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core.Passes;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.EventsProcessing.Mutators.MultipleEventsMutators;

public interface IMethodStartEndEventsLogMutator : IMultipleEventsMutator
{
}

[EventMutator(MultipleEventMutatorsPasses.MethodStartEndInserter)]
public class MethodStartEndEventsLogMutator : IMethodStartEndEventsLogMutator
{
  private readonly IProcfilerEventsFactory myEventsFactory;
  private readonly IProcfilerLogger myLogger;


  public IEnumerable<EventLogMutation> Mutations { get; }

  
  public MethodStartEndEventsLogMutator(IProcfilerEventsFactory eventsFactory, IProcfilerLogger logger)
  {
    myEventsFactory = eventsFactory;
    myLogger = logger;
    Mutations = new[]
    {
      new AddEventMutation(TraceEventsConstants.ProcfilerMethodStart),
      new AddEventMutation(TraceEventsConstants.ProcfilerMethodEnd),
    };
  }


  public void Process(IEventsCollection events, SessionGlobalData context)
  {
    if (events.Count == 0) return;

    var managedThreadId = events.GetFor(events.First!.Value).ManagedThreadId;
    if (!context.Stacks.TryGetValue(managedThreadId, out var shadowStack))
    {
      myLogger.LogError("Managed thread {Id} was not in shadow stacks", managedThreadId);
      return;
    }

    EventRecordWithMetadata? TryCreateMethodEvent(int index)
    {
      var methodId = shadowStack[index].FunctionId;
      if (!context.MethodIdToFqn.TryGetValue(methodId, out var fqn))
      {
        myLogger.LogError("Failed to get fqn for {FunctionId}", methodId);
        return null;
      }
      
      var creationContext = new EventsCreationContext(shadowStack[index].TimeStamp, managedThreadId);
      return shadowStack[index].IsStart switch
      {
        true => myEventsFactory.CreateMethodStartEvent(creationContext, fqn),
        false => myEventsFactory.CreateMethodEndEvent(creationContext, fqn)
      };
    }

    var index = 0;
    events.ApplyNotPureActionForAllEvents((ptr, eventRecord) =>
    {
      while (index < shadowStack.Count && shadowStack[index].TimeStamp < eventRecord.Stamp)
      {
        if (TryCreateMethodEvent(index) is { } createMethodEvent)
        {
          events.InsertBefore(ptr, createMethodEvent);
        }

        ++index;
      }

      return false;
    });

    while (index < shadowStack.Count)
    {
      var last = events.Last;
      Debug.Assert(last is { });
      
      if (TryCreateMethodEvent(index) is { } createMethodEvent)
      {
        events.InsertAfter(last.Value, createMethodEvent);
      }

      ++index;
    }
  }
}