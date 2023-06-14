using Autofac;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.Processes;
using Procfiler.Core.Processes.Build;
using ProcfilerTests.Core;
using TestsUtil;

namespace ProcfilerTests.Tests.CppStacksTests;

[TestFixture]
public class PresenceOfBinStacksFileTest : TestWithContainerBase
{
  [TestCaseSource(nameof(Source))]
  public void TestPresenceOfBinStacks(KnownSolution solution) => DoTest(solution);
  
  private void DoTest(KnownSolution solution)
  {
    var context = solution.CreateContextWithMethodsFilter();
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

    var binStacksPath = binStacksSavePathCreator.CreateSavePath(result.Value);
    Assert.That(Path.Exists(binStacksPath));
  }
}