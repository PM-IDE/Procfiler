using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.CppProcfiler.ShadowStacks;
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

public interface IMethodsStartEndProcessor
{
  void Process(IEventsCollection events, SessionGlobalData context);
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

    IMethodsStartEndProcessor processor = context.Stacks switch
    {
      IFromEventsShadowStacks => new FromEventsMethodsStartEndMutator(myFactory, myLogger),
      ICppShadowStacks => new CppStacksMethodsStartEndMutator(myFactory, myLogger),
      _ => throw new ArgumentOutOfRangeException(context.Stacks.GetType().Name)
    };
    
    processor.Process(events, context);
  }
}