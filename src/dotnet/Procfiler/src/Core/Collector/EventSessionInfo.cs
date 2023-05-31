﻿using Procfiler.Core.EventsCollection;
using Procfiler.Utils;

namespace Procfiler.Core.Collector;

public record EventSessionInfo(IEnumerable<IEventsCollection> Events, SessionGlobalData GlobalData);

public class SessionGlobalData
{
  private readonly Dictionary<long, string> myMethodIdToFqn;
  private readonly Dictionary<long, string> myTypeIdsToNames;

  
  public IReadOnlyDictionary<long, string> TypeIdToNames => myTypeIdsToNames;
  public IReadOnlyDictionary<long, string> MethodIdToFqn => myMethodIdToFqn;
  public IShadowStacks Stacks { get; }


  public SessionGlobalData(IShadowStacks shadowStacks)
  {
    Stacks = shadowStacks;
    myMethodIdToFqn = new Dictionary<long, string>();
    myTypeIdsToNames = new Dictionary<long, string>();
  }


  public void AddInfoFrom(EventWithGlobalDataUpdate update)
  {
    AddTypeIdWithName(update.TypeIdToName);
    AddMethodIdWithName(update.MethodIdToFqn);

    if (Stacks is IFromEventsShadowStacks fromEventsShadowStacks)
    {
      fromEventsShadowStacks.AddStack(update.OriginalEvent);
    }
  }

  private void AddMethodIdWithName(MethodIdToFqn? updateMethodIdToFqn)
  {
    if (updateMethodIdToFqn is { })
    {
      myMethodIdToFqn[updateMethodIdToFqn.Value.Id] = updateMethodIdToFqn.Value.Fqn;
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
    myMethodIdToFqn.MergeOrThrow(other.myMethodIdToFqn);
  }
}