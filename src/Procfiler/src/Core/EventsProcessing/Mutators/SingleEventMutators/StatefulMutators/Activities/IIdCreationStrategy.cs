using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventRecord;

namespace Procfiler.Core.EventsProcessing.Mutators.SingleEventMutators.StatefulMutators.Activities;

public interface IIdCreationStrategy
{
  public string CreateIdTemplate();
  public string CreateId(EventRecordWithMetadata eventRecord);
}

public class DefaultIdCreationStrategy : IIdCreationStrategy
{
  private readonly string myBasePart;
  private readonly IReadOnlySet<string> myStartEvents;
  private int myNextId;
  

  public DefaultIdCreationStrategy(string basePart, IReadOnlySet<string> startEvents)
  {
    myBasePart = basePart;
    myStartEvents = startEvents;
  }


  public string CreateIdTemplate() => DoCreateId("{AUTOINCREMENT_ID}");
  
  private string DoCreateId(string nextId)
  {
    return $"{myBasePart}_{nextId}";
  }

  public string CreateId(EventRecordWithMetadata eventRecord)
  {
    if (myStartEvents.Contains(eventRecord.EventClass))
    {
      myNextId++;
    }

    return DoCreateId(myNextId.ToString());
  }
}

public class FromAttributesIdCreationStrategy : IIdCreationStrategy
{
  private readonly string myBasePart;
  private readonly ICollection<string> myAttributesToUse;
  
  
  public FromAttributesIdCreationStrategy(string basePart, ICollection<string> attributesToUse)
  {
    myBasePart = basePart;
    myAttributesToUse = attributesToUse;
  }


  public string CreateIdTemplate() => DoCreateId(myBasePart, myAttributesToUse);

  private static string DoCreateId(string basePart, IEnumerable<string> attributes)
  {
    var basePartSb = new StringBuilder(basePart);

    foreach (var attribute in attributes)
    {
      basePartSb.Append(TraceEventsConstants.Underscore).Append(attribute);
    }

    return basePartSb.ToString();
  }
  
  public string CreateId(EventRecordWithMetadata eventRecord)
  {
    var attributeValues = new List<string>();
    foreach (var attributeName in myAttributesToUse)
    {
      if (!eventRecord.Metadata.TryGetValue(attributeName, out var value))
      {
        throw new KeyNotFoundException(attributeName);
      }
      
      attributeValues.Add(value);
    }

    return DoCreateId(myBasePart, attributeValues);
  }
}

public class FromEventActivityIdIdCreationStrategy : IIdCreationStrategy
{
  private readonly string myBasePart;


  public FromEventActivityIdIdCreationStrategy(string basePart)
  {
    myBasePart = basePart;
  }


  public string CreateIdTemplate() => $"{myBasePart}_GUID";

  public string CreateId(EventRecordWithMetadata eventRecord) => eventRecord.ActivityId.ToString();
}