using System.Text.RegularExpressions;
using Autofac;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core.CppProcfiler.ShadowStacks;
using Procfiler.Core.EventRecord;
using ProcfilerTests.Core;
using TestsUtil;

namespace ProcfilerTests.Tests.CppStacksTests;

public class BinStacksWithFilterTest : CppBinStacksTestBase
{
  protected override bool UseMethodsFilter => true;


  [TestCaseSource(nameof(DefaultContextsWithFilter))]
  public void TestBinStacksWithFilter(ContextWithSolution dto) => DoTestWithCollectedEvents(dto.Context, events =>
  {
    var shadowStacks = events.GlobalData.Stacks;
    Assert.That(shadowStacks is ICppShadowStacks);
    var cppShadowStacks = (ICppShadowStacks)shadowStacks;
    var regex = new Regex(dto.Solution.Name);
    var eventsFactory = Container.Resolve<IProcfilerEventsFactory>();

    foreach (var cppShadowStack in cppShadowStacks.EnumerateStacks())
    {
      foreach (var methodEvent in cppShadowStack.EnumerateMethods(eventsFactory, events.GlobalData))
      {
        Assert.That(methodEvent.IsMethodStartOrEndEvent(), Is.True);
        var frameName = methodEvent.TryGetMethodStartEndEventInfo()!.Value.Frame;
        Assert.That(regex.IsMatch(frameName), Is.True);
      }
    }
  });
}