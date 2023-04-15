using Autofac;
using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.EventsProcessing.Mutators;
using Procfiler.Utils;
using ProcfilerTests.Core;

namespace ProcfilerTests.Tests;

[TestFixture]
public class MethodStartEndConsistencyTest : ProcessTestBase
{
  [TestCaseSource(nameof(Source))]
  public void Test(KnownSolution knownSolution) => DoTest(knownSolution);
  
  private void DoTest(KnownSolution knownSolution)
  {
    StartProcessAndDoTest(knownSolution, events =>
    {
      var globalData = events.GlobalData;
      var eventsByThreads = SplitEventsHelper.SplitByKey(
        TestLogger.CreateInstance(), events.Events, SplitEventsHelper.ManagedThreadIdExtractor);
      var undefinedThreadEvents = eventsByThreads[-1];
      eventsByThreads.Remove(-1);
      
      Container.Resolve<IUndefinedThreadsEventsMerger>().Merge(eventsByThreads, undefinedThreadEvents);
      foreach (var (_, eventsForThread) in eventsByThreads)
      {
        var processor = Container.Resolve<IUnitedEventsProcessor>(); 
        processor.ApplyMultipleMutators(eventsForThread, globalData, EmptyCollections<Type>.EmptySet);
        TestUtil.CheckMethodConsistencyOrThrow(eventsForThread);
      }

      return ValueTask.CompletedTask;
    });
  }
}