using Autofac;
using JetBrains.Lifetimes;
using Procfiler.Commands.CollectClrEvents;
using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.Collector;
using Procfiler.Core.EventsCollection;
using TestsUtil;

namespace ProcfilerTests.Core;

public abstract class ProcessTestBase : TestWithContainerBase
{
  protected static IEnumerable<KnownSolution> Source() => KnownSolution.AllSolutions;
  

  protected void StartProcessAndDoTest(
    KnownSolution solution, Func<CollectedEvents, Lifetime, ValueTask> testFunc)
  {
    var pathToSolutionSource = TestPaths.CreatePathToSolutionsSource();
    var context = solution.CreateContext(pathToSolutionSource);

    Container.Resolve<ICommandExecutorDependantOnContext>().Execute(context, testFunc).AsTask().Wait();
  }

  protected void StartProcessSplitEventsByThreadsAndDoTest(
    KnownSolution solution, Action<Dictionary<long, IEventsCollection>, SessionGlobalData> testFunc)
  {
    StartProcessAndDoTest(solution, (events, _) =>
    {
      var eventsByThreads = SplitEventsHelper.SplitByKey(
        TestLogger.CreateInstance(), events.Events, SplitEventsHelper.ManagedThreadIdExtractor);
      testFunc(eventsByThreads, events.GlobalData);
      return ValueTask.CompletedTask;
    });
  }
}