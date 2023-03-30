using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Utils;

namespace Procfiler.Core.Collector;

public record EventSessionInfo(IEnumerable<IEventsCollection> Events, SessionGlobalData GlobalData);

public class SessionGlobalData
{
  private readonly Dictionary<string, string> myMethodIdToFqn;
  private readonly Dictionary<string, string> myTypeIdsToNames;
  private readonly Dictionary<int, StackTraceInfo> myStacks;

  
  public IReadOnlyDictionary<int, StackTraceInfo> Stacks => myStacks;
  public IReadOnlyDictionary<string, string> TypeIdToNames => myTypeIdsToNames;
  public IReadOnlyDictionary<string, string> MethodIdToFqn => myMethodIdToFqn;


  public SessionGlobalData()
  {
    myMethodIdToFqn = new Dictionary<string, string>();
    myTypeIdsToNames = new Dictionary<string, string>();
    myStacks = new Dictionary<int, StackTraceInfo>();
  }


  public void AddInfoFrom(EventWithGlobalDataUpdate update)
  {
    AddStack(update.StackTrace);
    AddTypeIdWithName(update.TypeIdToName);
    AddMethodIdWithName(update.MethodIdToFqn);
  }

  private void AddMethodIdWithName(MethodIdToFqn? updateMethodIdToFqn)
  {
    if (updateMethodIdToFqn is { })
    {
      myTypeIdsToNames[updateMethodIdToFqn.Value.Id] = updateMethodIdToFqn.Value.Fqn;
    }
  }

  private void AddStack(StackTraceInfo? stack)
  {
    if (stack is { })
    {
      myStacks[stack.StackTraceId] = stack;
    }
  }

  private void AddTypeIdWithName(TypeIdToName? recordTypeIdToName)
  {
    if (recordTypeIdToName.HasValue)
    {
      myTypeIdsToNames[recordTypeIdToName.Value.Id] = recordTypeIdToName.Value.Name;
    }
  }

  public void MergeWith(SessionGlobalData other)
  {
    myTypeIdsToNames.MergeOrThrow(other.myTypeIdsToNames);
    myStacks.MergeOrThrow(other.myStacks);
    myMethodIdToFqn.MergeOrThrow(other.myMethodIdToFqn);
  }
}