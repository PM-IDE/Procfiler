using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;

namespace Procfiler.Core;

public class OrderedEventsEnumerator : IEnumerable<EventRecordWithPointer>, IEnumerator<EventRecordWithPointer>
{
  private struct EnumeratorState
  {
    public required IEnumerator<EventRecordWithPointer> Enumerator { get; init; }
    public bool Finished { get; set; }
  }

  private readonly EnumeratorState[] myStates;
  private readonly IEnumerable<EventRecordWithPointer>[] myCollections;
  private readonly PriorityQueue<int, long> myQueue;

  private int myLastReturnedEnumerator = -1;


  public EventRecordWithPointer Current { get; private set; }
  object IEnumerator.Current => Current;

  
  /// <invariant name = "EventsMustBeOrderedByStamp">
  /// The Lists of events in eventsByManagedThreads should be sorted by "Stamp" property
  /// </invariant>
  public OrderedEventsEnumerator(IEnumerable<IEnumerable<EventRecordWithPointer>> eventsByTraces)
  {
    //reference to invariant: EventsMustBeOrderedByStamp
    myCollections = eventsByTraces.ToArray();
    myStates = new EnumeratorState[myCollections.Length];
    myQueue = new PriorityQueue<int, long>();
    
    Reset();
  }

  
  public bool MoveNext()
  {
    if (myLastReturnedEnumerator != -1)
    {
      var lastUsedState = myStates[myLastReturnedEnumerator];
      if (lastUsedState.Enumerator.MoveNext())
      {
        myQueue.Enqueue(myLastReturnedEnumerator, lastUsedState.Enumerator.Current.Event.Stamp);
      }
      else
      {
        lastUsedState.Finished = true;
      }
    }
    
    while (true)
    {
      if (myQueue.Count == 0) return false;
      
      var next = myQueue.Dequeue();
      ref var currentEnumeratorState = ref myStates[next];
      
      if (currentEnumeratorState.Finished) continue;

      var enumerator = currentEnumeratorState.Enumerator;
      Current = enumerator.Current;
      myLastReturnedEnumerator = next;
      
      return true;
    }
  }

  public void Reset()
  {
    myQueue.Clear();
    for (var i = 0; i < myCollections.Length; i++)
    {
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
      else
      {
        myQueue.Enqueue(i, state.Enumerator.Current.Event.Stamp);
      }
    }
  }
  
  public IEnumerator<EventRecordWithPointer> GetEnumerator() => this;

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public void Dispose()
  {
  }
}