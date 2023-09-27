using Autofac;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.Processes;
using Procfiler.Core.Processes.Build;
using TestsUtil;

namespace ProcfilerTests.Core;

public abstract class CppBinStacksTestBase : ProcessTestBase
{
  protected abstract bool UseMethodsFilter { get; }

  protected void DoTestWithPath(KnownSolution solution, Action<string, CppProfilerMode> testAction)
  {
    foreach (var context in solution.CreateContextsWithMethodsFilter())
    {
      var result = Container.Resolve<IDotnetProjectBuilder>().TryBuildDotnetProject(context.ProjectBuildInfo);
      Assert.That(result, Is.Not.Null);

      var locator = Container.Resolve<ICppProcfilerLocator>();
      var binStacksSavePathCreator = Container.Resolve<IBinaryStackSavePathCreator>();

      var dto = DotnetProcessLauncherDto.CreateFrom(
        context.CommonContext, result!.Value, locator, binStacksSavePathCreator);

      var launcher = Container.Resolve<IDotnetProcessLauncher>();
      var process = launcher.TryStartDotnetProcess(dto with { DefaultDiagnosticPortSuspend = false });
      Assert.That(process, Is.Not.Null);
      process!.WaitForExit();

      var binStacksPath = binStacksSavePathCreator.CreateSavePath(result.Value, context.CommonContext.CppProfilerMode);
      testAction(binStacksPath, context.CommonContext.CppProfilerMode);
    }
  }

  protected void DoTestWithCollectedEvents(KnownSolution solution, Action<CollectedEvents> testAction) =>
    StartProcessAndDoTest(CreateContext(solution), testAction);

  private IEnumerable<CollectClrEventsContext> CreateContext(KnownSolution solution) =>
    UseMethodsFilter ? solution.CreateContextsWithMethodsFilter() : solution.CreateContexts();
}