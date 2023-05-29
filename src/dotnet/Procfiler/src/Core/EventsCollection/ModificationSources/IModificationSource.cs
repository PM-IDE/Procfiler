using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventRecord;
using Procfiler.Utils;

namespace Procfiler.Core.EventsCollection.ModificationSources;

public interface IModificationSource : IEventsOwner, IDisposable
{
}

public abstract class ModificationSourceBase : EventsOwnerBase, IModificationSource
{
  protected readonly HashSet<int> RemovedIndexes;

  
  protected ModificationSourceBase(long initialEventsCount) : base(initialEventsCount)
  {
    RemovedIndexes = new HashSet<int>();
  }
  
  
  public override bool Remove(EventPointer pointer)
  {
    AssertNotFrozen();
    if (pointer.IsInInitialArray)
    {
      if (RemovedIndexes.Contains(pointer.IndexInArray))
      {
        return false;
      }

      RemovedIndexes.Add(pointer.IndexInArray);
      DecreaseCount();
      return true;
    }

    if (PointersManager.Remove(pointer))
    {
      DecreaseCount();
      return true;
    }

    return false;
  }

  public abstract void Dispose();
}

public class MethodStartEndModificationSource : ModificationSourceBase, IModificationSource
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
    var index = -1;
    foreach (var frameInfo in myShadowStack)
    {
      ++index;
      
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

      if (RemovedIndexes.Contains(index))
      {
        createMethodEvent.IsRemoved = true;
      }
      
      yield return createMethodEvent;
    }
  }

  public override void Dispose() => myShadowStack.Dispose();
}