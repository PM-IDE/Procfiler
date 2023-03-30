using Procfiler.Utils.Container;

namespace Procfiler.Core.Collector;

public enum ProvidersCategoryKind
{
  All,
  Gc,
  GcAllocHigh,
  GcAllocLow,
}

public interface IEventPipeProvidersProvider
{
  IReadOnlyList<EventPipeProvider> GetProvidersFor(ProvidersCategoryKind category);
}

[AppComponent]
public class EventPipeProvidersProviderImpl : IEventPipeProvidersProvider
{
  private const string FrameworkEventSource = "System.Diagnostics.Eventing.FrameworkEventSource";
  private const string NetHttp = "System.Net.Http";
  private const string NetSockets = "System.Net.Sockets";
  private const string Runtime = "System.Runtime";
  private const string ArrayPoolSource = "System.Buffers.ArrayPoolEventSource";


  private static readonly IReadOnlyDictionary<ProvidersCategoryKind, EventPipeProvider[]> ourProvidersForCategories =
    new Dictionary<ProvidersCategoryKind, EventPipeProvider[]>
    {
      [ProvidersCategoryKind.All] = new EventPipeProvider[]
      {
        new(ClrTraceEventParser.ProviderName, EventLevel.Verbose, (long)ClrTraceEventParser.Keywords.All),
        new(SampleProfilerTraceEventParser.ProviderName, EventLevel.Verbose),
        new(TplEtwProviderTraceEventParser.ProviderName, EventLevel.Verbose, (long)TplEtwProviderTraceEventParser.Keywords.Default),
        new(ClrPrivateTraceEventParser.ProviderName, EventLevel.Verbose, CreateClrPrivateTraceEventParserKeywords),
        new(FrameworkEventSource, EventLevel.Verbose, CreateFrameworkTraceEventParserKeywords),
        new(NetHttp, EventLevel.Verbose),
        new(NetSockets, EventLevel.Verbose),
        new(Runtime, EventLevel.Verbose),
        new(ArrayPoolSource, EventLevel.Verbose)
      },
      [ProvidersCategoryKind.Gc] = new EventPipeProvider[]
      {
        new(ClrPrivateTraceEventParser.ProviderName, EventLevel.Verbose, CreateGcPrivateKeywords),
        new(ClrTraceEventParser.ProviderName, EventLevel.Verbose, CreateGcKeywords)
      },
      [ProvidersCategoryKind.GcAllocHigh] = new EventPipeProvider[]
      {
        new(ClrPrivateTraceEventParser.ProviderName, EventLevel.Verbose, CreateGcPrivateKeywords),
        new(ClrTraceEventParser.ProviderName, EventLevel.Verbose, CreateGcAllocHighKeywords)
      },
      [ProvidersCategoryKind.GcAllocLow] = new EventPipeProvider[]
      {
        new(ClrPrivateTraceEventParser.ProviderName, EventLevel.Verbose, CreateGcPrivateKeywords),
        new(ClrTraceEventParser.ProviderName, EventLevel.Verbose, CreateGcAllocLowKeywords)
      }
    };

  private static long CreateClrPrivateTraceEventParserKeywords => (long)
  (
    ClrPrivateTraceEventParser.Keywords.GC |
    ClrPrivateTraceEventParser.Keywords.Binding |
    ClrPrivateTraceEventParser.Keywords.NGenForceRestore |
    ClrPrivateTraceEventParser.Keywords.Fusion |
    ClrPrivateTraceEventParser.Keywords.LoaderHeap |
    ClrPrivateTraceEventParser.Keywords.Security |
    ClrPrivateTraceEventParser.Keywords.Threading |
    ClrPrivateTraceEventParser.Keywords.MulticoreJit |
    ClrPrivateTraceEventParser.Keywords.PerfTrack |
    ClrPrivateTraceEventParser.Keywords.Stack |
    ClrPrivateTraceEventParser.Keywords.Startup
  );

  private static long CreateFrameworkTraceEventParserKeywords => (long)
  (
    FrameworkEventSourceTraceEventParser.Keywords.Loader |
    FrameworkEventSourceTraceEventParser.Keywords.NetClient |
    FrameworkEventSourceTraceEventParser.Keywords.ThreadPool |
    FrameworkEventSourceTraceEventParser.Keywords.ThreadTransfer |
    FrameworkEventSourceTraceEventParser.Keywords.DynamicTypeUsage
  );

  private static long CreateGcKeywords => (long)ClrTraceEventParser.Keywords.GCHeapSnapshot;

  private static long CreateGcAllocHighKeywords => (long)
  (
    ClrTraceEventParser.Keywords.GCHeapSnapshot |
    ClrTraceEventParser.Keywords.GCSampledObjectAllocationHigh
  );

  private static long CreateGcAllocLowKeywords => (long)
  (
    ClrTraceEventParser.Keywords.GCHeapSnapshot |
    ClrTraceEventParser.Keywords.GCSampledObjectAllocationLow
  );

  private static long CreateGcPrivateKeywords => (long)ClrPrivateTraceEventParser.Keywords.GC;
  

  public IReadOnlyList<EventPipeProvider> GetProvidersFor(ProvidersCategoryKind category) =>
    ourProvidersForCategories[category];
}