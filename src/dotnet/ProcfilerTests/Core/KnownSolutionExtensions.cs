using System.CommandLine;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.InstrumentalProfiler;
using Procfiler.Utils;
using TestsUtil;

namespace ProcfilerTests.Core;

public static class KnownSolutionExtensions
{
  public static IEnumerable<CollectClrEventsFromExeContext> CreateContextsWithMethodsFilter(this KnownSolution solution) =>
    solution.CreateContexts().Select(context => context.CommonContext.CppProfilerMode switch
    {
      CppProfilerMode.SingleFileBinStack or CppProfilerMode.PerThreadBinStacksFiles => context with
      {
        CommonContext = context.CommonContext with
        {
          CppProcfilerMethodsFilterRegex = solution.Name
        }
      },
      CppProfilerMode.PerThreadBinStacksFilesOnline => context with
      {
        CommonContext = context.CommonContext with
        {
          CppProcfilerMethodsFilterRegex = solution.Name,
          UseDuringRuntimeFiltering = true
        }
      },
      _ => throw new ArgumentOutOfRangeException()
    });

  public static IEnumerable<CollectClrEventsFromExeContext> CreateContexts(this KnownSolution knownSolution)
  {
    var solutionsDir = TestPaths.CreatePathToSolutionsSource();
    var csprojPath = Path.Combine(solutionsDir, knownSolution.Name, knownSolution.Name + ".csproj");
    
    foreach (var context in CreateCommonContexts())
    {
      var projectBuildInfo = new ProjectBuildInfo(
        csprojPath, knownSolution.Tfm, BuildConfiguration.Debug, InstrumentationKind.None,
        true, PathUtils.CreateTempFolderPath(), false, null);

      yield return new CollectClrEventsFromExeContext(projectBuildInfo, context); 
    }
  }

  private static IEnumerable<CollectingClrEventsCommonContext> CreateCommonContexts()
  {
    var serializationContext = new SerializationContext(FileFormat.Csv);
    var ctx = new CollectingClrEventsCommonContext(
      string.Empty, serializationContext, new TestParseResultsProvider(), string.Empty, ProvidersCategoryKind.All,
      false, 10_000, 10_000, false, null, 10_000, CppProfilerMode.SingleFileBinStack, false, false, true);

    yield return ctx;
    //yield return ctx with { CppProfilerMode = CppProfilerMode.PerThreadBinStacksFilesOnline };
  }
}

internal class TestParseResultsProvider : IParseResultInfoProvider
{
  public T? TryGetOptionValue<T>(Option<T> option)
  {
    return default;
  }
}