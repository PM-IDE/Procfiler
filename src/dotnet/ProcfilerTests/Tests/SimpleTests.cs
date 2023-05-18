using Procfiler.Commands.CollectClrEvents.Split;
using ProcfilerTests.Core;
using TestsUtil;

namespace ProcfilerTests.Tests;

[TestFixture]
public class SimpleTests : ProcessTestBase
{
  [TestCaseSource(nameof(Source))]
  public void TestSimpleManagedThreadSplitAttributes(KnownSolution knownSolution)
  {
    StartProcessAndDoTest(knownSolution, events =>
    {
      Assert.That(events.Events, Has.Count.GreaterThan(KnownSolution.ConsoleApp1.ExpectedEventsCount));
      var eventsByThreads = SplitEventsHelper.SplitByKey(
        TestLogger.CreateInstance(), events.Events, SplitEventsHelper.ManagedThreadIdExtractor);
      
      var orderedKeys = eventsByThreads.Keys.OrderByDescending(static key => key);
      Assert.That(orderedKeys.Last(), Is.EqualTo(-1));
      
      return ValueTask.CompletedTask;
    });
  }

  [TestCaseSource(nameof(Source))]
  public void TestSimpleSplitByNamesAttributes(KnownSolution knownSolution)
  {
    StartProcessAndDoTest(knownSolution, events =>
    {
      Assert.That(events.Events, Has.Count.GreaterThan(KnownSolution.ConsoleApp1.ExpectedEventsCount));
      var eventsByNames = SplitEventsHelper.SplitByKey(TestLogger.CreateInstance(), events.Events, SplitEventsHelper.EventClassKeyExtractor);
      foreach (var (name, traceEvents) in eventsByNames)
      {
        foreach (var traceEvent in traceEvents)
        {
          Assert.That(traceEvent.EventName.Replace('/', '_'), Is.EqualTo(name));
        }
      }
      
      var count = eventsByNames.Select(e => e.Value.Count).Aggregate((x, y) => x + y);
      Assert.That(count, Is.EqualTo(events.Events.Count));
      
      return ValueTask.CompletedTask;
    });
  }
}