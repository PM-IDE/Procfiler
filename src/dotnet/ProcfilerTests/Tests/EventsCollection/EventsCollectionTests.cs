using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using ProcfilerTests.Core;

namespace ProcfilerTests.Tests.EventsCollection;

[TestFixture]
public class EventsCollectionTests
{
  [Test]
  public void TestEnumeration()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);
    
    Assert.That(collection, Has.Count.EqualTo(events.Length));
    foreach (var (arrayEvent, collectionEvent) in events.Zip(collection))
    {
      Assert.That(ReferenceEquals(arrayEvent, collectionEvent), Is.True);
    }
  }

  [Test]
  public void TestInsertionInBeginning()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);
    var eventsToInsert = CreateInitialArrayOfRandomEvents(10);
    
    collection.ApplyNotPureActionForAllEvents((ptr, _) =>
    {
      foreach (var eventRecord in eventsToInsert)
      {
        collection.InsertBefore(ptr, eventRecord);
      }
      
      return true;
    });

    Assert.That(collection, Has.Count.EqualTo(events.Length + eventsToInsert.Length));
    AssertCollectionsAreSame(collection, eventsToInsert.Concat(events));
  }

  [Test]
  public void TestInsertBefore()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);
    var index = Random.Shared.Next(collection.Count);
    var eventRecord = TestUtil.CreateAbsolutelyRandomEvent();
    var pointerForIndex = EventPointer.ForInitialArray(index);
    var eventRecordAtIndex = collection.TryGetForWithDeletionCheck(pointerForIndex);
    Assert.That(eventRecordAtIndex, Is.Not.Null);
    
    collection.InsertBefore(EventPointer.ForInitialArray(index), eventRecord);

    var eventRecordAtIndexAfterInsertion = collection.TryGetForWithDeletionCheck(pointerForIndex);
    Assert.That(ReferenceEquals(eventRecordAtIndex, eventRecordAtIndexAfterInsertion), Is.True);
    Assert.That(collection, Has.Count.EqualTo(events.Length + 1));
    
    var list = events.ToList();
    list.Insert(index, eventRecord);

    AssertCollectionsAreSame(collection, list);
  }

  [Test]
  public void TestInsertAfter()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);
    var index = Random.Shared.Next(collection.Count);
    var eventRecord = TestUtil.CreateAbsolutelyRandomEvent();
    var pointerForIndex = EventPointer.ForInitialArray(index);
    var eventRecordAtIndex = collection.TryGetForWithDeletionCheck(pointerForIndex);
    Assert.That(eventRecordAtIndex, Is.Not.Null);
    
    collection.InsertAfter(EventPointer.ForInitialArray(index), eventRecord);

    var eventRecordAtIndexAfterInsertion = collection.TryGetForWithDeletionCheck(pointerForIndex);
    Assert.That(ReferenceEquals(eventRecordAtIndex, eventRecordAtIndexAfterInsertion), Is.True);
    Assert.That(collection, Has.Count.EqualTo(events.Length + 1));

    var list = events.ToList();
    list.Insert(index + 1, eventRecord);
    
    AssertCollectionsAreSame(collection, list);
  }

  private static void AssertCollectionsAreSame(
    IEventsCollection collection, IEnumerable<EventRecordWithMetadata> events)
  {
    foreach (var (collectionEvent, listEvent) in collection.Zip(events))
    {
      Assert.That(ReferenceEquals(collectionEvent, listEvent), Is.True);
    }
  }

  [Test]
  public void TestRemoval()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);
    var index = Random.Shared.Next(collection.Count);
    
    collection.Remove(collection.TryGetForWithDeletionCheck(EventPointer.ForInitialArray(index))!);
    
    Assert.That(collection, Has.Count.EqualTo(events.Length - 1));
    var list = events.ToList();
    list.RemoveAt(index);
    
    AssertCollectionsAreSame(collection, list);
  }

  [Test]
  public void TestMultipleInsertAfter()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);
    var eventsToInsert = CreateInitialArrayOfRandomEvents();

    var index = 0;
    var added = false;
    collection.ApplyNotPureActionForAllEvents((ptr, _) =>
    {
      if (!added)
      {
        added = true;
        collection.InsertAfter(ptr, eventsToInsert[index++]);
      }
      else
      {
        added = false;
      }
      
      return false;
    });

    var expectedResult = new List<EventRecordWithMetadata>();
    foreach (var (initialEvent, insertedEvent) in events.Zip(eventsToInsert))
    {
      expectedResult.Add(initialEvent);
      expectedResult.Add(insertedEvent);
    }
    
    AssertCollectionsAreSame(collection, expectedResult);
  }

  [Test]
  public void TestMultipleInsertBefore()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);
    var eventsToInsert = CreateInitialArrayOfRandomEvents();

    var index = 0;
    collection.ApplyNotPureActionForAllEvents((ptr, _) =>
    {
      collection.InsertBefore(ptr, eventsToInsert[index++]);
      return false;
    });

    var expectedResult = new List<EventRecordWithMetadata>();
    foreach (var (insertedEvent, initialEvent) in eventsToInsert.Zip(events))
    {
      expectedResult.Add(insertedEvent);
      expectedResult.Add(initialEvent);
    }
    
    AssertCollectionsAreSame(collection, expectedResult);
  }

  [Test]
  public void TestFirst()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);
    
    Assert.That(collection, Has.Count.EqualTo(events.Length));
    Assert.That(ReferenceEquals(collection.TryGetForWithDeletionCheck(collection.First!.Value), events[0]), Is.True);
  }

  [Test]
  public void TestLast()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);
    
    Assert.That(collection, Has.Count.EqualTo(events.Length));
    Assert.That(ReferenceEquals(collection.TryGetForWithDeletionCheck(collection.Last!.Value), events[^1]), Is.True);
  }

  [Test]
  public void TestFreezeOfCollections()
  {
    var collection = CreateNewCollection(CreateInitialArrayOfRandomEvents());

    var randomEvent = TestUtil.CreateAbsolutelyRandomEvent();
    var actions = new TestDelegate[]
    {
      () => collection.InsertAfter(collection.First!.Value, randomEvent),
      () => collection.InsertBefore(collection.First!.Value, randomEvent),
      () => collection.Remove(collection.GetFor(collection.First!.Value))
    };
    
    collection.Freeze();
    foreach (var action in actions)
    {
      Assert.Throws<CollectionIsFrozenException>(action);
    }

    collection.UnFreeze();
    foreach (var action in actions)
    {
      Assert.DoesNotThrow(action);
    }
  }

  [Test]
  public void TestInsertBeforeFirst()
  {
    var events = CreateInitialArrayOfRandomEvents(1);
    var collection = CreateNewCollection(events);
    
    Assert.That(collection, Has.Count.EqualTo(1));
    var eventToInsert = TestUtil.CreateAbsolutelyRandomEvent();
    collection.InsertBefore(collection.First!.Value, eventToInsert);
    Assert.That(collection, Has.Count.EqualTo(2));
    AssertCollectionsAreSame(collection, new[] { eventToInsert }.Concat(events));
  }
  
  [Test]
  public void TestInsertAfterLast()
  {
    var events = CreateInitialArrayOfRandomEvents(1);
    var collection = CreateNewCollection(events);
    
    Assert.That(collection, Has.Count.EqualTo(1));
    var eventToInsert = TestUtil.CreateAbsolutelyRandomEvent();
    collection.InsertAfter(collection.First!.Value, eventToInsert);
    Assert.That(collection, Has.Count.EqualTo(2));
    AssertCollectionsAreSame(collection, events.Concat(new[] { eventToInsert }));
  }

  [Test]
  public void TestNextNotDeleted()
  {
    var (collection, events) = CreateDefaultSizeCollectionAndDeleteAllExceptFirstAndLast();
    Assert.That(collection, Has.Count.EqualTo(2));
    var first = collection.First;
    Assert.That(first, Is.Not.Null);
    var nextNotDeleted = collection.NextNotDeleted(first!.Value);
    Assert.Multiple(() =>
    {
      Assert.That(collection.Last, Is.Not.Null);
      Assert.That(nextNotDeleted, Is.EqualTo(collection.Last!.Value));
      Assert.That(ReferenceEquals(events.First(), collection.GetFor(collection.First!.Value)), Is.True);
      Assert.That(ReferenceEquals(events.Last(), collection.GetFor(collection.Last.Value)), Is.True);
    });
  }

  private static (IEventsCollection, EventRecordWithMetadata[]) CreateDefaultSizeCollectionAndDeleteAllExceptFirstAndLast()
  {
    var events = CreateInitialArrayOfRandomEvents();
    var collection = CreateNewCollection(events);

    collection.ApplyNotPureActionForAllEvents((ptr, eventRecord) =>
    {
      if (ptr == collection.First || ptr == collection.Last) return false;

      collection.Remove(eventRecord);
      return false;
    });

    return (collection, events);
  }

  [Test]
  public void TestPrevNotDeleted()
  {
    var (collection, events) = CreateDefaultSizeCollectionAndDeleteAllExceptFirstAndLast();
    Assert.That(collection, Has.Count.EqualTo(2));
    var last = collection.Last;
    Assert.That(last, Is.Not.Null);
    var prevNotDeleted = collection.PrevNotDeleted(last!.Value);
    Assert.Multiple(() =>
    {
      Assert.That(collection.Last, Is.Not.Null);
      Assert.That(prevNotDeleted, Is.EqualTo(collection.First!.Value));
      Assert.That(ReferenceEquals(events.First(), collection.GetFor(collection.First.Value)), Is.True);
      Assert.That(ReferenceEquals(events.Last(), collection.GetFor(collection.Last!.Value)), Is.True);
    });
  }

  private static IEventsCollection CreateNewCollection(EventRecordWithMetadata[] events) =>
    new EventsCollectionImpl(events, TestLogger.CreateInstance());
  
  private static EventRecordWithMetadata[] CreateInitialArrayOfRandomEvents(int count = 200) =>
    Enumerable.Range(0, count).Select(_ => TestUtil.CreateAbsolutelyRandomEvent()).ToArray();
}