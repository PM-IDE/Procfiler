using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler.ShadowStacks;
using Procfiler.Core.EventRecord;
using Procfiler.Utils;

namespace Procfiler.Core.EventsCollection.ModificationSources;

public interface IModificationSource : IEventsOwner
{
}

public abstract class ModificationSourceBase : EventsOwnerBase, IModificationSource
{
  protected ModificationSourceBase(long initialEventsCount) : base(initialEventsCount)
  {
  }
  
  
  public override bool Remove(EventPointer pointer)
  {
    AssertNotFrozen();
    return PointersManager.Remove(pointer);
  }
}

public class MethodStartEndModificationSource : ModificationSourceBase
{
  private readonly IProcfilerLogger myLogger;
  private readonly IProcfilerEventsFactory myEventsFactory;
  private readonly SessionGlobalData myGlobalData;
  private readonly ICppShadowStack myShadowStack;


  public override long Count => PointersManager.Count;


  public MethodStartEndModificationSource(
    IProcfilerLogger logger, 
    IProcfilerEventsFactory eventsFactory,
    SessionGlobalData globalData,
    ICppShadowStack shadowStack) : base(shadowStack.FramesCount)
  {
    Debug.Assert(shadowStack.FramesCount > 0);
    
    myLogger = logger;
    myGlobalData = globalData;
    myShadowStack = shadowStack;
    myEventsFactory = eventsFactory;
  }

  
  protected override IEnumerable<EventRecordWithMetadata> EnumerateInitialEvents() => 
    myShadowStack.EnumerateMethods(myEventsFactory, myGlobalData);
}