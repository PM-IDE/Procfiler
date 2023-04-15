using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;
using Procfiler.Utils;

namespace Procfiler.Core.EventsProcessing.Mutators.Core;

public abstract class MutatorBase
{
  protected readonly IProcfilerLogger Logger;
  
  
  protected MutatorBase(IProcfilerLogger logger)
  {
    Logger = logger;
  }
}

public abstract class SingleEventMutatorBase : MutatorBase, ISingleEventMutator
{
  public abstract string EventClass { get; }
  public abstract IEnumerable<EventLogMutation> Mutations { get; }


  protected SingleEventMutatorBase(IProcfilerLogger logger) : base(logger)
  {
  }


  public void Process(EventRecordWithMetadata eventRecord, SessionGlobalData context)
  {
    if (eventRecord.EventClass == EventClass)
    {
      ProcessInternal(eventRecord, context);
    }
  }

  protected abstract void ProcessInternal(EventRecordWithMetadata eventRecord, SessionGlobalData context);
}