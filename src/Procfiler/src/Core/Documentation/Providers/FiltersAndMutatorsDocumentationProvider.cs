using Procfiler.Core.Documentation.Markdown;
using Procfiler.Core.EventsProcessing.Filters.Core;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Documentation.Providers;

[AppComponent]
public class FiltersAndMutatorsDocumentationProvider : IMarkdownDocumentationProvider
{
  private readonly IEnumerable<IEventsFilter> myFilters;
  private readonly IEnumerable<IEventsLogMutator> myMutators;


  public FiltersAndMutatorsDocumentationProvider(
    IEnumerable<IEventsFilter> filters,
    IEnumerable<IEventsLogMutator> mutators)
  {
    myFilters = filters;
    myMutators = mutators;
  }

  
  public MdDocument CreateDocumentationFile()
  {
    var allMutations = GetAllMutations();
    var allEventNames = GetEventsNames(allMutations);
    var eventClassToSingleEventMutationsMap = CreateEventClassToEventMutationsMap(allMutations);
    var headerCells = CreateHeaderCells();
    var table = new MdTable(headerCells.Count);
    
    foreach (var eventName in allEventNames.OrderBy(static name => name))
    {
      if (eventClassToSingleEventMutationsMap.TryGetValue(eventName, out var mutations))
      {
        table.Add(CreateRow(mutations, eventName));
      }
      else
      {
        table.Add(CreateEmptyRow(eventName, headerCells.Count));
      }
    }

    table.Header = headerCells.ToArray();
    return new MdDocument("EventsProcessing")
    {
      table
    };
  }

  private static string[] CreateEmptyRow(string eventName, int headerCount) =>
    Enumerable.Range(0, headerCount - 1).Select(_ => string.Empty).Prepend(eventName).ToArray();

  private static ICollection<MdTableCell> CreateHeaderCells() => new List<MdTableCell>
  {
    new("Event name"),
    new("New event name after attributes to name move"),
    new("Activity name"),
    new("Lifecycle transition"),
    new("Attributes renames"),
    new("Attributes which will be removed"),
    new("Attributes which will be added")
  };
  
  private IList<EventLogMutation> GetAllMutations() => myMutators.SelectMany(mutator => mutator.Mutations).ToList();

  private IEnumerable<string> GetEventsNames(IEnumerable<EventLogMutation> allMutations)
  {
    return allMutations
      .Select(m => m.EventClass)
      .Concat(myFilters.SelectMany(filter => filter.AllowedEventsNames))
      .ToHashSet();
  }

  private static Dictionary<string, List<EventLogMutation>> CreateEventClassToEventMutationsMap(
    IEnumerable<EventLogMutation> allMutations)
  {
    var eventsMutations = new Dictionary<string, List<EventLogMutation>>();
    foreach (var eventLogMutation in allMutations)
    {
      eventsMutations
        .GetOrCreate(eventLogMutation.EventClass, static () => new List<EventLogMutation>())
        .Add(eventLogMutation);
    }
    
    return eventsMutations;
  }

  private static string[] CreateRow(IEnumerable<EventLogMutation> mutations, string eventName)
  {
    var listOfMutations = mutations.ToList();

    var attributesToRemoveCell = CreateAttributesToRemoveCell(listOfMutations);
    var newEventNameCell = CreateNewNameCell(listOfMutations);
    var lifecycleTransition = CreateLifecycleTransition(listOfMutations);
    var activityName = CreateActivityNameCell(listOfMutations);
    var newAttributesCell = CreateNewAttributesCell(listOfMutations);
    var renamesCell = CreateAttributeRenamesCell(listOfMutations);
    
    return new[]
    {
      eventName, newEventNameCell, activityName, lifecycleTransition, renamesCell, attributesToRemoveCell, newAttributesCell
    };
  }

  private static string CreateAttributeRenamesCell(ICollection<EventLogMutation> mutations)
  {
    var renames = mutations.OfType<AttributeRenameMutation>().Select(m => $"{m.OldName} -> {m.NewName}").ToList();
    return (renames.Count > 0) switch
    {
      true => string.Join('\n', renames),
      false => string.Empty
    };
  }
  
  private static string CreateNewAttributesCell(ICollection<EventLogMutation> mutations)
  {
    var newAttributes = mutations.OfType<NewAttributeCreationMutation>().Select(m => m.AttributeName);
    return string.Join(", ", newAttributes);
  }
  
  private static string CreateActivityNameCell(ICollection<EventLogMutation> mutations)
  {
    return mutations.OfType<ActivityIdCreation>().Select(m => m.ActivityIdTemplate).FirstOrDefault() switch
    {
      { } template => template,
      _ => string.Empty
    };
  }
  
  private static string CreateLifecycleTransition(ICollection<EventLogMutation> mutations)
  {
    var lifecycleTransitionMutation = mutations.OfType<AddLifecycleTransitionAttributeMutation>().FirstOrDefault();
    
    return lifecycleTransitionMutation switch
    {
      null => string.Empty,
      _ => lifecycleTransitionMutation.LifecycleTransition
    };
  }
  
  private static string CreateNewNameCell(ICollection<EventLogMutation> list)
  {
    var attributesToName = list.OfType<AttributeToNameAppendMutation>().Select(m => m.AttributeName).ToList();

    if (attributesToName.Count <= 0) return string.Empty;
    
    const string BaseName = "BaseName";
    return $"{BaseName}_{string.Join('_', attributesToName)}";
  }
  
  private static string CreateAttributesToRemoveCell(ICollection<EventLogMutation> mutations)
  {
    var attributesToRemoveWhenAppendingToName = mutations
      .OfType<AttributeToNameAppendMutation>()
      .Where(m => m.RemoveFromMetadata)
      .Select(m => m.AttributeName);
    
    var attributesToRemove = mutations
      .OfType<AttributeRemovalMutation>()
      .Select(m => m.AttributeName)
      .Concat(attributesToRemoveWhenAppendingToName);
    
    return string.Join(", ", attributesToRemove);
  }
}