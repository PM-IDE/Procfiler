using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventRecord;
using Procfiler.Utils;

namespace Procfiler.Core.EventsCollection.ModificationSources;

public interface IModificationSource : IEnumerable<EventRecordWithMetadata>
{
}

public class MethodStartEndModificationSource : IModificationSource
{
  private readonly IProcfilerLogger myLogger;
  private readonly IShadowStacks myShadowStacks;
  private readonly IProcfilerEventsFactory myEventsFactory;
  private readonly SessionGlobalData myGlobalData;
  private readonly long myManagedThreadId;


  public MethodStartEndModificationSource(
    IProcfilerLogger logger, 
    IProcfilerEventsFactory eventsFactory,
    IShadowStacks shadowStacks,
    SessionGlobalData globalData,
    long managedThreadId)
  {
    myLogger = logger;
    myShadowStacks = shadowStacks;
    myManagedThreadId = managedThreadId;
    myGlobalData = globalData;
    myEventsFactory = eventsFactory;
  }

  
  public IEnumerator<EventRecordWithMetadata> GetEnumerator()
  {
    if (myShadowStacks.FindShadowStack(myManagedThreadId) is not { } shadowStack)
    {
      myLogger.LogError("Failed to retrieve shadow stack for managed thread {Id}", myManagedThreadId);
      yield break;
    }
    
    foreach (var frameInfo in shadowStack)
    {
      var creationContext = new FromFrameInfoCreationContext
      {
        FrameInfo = frameInfo,
        GlobalData = myGlobalData,
        ManagedThreadId = myManagedThreadId
      };

      if (myEventsFactory.TryCreateMethodEvent(creationContext) is not { } createMethodEvent)
      {
        myLogger.LogWarning("Failed to create method event for {FunctionId}", creationContext.FrameInfo.FunctionId);
        continue;
      }

      yield return createMethodEvent;
    }
  }

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}