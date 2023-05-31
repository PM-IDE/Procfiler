using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsCollection.ModificationSources;
using Procfiler.Utils;

namespace Procfiler.Core.EventsProcessing.Mutators.MultipleEventsMutators;

public class CppStacksMethodsStartEndMutator : IMethodsStartEndProcessor
{
  private readonly IProcfilerEventsFactory myFactory;
  private readonly IProcfilerLogger myLogger;
  
  
  public CppStacksMethodsStartEndMutator(IProcfilerEventsFactory factory, IProcfilerLogger logger)
  {
    myFactory = factory;
    myLogger = logger;
  }
  
  
  public void Process(IEventsCollection events, SessionGlobalData context)
  {
    if (context.Stacks is not ICppShadowStacks cppShadowStacks)
    {
      var name = context.Stacks.GetType().Name;
      myLogger.LogError("Not compatible shadow stacks, got {Type}, expected {Type}", name, nameof(ICppShadowStacks));
      
      return;
    }
    
    using var collectionEnumerator = events.GetEnumerator();
    if (!collectionEnumerator.MoveNext())
    {
      return;
    }

    var managedThreadId = collectionEnumerator.Current.Event.ManagedThreadId;
    if (cppShadowStacks.FindShadowStack(managedThreadId) is not { } foundShadowStack)
    {
      myLogger.LogWarning("Managed thread {Id} was not in shadow stacks", managedThreadId);
      return;
    }

    var modificationSource = new MethodStartEndModificationSource(myLogger, myFactory, context, foundShadowStack);
    events.InjectModificationSource(modificationSource);
  }
}