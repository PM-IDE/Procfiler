using Procfiler.Core.CppProcfiler;
using ProcfilerTests.Core;
using TestsUtil;

namespace ProcfilerTests.Tests.CppStacksTests;

[TestFixture]
public class PresenceOfBinStacksFileTest : CppBinStacksTestBase
{
  protected override bool UseMethodsFilter => false;


  [TestCaseSource(nameof(Source))]
  public void TestPresenceOfBinStacks(KnownSolution solution) => DoTestWithPath(solution, (binStacksPath, mode) =>
  {
    Assert.Multiple(() =>
    {
      switch (mode.ToFileMode())
      {
        case CppProfilerBinStacksFileMode.SingleFile:
          Assert.That(Path.Exists(binStacksPath), Is.True);
          Assert.That(new FileInfo(binStacksPath).Length > 0);
          break;
        case CppProfilerBinStacksFileMode.PerThreadFiles:
          var binStacksFiles = Directory.EnumerateFiles(binStacksPath).Where(file => file.Contains("binstack_")).ToList();
          Assert.That(binStacksFiles.Count, Is.GreaterThan(0));
          
          foreach (var binStacksFile in binStacksFiles)
          {
            Assert.That(new FileInfo(binStacksFile).Length > 0);
          }
          
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    });
  });
}