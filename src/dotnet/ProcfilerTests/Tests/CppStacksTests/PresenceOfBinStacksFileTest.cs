using ProcfilerTests.Core;
using TestsUtil;

namespace ProcfilerTests.Tests.CppStacksTests;

[TestFixture]
public class PresenceOfBinStacksFileTest : CppBinStacksTestBase
{
  protected override bool UseMethodsFilter => false;


  [TestCaseSource(nameof(Source))]
  public void TestPresenceOfBinStacks(KnownSolution solution) => DoTestWithPath(solution, binStacksPath =>
  {
    Assert.Multiple(() =>
    {
      Assert.That(Path.Exists(binStacksPath), Is.True);
      Assert.That(new FileInfo(binStacksPath).Length > 0);
    });
  });
}