using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.EventsProcessing;
using Procfiler.Utils;

namespace ProcfilerTests.Core;

public static class TestUtil
{
  public static IEventsCollection FindEventsForMainThread(IEventsCollection events)
  {
    var eventsByThreads = SplitEventsHelper.SplitByKey(
      TestLogger.CreateInstance(), events, SplitEventsHelper.ManagedThreadIdExtractor);
    
    return FindEventsForMainThread(eventsByThreads);
  }

  public static IEventsCollection FindEventsForMainThread(
    Dictionary<int, IEventsCollection> eventsByThreads)
  {
    var (_, mainThreadEvents) = eventsByThreads.Where(e => e.Key != -1).MaxBy(e => e.Value.Count);
    return mainThreadEvents;
  }
  
  public static EventsProcessingContext CreateEventsProcessingContext(
    IEventsCollection managedThreadEvents, 
    SessionGlobalData globalData)
  {
    var config = new EventsProcessingConfig(false, true, EmptyCollections<Type>.EmptySet);
    return new EventsProcessingContext(managedThreadEvents, globalData, config);
  }

  public static void CheckMethodConsistencyOrThrow(IEventsCollection events)
  {
    var frames = new Stack<string>();
    foreach (var eventRecord in events)
    {
      if (eventRecord.TryGetMethodStartEndEventInfo() is var (frame, isStart))
      {
        if (isStart)
        {
          frames.Push(frame);
        }
        else
        {
          var topMost = frames.Pop();
          if (topMost != frame)
          {
            Assert.Fail($"{topMost} != {frame}");
          }
        }
      }
    }

    if (frames.Count != 0)
    {
      Assert.Fail("frames.Count != 0");
    }
  }

  public static EventRecordWithMetadata CreateRandomEvent(string eventClass, EventMetadata metadata)
  {
    var randomStamp = Random.Shared.NextInt64(long.MaxValue);
    var randomManagedThreadId = Random.Shared.Next(10) - 1;
    return new EventRecordWithMetadata(randomStamp, eventClass, randomManagedThreadId, metadata);
  }

  public static EventRecordWithMetadata CreateAbsolutelyRandomEvent()
  {
    var randomStamp = Random.Shared.NextInt64(long.MaxValue);
    var randomManagedThreadId = Random.Shared.Next(10) - 1;
    return new EventRecordWithMetadata(
      randomStamp, CreateRandomEventClass(), randomManagedThreadId, new EventMetadata());
  }

  public static string CreateRandomEventClass()
  {
    var chars = Enumerable
      .Range(0, Random.Shared.Next(1, 13))
      .Select(_ => (char) Random.Shared.Next('a', 'z' + 1))
      .ToArray();

    return new string(chars);
  }

  public static void AssertCollectionsAreSame<TValue>(ICollection<TValue> first, ICollection<TValue> second)
  {
    Assert.That(first.Count, Is.EqualTo(second.Count));
    foreach (var (firstItem, secondItem) in first.Zip(second))
    {
      Assert.That(firstItem is { }, Is.True);
      Assert.That(secondItem is { }, Is.True);
      
      Assert.That(firstItem!.Equals(secondItem), Is.True);
    }
  }
}