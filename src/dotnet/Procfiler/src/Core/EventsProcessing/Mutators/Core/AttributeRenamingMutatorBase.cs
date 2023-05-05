using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;
using Procfiler.Utils;

namespace Procfiler.Core.EventsProcessing.Mutators.Core;

public abstract class AttributeRenamingMutatorBase : SingleEventMutatorBase
{
  private readonly string myInitialName;
  private readonly string myFinalName;


  public override IEnumerable<EventLogMutation> Mutations =>
    new[] { new AttributeRenameMutation(EventType, myInitialName, myFinalName) };

  
  protected AttributeRenamingMutatorBase(IProcfilerLogger logger, string initialName, string finalName) : base(logger)
  {
    myInitialName = initialName;
    myFinalName = finalName;
  }

  
  protected override void ProcessInternal(EventRecordWithMetadata eventRecord, SessionGlobalData context)
  {
    eventRecord.Metadata[myFinalName] = eventRecord.Metadata[myInitialName];
    eventRecord.Metadata.Remove(myInitialName);
  }
}