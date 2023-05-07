using Autofac;
using Procfiler.Core.Collector;
using Procfiler.Core.EventsProcessing;
using Procfiler.Utils;
using ProcfilerTests.Core;
using TestsUtil;

namespace ProcfilerTests.Tests;

[TestFixture]
public class MethodsStartEndTests : GoldProcessBasedTest
{
  [TestCaseSource(nameof(Source))]
  public void Test(KnownSolution knownSolution) => DoTest(knownSolution); 
  
  
  private void DoTest(KnownSolution knownSolution)
  {
    ExecuteTestWithGold(
      knownSolution, events => DumpMethodCallTree(knownSolution.NamespaceFilterPattern, events));
  }

  private string DumpMethodCallTree(string filterPattern, CollectedEvents events)
  {
    var mainThreadEvents = TestUtil.FindEventsForMainThread(events.Events);
    var processingContext = EventsProcessingContext.DoEverything(mainThreadEvents, events.GlobalData);
    
    var processor = Container.Resolve<IUnitedEventsProcessor>();
    processor.ProcessFullEventLog(processingContext);
    processor.ApplyMultipleMutators(mainThreadEvents, events.GlobalData, EmptyCollections<Type>.EmptySet);
    
    return ProgramMethodCallTreeDumper.CreateDump(mainThreadEvents, filterPattern);
  }
}