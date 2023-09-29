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
  public static CollectClrEventsFromExeContext CreateContextWithFilter(this KnownSolution solution) => 
    CreateContextInternal(solution, CreateDefaultContextWithFilter(solution));

  public static CollectClrEventsFromExeContext CreateDefaultContext(this KnownSolution solution) => 
    CreateContextInternal(solution, CreateDefaultCommonContext());

  public static CollectClrEventsFromExeContext CreateOnlineSerializationContext(this KnownSolution solution) => 
    CreateContextInternal(solution, CreateOnlineSerializationCommonContext());

  public static CollectClrEventsFromExeContext CreateOnlineSerializationContextWithFilter(this KnownSolution solution) => 
    CreateContextInternal(solution, CreateOnlineSerializationCommonContextWithFilter(solution));
  
  private static CollectClrEventsFromExeContext CreateContextInternal(
    KnownSolution knownSolution, CollectingClrEventsCommonContext context)
  {
    var solutionsDir = TestPaths.CreatePathToSolutionsSource();
    var csprojPath = Path.Combine(solutionsDir, knownSolution.Name, knownSolution.Name + ".csproj");
    var projectBuildInfo = new ProjectBuildInfo(
      csprojPath, knownSolution.Tfm, BuildConfiguration.Debug, InstrumentationKind.None,
      true, PathUtils.CreateTempFolderPath(), false, null);

    return new CollectClrEventsFromExeContext(projectBuildInfo, context);
  }

  private static CollectingClrEventsCommonContext CreateDefaultCommonContext()
  {
    var serializationContext = new SerializationContext(FileFormat.Csv);
    return new CollectingClrEventsCommonContext(
      string.Empty, serializationContext, new TestParseResultsProvider(), string.Empty, ProvidersCategoryKind.All,
      false, 10_000, 10_000, false, null, 10_000, CppProfilerMode.SingleFileBinStack, false, false, true);
  }

  private static CollectingClrEventsCommonContext CreateOnlineSerializationCommonContext()
  {
    return CreateDefaultCommonContext() with
    {
      CppProfilerMode = CppProfilerMode.PerThreadBinStacksFilesOnline,
      UseDuringRuntimeFiltering = true
    };
  }
  
  private static CollectingClrEventsCommonContext CreateDefaultContextWithFilter(KnownSolution solution)
  {
    return CreateDefaultCommonContext() with
    {
      CppProcfilerMethodsFilterRegex = solution.Name
    };
  }

  private static CollectingClrEventsCommonContext CreateOnlineSerializationCommonContextWithFilter(KnownSolution solution)
  {
    return CreateOnlineSerializationCommonContext() with
    {
      CppProcfilerMethodsFilterRegex = solution.Name
    };
  }
}

internal class TestParseResultsProvider : IParseResultInfoProvider
{
  public T? TryGetOptionValue<T>(Option<T> option)
  {
    return default;
  }
}