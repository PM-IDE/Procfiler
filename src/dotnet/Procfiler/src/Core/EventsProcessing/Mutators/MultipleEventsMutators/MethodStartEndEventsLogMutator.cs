using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.CppProcfiler;
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
    if (context.Stacks.FindShadowStack(managedThreadId) is not { } foundShadowStack)
    {
      myLogger.LogWarning("Managed thread {Id} was not in shadow stacks", managedThreadId);
      return;
    }

    using var shadowStack = foundShadowStack;
    
    var enumerator = shadowStack.GetEnumerator();
    enumerator.MoveNext();
    
    var finished = AddMethodsEventsToCollection(events, context, managedThreadId, enumerator);
    
    if (!finished)
    {
      AddRemainingMethodsEvents(events, context, managedThreadId, enumerator);
    }
  }

  private bool AddMethodsEventsToCollection(
    IEventsCollection events, SessionGlobalData context, int managedThreadId, IEnumerator<FrameInfo> enumerator)
  {
    var finished = false;
    events.ApplyNotPureActionForAllEvents((ptr, eventRecord) =>
    {
      while (!finished && enumerator.Current.TimeStamp < eventRecord.Stamp)
      {
        if (TryCreateMethodEvent(enumerator.Current, context, managedThreadId) is { } createMethodEvent)
        {
          events.InsertBefore(ptr, createMethodEvent);
        }

        if (!enumerator.MoveNext())
        {
          finished = true;
          return false;
        }
      }

      return false;
    });

    return finished;
  }

  private void AddRemainingMethodsEvents(
    IEventsCollection events, SessionGlobalData context, int managedThreadId, IEnumerator<FrameInfo> enumerator)
  {
    do
    {
      var last = events.Last;
      Debug.Assert(last is { });

      if (TryCreateMethodEvent(enumerator.Current, context, managedThreadId) is { } createMethodEvent)
      {
        events.InsertAfter(last.Value, createMethodEvent);
      }
    } while (enumerator.MoveNext());
  }
  
  private EventRecordWithMetadata? TryCreateMethodEvent(
    FrameInfo frameInfo, SessionGlobalData context, int managedThreadId)
  {
    var methodId = frameInfo.FunctionId;
    if (!context.MethodIdToFqn.TryGetValue(methodId, out var fqn))
    {
      myLogger.LogWarning("Failed to get fqn for {FunctionId}", methodId);
      return null;
    }

    var creationContext = new EventsCreationContext(frameInfo.TimeStamp, managedThreadId);
    return frameInfo.IsStart switch
    {
      true => myEventsFactory.CreateMethodStartEvent(creationContext, fqn),
      false => myEventsFactory.CreateMethodEndEvent(creationContext, fqn)
    };
  }
}