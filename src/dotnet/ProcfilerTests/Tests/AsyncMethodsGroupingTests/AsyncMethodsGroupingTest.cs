using System.Text;
using System.Text.RegularExpressions;
using Autofac;
using JetBrains.Lifetimes;
using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.SplitByMethod;
using Procfiler.Utils;
using ProcfilerTests.Core;
using TestsUtil;

namespace ProcfilerTests.Tests.AsyncMethodsGroupingTests;

[TestFixture]
public class AsyncMethodsGroupingTest : GoldProcessBasedTest
{
  [Test]
  public void TestNotSimpleAsync() => DoSimpleTest(KnownSolution.NotSimpleAsyncAwait);

  [Test]
  public void TestSimpleAsyncAwait() => DoSimpleTest(KnownSolution.SimpleAsyncAwait);

  
  private void DoSimpleTest(KnownSolution solution)
  {
    ExecuteTestWithGold(
      solution, 
      events => ExecuteAsyncGroupingTest(events, solution, DumpsAllocationsWith));
  }

  private static string DumpsAllocationsWith(IReadOnlyList<EventRecordWithMetadata> events)
  {
    var regex = new Regex("[a-zA-Z]+.Class[0-9]");
    var sb = new StringBuilder();
    foreach (var eventRecord in events)
    {
      if (!eventRecord.IsGcSampledObjectAlloc(out var typeName)) continue;
      if (regex.Match(typeName).Length != typeName.Length) continue;
      sb.Append(typeName).AppendNewLine();
    }

    return sb.ToString();
  }
  
  private string ExecuteAsyncGroupingTest(
    CollectedEvents events, 
    KnownSolution knownSolution,
    Func<IReadOnlyList<EventRecordWithMetadata>, string> tracesDumber)
  {
    return Lifetime.Using(lifetime =>
    {
      var processingContext = EventsProcessingContext.DoEverything(events.Events, events.GlobalData);
      Container.Resolve<IUnitedEventsProcessor>().ProcessFullEventLog(processingContext);
      var methods = Container.Resolve<IByMethodsSplitter>().Split(events, lifetime, string.Empty, false, false, true);
      var asyncMethodsPrefix = Container.Resolve<IAsyncMethodsGrouper>().AsyncMethodsPrefix;

      var asyncMethods = methods.Where(pair => pair.Key.StartsWith(asyncMethodsPrefix));
      var sb = new StringBuilder();
      var filter = new Regex(knownSolution.NamespaceFilterPattern);

      foreach (var (methodName, methodsTraces) in asyncMethods)
      {
        if (!filter.IsMatch(methodName)) continue;

        sb.Append(methodName);
        foreach (var trace in methodsTraces.OrderBy(t => t[0].Stamp))
        {
          sb.AppendNewLine().Append("Trace:").AppendNewLine();
          sb.Append(tracesDumber(trace));
        }

        sb.AppendNewLine().AppendNewLine();
      }

      return sb.ToString();
    });
  }
}