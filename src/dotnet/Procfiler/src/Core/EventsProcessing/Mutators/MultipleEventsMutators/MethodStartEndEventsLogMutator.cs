using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsCollection.ModificationSources;
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

    var modificationSource = new MethodStartEndModificationSource(myLogger, myFactory, context, foundShadowStack);
    events.InjectModificationSource(modificationSource);
  }
}