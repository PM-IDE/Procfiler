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
  private readonly IProcfilerEventsFactory myFactory;
  private readonly IProcfilerLogger myLogger;


  public IEnumerable<EventLogMutation> Mutations { get; }

  
  public MethodStartEndEventsLogMutator(IProcfilerEventsFactory factory, IProcfilerLogger logger)
  {
    myFactory = factory;
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

    using var collectionEnumerator = events.GetEnumerator();
    if (!collectionEnumerator.MoveNext())
    {
      return;
    }

    var managedThreadId = collectionEnumerator.Current.Event.ManagedThreadId;
    if (context.Stacks.FindShadowStack(managedThreadId) is not { } foundShadowStack)
    {
      myLogger.LogWarning("Managed thread {Id} was not in shadow stacks", managedThreadId);
      return;
    }

    using var shadowStack = foundShadowStack;
    
    var enumerator = shadowStack.GetEnumerator();
    enumerator.MoveNext();
    
    AddMethodsEventsToCollection(events, context, managedThreadId, enumerator);
  }

  private void AddMethodsEventsToCollection(
    IEventsCollection events, SessionGlobalData context, long managedThreadId,
    IEnumerator<FrameInfo> enumerator)
  {
    var finished = false;
    EventPointer? lastPtr = null;
    events.ApplyNotPureActionForAllEvents(eventWithPtr =>
    {
      lastPtr = eventWithPtr.EventPointer;
      while (!finished && enumerator.Current.TimeStamp < eventWithPtr.Event.Stamp)
      {
        var ctx = new FromFrameInfoCreationContext
        {
          FrameInfo = enumerator.Current,
          GlobalData = context,
          ManagedThreadId = managedThreadId
        };
        
        if (myFactory.TryCreateMethodEvent(ctx) is { } createMethodEvent)
        {
          events.InsertBefore(eventWithPtr.EventPointer, createMethodEvent);
        }

        if (!enumerator.MoveNext())
        {
          finished = true;
          return false;
        }
      }

      return false;
    });
    
    do
    {
      if (finished) return;
      
      Debug.Assert(lastPtr is { });
      var ctx = new FromFrameInfoCreationContext
      {
        FrameInfo = enumerator.Current,
        GlobalData = context,
        ManagedThreadId = managedThreadId
      };
      
      if (myFactory.TryCreateMethodEvent(ctx) is { } createMethodEvent)
      {
        lastPtr = events.InsertAfter(lastPtr.Value, createMethodEvent);
      }
    }
    while (enumerator.MoveNext());
  }
}