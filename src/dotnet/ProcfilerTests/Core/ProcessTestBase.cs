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
    StartProcessAndDoTest(solution.CreateContext(), testAction);
  }

  protected void StartProcessAndDoTest(CollectClrEventsContext context, Action<CollectedEvents> testAction)
  {
    Container.Resolve<ICommandExecutorDependantOnContext>().Execute(context, testAction);
  }

  protected void StartProcessSplitEventsByThreadsAndDoTest(
    KnownSolution solution, Action<Dictionary<long, IEventsCollection>, SessionGlobalData> testFunc)
  {
    StartProcessAndDoTestWithDefaultContext(solution, events =>
    {
      var eventsByThreads = SplitEventsHelper.SplitByKey(
        TestLogger.CreateInstance(), events.Events, SplitEventsHelper.ManagedThreadIdExtractor);
      testFunc(eventsByThreads, events.GlobalData);
    });
  }
}