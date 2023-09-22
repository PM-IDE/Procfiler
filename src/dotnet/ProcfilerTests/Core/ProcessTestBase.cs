using Autofac;
using Procfiler.Commands.CollectClrEvents;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.Collector;
using Procfiler.Core.EventsCollection;
using TestsUtil;

namespace ProcfilerTests.Core;

public abstract class ProcessTestBase : TestWithContainerBase
{
  protected void StartProcessAndDoTestWithDefaultContext(
    KnownSolution solution, Action<CollectedEvents> testAction)
  {
    StartProcessAndDoTest(solution.CreateContexts(), testAction);
  }

  protected void StartProcessAndDoTest(IEnumerable<CollectClrEventsContext> contexts, Action<CollectedEvents> testAction)
  {
    foreach (var context in contexts)
    {
      Container.Resolve<ICommandExecutorDependantOnContext>().Execute(context, testAction);
    }
  }

  protected void StartProcessSplitEventsByThreadsAndDoTest(
    KnownSolution solution, Action<Dictionary<long, IEventsCollection>, SessionGlobalData> testFunc)
  {
    StartProcessAndDoTestWithDefaultContext(solution, events =>
    {
      var extractor = SplitEventsHelper.ManagedThreadIdExtractor;
      var eventsByThreads = SplitEventsHelper.SplitByKey(TestLogger.CreateInstance(), events.Events, extractor);

      testFunc(eventsByThreads, events.GlobalData);
    });
  }
}