using Autofac;
using Microsoft.Extensions.Logging;
using Procfiler.Commands.CollectClrEvents;
using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.Collector;
using Procfiler.Core.EventsCollection;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace ProcfilerTests.Core;

public abstract class ProcessTestBase
{
  protected static IEnumerable<KnownSolution> Source() => KnownSolution.AllSolutions;

  
  protected readonly IContainer Container;


  protected ProcessTestBase()
  {
    var assembly = typeof(IClrEventsCollector).Assembly;
    var builder = ProcfilerContainerBuilder.BuildFromAssembly(LogLevel.Trace, assembly);
    builder.RegisterInstance(TestLogger.CreateInstance()).As<IProcfilerLogger>();
    Container = builder.Build();
  }


  protected void StartProcessAndDoTest(
    KnownSolution solution, Func<CollectedEvents, ValueTask> testFunc)
  {
    var pathToSolutionSource = CreatePathToSolutionsSource();
    var context = solution.CreateContext(pathToSolutionSource);
    
    Container.Resolve<ICommandExecutorDependantOnContext>().Execute(context, testFunc).AsTask().Wait();
  }

  protected void StartProcessSplitEventsByThreadsAndDoTest(
    KnownSolution solution, Action<Dictionary<int, IEventsCollection>> testFunc)
  {
    StartProcessAndDoTest(solution, events =>
    {
      var eventsByThreads = SplitEventsHelper.SplitByKey(
        TestLogger.CreateInstance(), events.Events, SplitEventsHelper.ManagedThreadIdExtractor);
      testFunc(eventsByThreads);
      return ValueTask.CompletedTask;
    });
  }

  protected static string CreatePathToTestData()
  {
    var dir = Directory.GetParent(Directory.GetCurrentDirectory())?.Parent?.Parent?.Parent?.Parent;
    var path = Path.Combine(dir!.FullName, "test_data");
    Assert.That(Directory.Exists(path), Is.True);
    return path;
  }
  
  protected static string CreatePathToSolutionsSource()
  {
    var path = Path.Combine(CreatePathToTestData(), "source");
    Assert.That(Directory.Exists(path), Is.True);
    return path;
  }
}