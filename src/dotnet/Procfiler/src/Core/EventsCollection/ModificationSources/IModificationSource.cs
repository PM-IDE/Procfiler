using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventRecord;
using Procfiler.Utils;

namespace Procfiler.Core.EventsCollection.ModificationSources;

public interface IModificationSource : IInsertableEventsCollection, IEnumerable<EventRecordWithPointer>, IEventsOwner
{
}

public class MethodStartEndModificationSource : EventsOwnerBase, IModificationSource
{
  private readonly IProcfilerLogger myLogger;
  private readonly IProcfilerEventsFactory myEventsFactory;
  private readonly SessionGlobalData myGlobalData;
  private readonly IShadowStack myShadowStack;


  public MethodStartEndModificationSource(
    IProcfilerLogger logger, 
    IProcfilerEventsFactory eventsFactory,
    SessionGlobalData globalData,
    IShadowStack shadowStack) : base(shadowStack.FramesCount)
  {
    myLogger = logger;
    myGlobalData = globalData;
    myShadowStack = shadowStack;
    myEventsFactory = eventsFactory;
  }
  

  protected override IEnumerable<EventRecordWithMetadata> EnumerateInitialEvents()
  {
    foreach (var frameInfo in myShadowStack)
    {
      var creationContext = new FromFrameInfoCreationContext
      {
        FrameInfo = frameInfo,
        GlobalData = myGlobalData,
        ManagedThreadId = myShadowStack.ManagedThreadId
      };

      if (myEventsFactory.TryCreateMethodEvent(creationContext) is not { } createMethodEvent)
      {
        myLogger.LogWarning("Failed to create method event for {FunctionId}", creationContext.FrameInfo.FunctionId);
        continue;
      }

      yield return createMethodEvent;
    }
  }
}