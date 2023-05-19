using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.Exceptions;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.SplitByMethod;

public readonly record struct ManagedEventsExtractionResult(
  IEnumerable<KeyValuePair<int, IEventsCollection>> NewManagedEvents,
  IEventsCollection NewUndefinedEvents
);

public interface IManagedEventsFromUndefinedThreadExtractor
{
  IEventsCollection Extract(
    IDictionary<int, IEventsCollection> managedThreadEventsById, IEventsCollection undefinedEvents);
}

[AppComponent]
public class ManagedEventsFromUndefinedThreadExtractor : IManagedEventsFromUndefinedThreadExtractor
{
  private readonly IProcfilerLogger myLogger;


  public ManagedEventsFromUndefinedThreadExtractor(IProcfilerLogger logger)
  {
    myLogger = logger;
  }

  
  public IEventsCollection Extract(
    IDictionary<int, IEventsCollection> managedThreadEventsById, IEventsCollection undefinedEvents)
  {
    var (newManagedEvents, newUndefinedEvents) = ExtractFrom(undefinedEvents);
    
    foreach (var (key, value) in newManagedEvents)
    {
      if (managedThreadEventsById.ContainsKey(key))
        throw new InvalidStateException($"Managed events already contain thread {key}");

      managedThreadEventsById[key] = value;
    }

    return new EventsCollectionImpl(newUndefinedEvents.ToArray(), myLogger);
  }
  
  private ManagedEventsExtractionResult ExtractFrom(IEventsCollection undefinedEvents)
  {
    var newUndefinedEvents = new List<EventRecordWithMetadata>();
    var managedThreadsTraces = new Dictionary<int, List<EventRecordWithMetadata>>();
    var currentThreadId = -1;
    
    foreach (var eventRecord in undefinedEvents)
    {
      if (eventRecord.TryGetMethodStartEndEventInfo() is not var (frame, isStart))
      {
        if (currentThreadId != -1 && eventRecord.IsMethodStartOrEndEvent())
        {
          managedThreadsTraces[currentThreadId].Add(eventRecord);
        }
        else
        {
          newUndefinedEvents.Add(eventRecord);
        }
        
        continue;
      }
      
      if (!TraceEventsExtensions.IsThreadStartMethod(frame, out var managedThreadId))
      {
        newUndefinedEvents.Add(eventRecord);
        continue;
      }
      
      if (currentThreadId != -1)
        throw new MethodStartEndConsistencyBrokenException();

      if (isStart)
      {
        currentThreadId = managedThreadId;
        if (managedThreadsTraces.ContainsKey(managedThreadId))
        {
          throw new InvalidStateException($"Two same managed threads in undefined events {managedThreadId}");
        }

        managedThreadsTraces[managedThreadId] = new List<EventRecordWithMetadata>() { eventRecord };
      }
      else
      {
        managedThreadsTraces[currentThreadId].Add(eventRecord);
        currentThreadId = -1;
      }
    }

    var newUndefinedEventsCollection = new EventsCollectionImpl(newUndefinedEvents.ToArray(), myLogger);
    var newManagedEvents = managedThreadsTraces.Select(
      pair => new KeyValuePair<int, IEventsCollection>(pair.Key, new EventsCollectionImpl(pair.Value.ToArray(), myLogger)));
    
    return new ManagedEventsExtractionResult(newManagedEvents, newUndefinedEventsCollection);
  }
}