using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;

namespace Procfiler.Core;

public class OrderedEventsEnumerator : IEnumerable<EventRecordWithMetadata>, IEnumerator<EventRecordWithMetadata>
{
  private readonly IEventsCollection[] myCollections;
  private readonly EventPointer?[] myFirstValues;
  private readonly EventPointer?[] myNextEventsNodes;
  private readonly PriorityQueue<int, long> myQueue;


  public EventRecordWithMetadata Current { get; private set; } = null!;
  object IEnumerator.Current => Current;

  
  /// <invariant name = "EventsMustBeOrderedByStamp">
  /// The Lists of events in eventsByManagedThreads should be sorted by "Stamp" property
  /// </invariant>
  public OrderedEventsEnumerator(IEnumerable<IEventsCollection> eventsByTraces)
  {
    //reference to invariant: EventsMustBeOrderedByStamp
    myCollections = eventsByTraces.ToArray();
    myNextEventsNodes = new EventPointer?[myCollections.Length];
    myQueue = new PriorityQueue<int, long>();

    myFirstValues = new EventPointer?[myCollections.Length];
    var index = 0;
    foreach (var list in myCollections)
    {
      myFirstValues[index++] = list.First;
    }

    Reset();
  }

  
  public bool MoveNext()
  {
    while (true)
    {
      if (myQueue.Count == 0) return false;
      var next = myQueue.Dequeue();
      var currentPointer = myNextEventsNodes[next];

      if (currentPointer is { } && 
          myCollections[next].TryGetForWithDeletionCheck(currentPointer.Value) is { } eventRecord)
      {
        Current = eventRecord;
        
        if (myCollections[next].NextNotDeleted(currentPointer.Value) is { } nextNode &&
            myCollections[next].TryGetForWithDeletionCheck(nextNode) is { } nextEvent)
        {
          myNextEventsNodes[next] = nextNode;
          myQueue.Enqueue(next, nextEvent.Stamp); 
        }
        else
        {
          myNextEventsNodes[next] = null;
        }
        
        return true;
      }
    }
  }

  public void Reset()
  {
    myQueue.Clear();
    for (var i = 0; i < myCollections.Length; i++)
    {
      var firstValue = myFirstValues[i];

      if (firstValue is { } &&
          myCollections[i].TryGetForWithDeletionCheck(firstValue.Value) is { } eventRecord)
      {
        myNextEventsNodes[i] = firstValue;
        myQueue.Enqueue(i, eventRecord.Stamp);
      }
      else
      {
        myNextEventsNodes[i] = null;
      }
    }
  }
  
  public IEnumerator<EventRecordWithMetadata> GetEnumerator() => this;

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public void Dispose()
  {
  }
}