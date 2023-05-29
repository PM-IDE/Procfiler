using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection.ModificationSources;

namespace ProcfilerTests.Tests.EventsCollection;

public class TestModificationSource : ModificationSourceBase, IModificationSource
{
  private readonly EventRecordWithMetadata[] myInitialEvents;
  
  
  public TestModificationSource(EventRecordWithMetadata[] initialEvents) : base(initialEvents.Length)
  {
    myInitialEvents = initialEvents;
  }
  
  
  protected override IEnumerable<EventRecordWithMetadata> EnumerateInitialEvents()
  {
    for (var i = 0; i < myInitialEvents.Length; i++)
    {
      if (RemovedIndexes.Contains(i)) continue;
      yield return myInitialEvents[i];
    }
  }
  
  public override void Dispose()
  {
  }
}