using Autofac;
using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsProcessing;
using Procfiler.Core.SplitByMethod;
using Procfiler.Utils;
using ProcfilerTests.Core;

namespace ProcfilerTests.Tests.SplitByMethods;

public static class SplitByMethodsTestUtil
{
  public static IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyList<EventRecordWithMetadata>>> SplitByMethods(
    CollectedEvents events, IContainer container, string filterPattern)
  {
    var mainThreadEvents = TestUtil.FindEventsForMainThread(events.Events);
    var processingContext = EventsProcessingContext.DoEverything(mainThreadEvents, events.GlobalData);
    
    var processor = container.Resolve<IUnitedEventsProcessor>();
    processor.ProcessFullEventLog(processingContext);
    processor.ApplyMultipleMutators(mainThreadEvents, events.GlobalData, EmptyCollections<Type>.EmptySet);
    
    return container.Resolve<IEventsCollectionByMethodsSplitter>().Split(mainThreadEvents, filterPattern, true);
  }
}