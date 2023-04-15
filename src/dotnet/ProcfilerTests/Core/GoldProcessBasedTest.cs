using System.Text;
using Procfiler.Core.Collector;
using Procfiler.Utils;

namespace ProcfilerTests.Core;

public class GoldProcessBasedTest : ProcessTestBase
{
  protected void ExecuteTestWithGold(KnownSolution solution, Func<CollectedEvents, string> testFunc)
  {
    StartProcessAndDoTest(solution, async events =>
    {
      var testValue = testFunc(events).RemoveRn();
      var pathToGoldFile = CreateGoldFilePath();
      if (!File.Exists(pathToGoldFile))
      {
        await using var fs = File.CreateText(CreateTmpFilePath());
        await fs.WriteAsync(testValue);
        Assert.Fail($"There was not gold file at {pathToGoldFile}");
        return;
      }

      var goldValue = (await File.ReadAllTextAsync(pathToGoldFile)).RemoveRn();
      if (goldValue != testValue)
      {
        var sb = new StringBuilder();
        sb.Append("The gold and test value were different:").AppendNewLine()
          .Append("Test value:").AppendNewLine()
          .Append(testValue).AppendNewLine()
          .Append("Gold value:").AppendNewLine()
          .Append(goldValue).AppendNewLine();

        await using var fs = File.CreateText(CreateTmpFilePath());
        await fs.WriteAsync(testValue);
        
        Assert.Fail(sb.ToString());
      }
    });
  }

  private string CreateGoldFilePath() => CreatePathInternal($"{CreateTestNameForFiles()}.gold");

  private static string CreateTestNameForFiles()
  {
    var test = TestContext.CurrentContext.Test;
    var testName = test.Name;
    
    if (test.Arguments.FirstOrDefault() is KnownSolution knownSolution)
    {
      testName = knownSolution.Name;
    }

    return testName;
  }

  private string CreatePathInternal(string fileName)
  {
    var folderName = GetType().Name;
    var osPrefix = GetOsFolderOrThrow();
    var directory = Path.Combine(TestPaths.CreatePathToTestData(), "gold", osPrefix, folderName);
    if (!Directory.Exists(directory))
    {
      Directory.CreateDirectory(directory);
    }
    
    return Path.Combine(directory, fileName);
  }

  private static string GetOsFolderOrThrow()
  {
    if (OperatingSystem.IsWindows()) return "windows";
    if (OperatingSystem.IsLinux()) return "linux";
    if (OperatingSystem.IsMacOS()) return "macos";

    throw new ArgumentOutOfRangeException(Environment.OSVersion.Platform.ToString());
  }

  private string CreateTmpFilePath() => CreatePathInternal($"{CreateTestNameForFiles()}.tmp");
}