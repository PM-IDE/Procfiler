using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;

namespace Procfiler.Core;

public class OrderedEventsEnumerator : IEnumerable<EventRecordWithMetadata>, IEnumerator<EventRecordWithMetadata>
{
  private struct EnumeratorState
  {
    public required IEnumerator<EventRecordWithPointer> Enumerator { get; init; }
    public bool Finished { get; set; }
  }

  private readonly EnumeratorState[] myStates;
  private readonly IEventsCollection[] myCollections;
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
    myStates = new EnumeratorState[myCollections.Length];
    myQueue = new PriorityQueue<int, long>();
    
    Reset();
  }

  
  public bool MoveNext()
  {
    while (true)
    {
      if (myQueue.Count == 0) return false;
      
      var next = myQueue.Dequeue();
      ref var currentEnumeratorState = ref myStates[next];
      
      if (currentEnumeratorState.Finished) continue;

      var enumerator = currentEnumeratorState.Enumerator;
      Current = enumerator.Current.Event;
      
      if (enumerator.MoveNext())
      {
        myQueue.Enqueue(next, enumerator.Current.Event.Stamp);
      }
      else
      {
        currentEnumeratorState.Finished = true;
      }
      
      return true;
    }
  }

  public void Reset()
  {
    myQueue.Clear();
    for (var i = 0; i < myCollections.Length; i++)
    {
      myStates[i].Enumerator.Dispose();
      myStates[i] = new EnumeratorState
      {
        Enumerator = myCollections[i].GetEnumerator()
      };
    }
    
    for (var i = 0; i < myCollections.Length; i++)
    {
      ref var state = ref myStates[i];
      if (!state.Enumerator.MoveNext())
      {
        state.Finished = true;
      }
    }
  }
  
  public IEnumerator<EventRecordWithMetadata> GetEnumerator() => this;

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public void Dispose()
  {
  }
}