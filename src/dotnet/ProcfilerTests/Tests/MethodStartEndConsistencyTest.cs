using Autofac;
using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.EventsProcessing;
using Procfiler.Utils;
using ProcfilerTests.Core;
using TestsUtil;

namespace ProcfilerTests.Tests;

[TestFixture]
public class MethodStartEndConsistencyTest : ProcessTestBase
{
  [TestCaseSource(nameof(Source))]
  public void Test(KnownSolution knownSolution) => DoTest(knownSolution);
  
  private void DoTest(KnownSolution knownSolution)
  {
    StartProcessAndDoTest(knownSolution, (events, _) =>
    {
      var globalData = events.GlobalData;
      var eventsByThreads = SplitEventsHelper.SplitByKey(
        TestLogger.CreateInstance(), events.Events, SplitEventsHelper.ManagedThreadIdExtractor);
      eventsByThreads.Remove(-1);
      
      foreach (var (threadId, eventsForThread) in eventsByThreads)
      {
        var processor = Container.Resolve<IUnitedEventsProcessor>(); 
        processor.ApplyMultipleMutators(eventsForThread, globalData, EmptyCollections<Type>.EmptySet);
        TestUtil.CheckMethodConsistencyOrThrow(threadId, eventsForThread, globalData, Container);
      }

      return ValueTask.CompletedTask;
    });
  }
}