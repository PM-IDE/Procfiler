using System.CommandLine;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core.Collector;
using Procfiler.Core.InstrumentalProfiler;
using Procfiler.Utils;
using TestsUtil;

namespace ProcfilerTests.Core;

public static class KnownSolutionExtensions
{
  public static CollectClrEventsFromExeContext CreateContext(this KnownSolution knownSolution, string solutionsDir)
  {
    var csprojPath = Path.Combine(solutionsDir, knownSolution.Name, knownSolution.Name + ".csproj");
    var projectBuildInfo = new ProjectBuildInfo(
      csprojPath, knownSolution.Tfm, BuildConfiguration.Debug, InstrumentationKind.None, 
      true, PathUtils.CreateTempFolderPath(), false);
    
    return new CollectClrEventsFromExeContext(projectBuildInfo, CreateCommonContext());
  }

  private static CollectingClrEventsCommonContext CreateCommonContext()
  {
    var serializationContext = new SerializationContext(FileFormat.Csv);
    return new CollectingClrEventsCommonContext(
      string.Empty, serializationContext, new TestParseResultsProvider(), string.Empty, ProvidersCategoryKind.All, false, 10_000, 10_000, false);
  }
}

internal class TestParseResultsProvider : IParseResultInfoProvider
{
  public T? TryGetOptionValue<T>(Option<T> option)
  {
    return default;
  }
}