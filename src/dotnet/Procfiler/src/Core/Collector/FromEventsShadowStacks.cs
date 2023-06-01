namespace Procfiler.Core.Collector;

public interface IFromEventsShadowStacks : IShadowStacks
{
  IReadOnlyDictionary<int, StackTraceInfo> StackTraceInfos { get; }
  
  
  void AddStack(TraceEvent traceEvent);
}

public class FromEventsShadowStacks : IFromEventsShadowStacks
{
  private readonly StackTracesStorage myStorage;


  public IReadOnlyDictionary<int, StackTraceInfo> StackTraceInfos => myStorage.StackTraces;
  
  
  public FromEventsShadowStacks(MutableTraceEventStackSource source)
  {
    myStorage = new StackTracesStorage(source);
  }


  public void AddStack(TraceEvent traceEvent)
  {
    myStorage.AddStackTraceInfoFrom(traceEvent);
  }
}

public class StackTracesStorage
{
  private readonly MutableTraceEventStackSource mySource;
  private readonly Dictionary<int, int> myStacksHashCodesToIds;
  private readonly Dictionary<int, StackTraceInfo> myIdsToStackTraces;

  
  public IReadOnlyDictionary<int, StackTraceInfo> StackTraces => myIdsToStackTraces;


  public StackTracesStorage(MutableTraceEventStackSource source)
  {
    mySource = source;
    myStacksHashCodesToIds = new Dictionary<int, int>();
    myIdsToStackTraces = new Dictionary<int, StackTraceInfo>();
  }
  
  
  public void AddStackTraceInfoFrom(TraceEvent @event)
  {
    var id = @event.CallStackIndex();
    if (id == CallStackIndex.Invalid) return;

    var intId = (int) id;
    if (myIdsToStackTraces.TryGetValue(intId, out _)) return;

    var info = @event.CreateEventStackTraceInfoOrThrow(mySource);
    var infoHash = info.GetHashCode();
    if (myStacksHashCodesToIds.TryGetValue(infoHash, out _))
    {
      return;
    }

    myStacksHashCodesToIds[infoHash] = intId;
    myIdsToStackTraces[intId] = info;
  }
}