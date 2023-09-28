using Autofac;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsProcessing;
using Procfiler.Utils;
using ProcfilerTests.Core;

namespace ProcfilerTests.Tests;

[TestFixture]
public class EventTimeStampsConsistencyTest : ProcessTestBase
{
  [TestCaseSource(nameof(DefaultContexts))]
  public void Test(ContextWithSolution dto) => DoTest(dto);


  private void DoTest(ContextWithSolution dto)
  {
    StartProcessSplitEventsByThreadsAndDoTest(dto.Context, (eventsByThreads, globalData) =>
    {
      foreach (var (_, events) in eventsByThreads)
      {
        var processor = Container.Resolve<IUnitedEventsProcessor>();
        processor.ApplyMultipleMutators(events, globalData, EmptyCollections<Type>.EmptySet);

        EventRecordWithMetadata? prev = null;
        foreach (var (_, currentEvent) in events)
        {
          if (prev is null)
          {
            prev = currentEvent;
          }
          else
          {
            if (prev.ManagedThreadId != currentEvent.ManagedThreadId)
            {
              Assert.Fail("Managed thread ids were not equal");
            }

            if (prev.Stamp > currentEvent.Stamp)
            {
              Assert.Fail("first.Value.Stamp > second.Value.Stamp");
            }
          }
        }
      }
    });
  }
}