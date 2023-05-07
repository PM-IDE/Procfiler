using System.Text;
using Procfiler.Core.Collector;
using Procfiler.Utils;
using ProcfilerTests.Core;
using TestsUtil;

namespace ProcfilerTests.Tests.SplitByMethods;

[TestFixture]
public class ByMethodSplitTests : GoldProcessBasedTest
{
  [TestCaseSource(nameof(Source))]
  public void Test(KnownSolution knownSolution)
  {
    DoTest(knownSolution);
  }

  private void DoTest(KnownSolution knownSolution)
  {
    ExecuteTestWithGold(knownSolution, events => DumpMethodCallTree(knownSolution.NamespaceFilterPattern, events));
  }
  
  private string DumpMethodCallTree(string filterPattern, CollectedEvents events)
  {
    var eventByMethods = SplitByMethodsTestUtil.SplitByMethods(events, Container, filterPattern);
    var interestingEvents = eventByMethods.OrderBy(pair => pair.Key);

    var sb = new StringBuilder();
    foreach (var (methodName, tracesOfEvents) in interestingEvents)
    {
      sb.Append("Method: ").Append(methodName).AppendNewLine().AppendNewLine();
      for (var i = 0; i < tracesOfEvents.Count; i++)
      {
        var trace = tracesOfEvents[i];
        sb.Append("Trace ").Append(i).AppendNewLine();
        sb.Append(ProgramMethodCallTreeDumper.CreateDump(trace, filterPattern));
      }

      sb.AppendNewLine();
    }

    return sb.ToString();
  }
}