using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler.ShadowStacks;
using Procfiler.Core.EventRecord;

namespace Procfiler.Core.EventsCollection.ModificationSources;

public interface IModificationSource : IEventsOwner;

public abstract class ModificationSourceBase(long initialEventsCount) 
  : EventsOwnerBase(initialEventsCount), IModificationSource
{
  public override bool Remove(EventPointer pointer)
  {
    AssertNotFrozen();
    return PointersManager.Remove(pointer);
  }
}

public class MethodStartEndModificationSource : ModificationSourceBase
{
  private readonly IProcfilerEventsFactory myEventsFactory;
  private readonly SessionGlobalData myGlobalData;
  private readonly ICppShadowStack myShadowStack;


  public override long Count => PointersManager.Count;


  public MethodStartEndModificationSource(
    IProcfilerEventsFactory eventsFactory,
    SessionGlobalData globalData,
    ICppShadowStack shadowStack) : base(shadowStack.FramesCount)
  {
    Debug.Assert(shadowStack.FramesCount > 0);

    myGlobalData = globalData;
    myShadowStack = shadowStack;
    myEventsFactory = eventsFactory;
  }

  
  protected override IEnumerable<EventRecordWithMetadata> EnumerateInitialEvents() => 
    myShadowStack.EnumerateMethods(myEventsFactory, myGlobalData);
}