using Procfiler.Core.Collector;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.CppProcfiler;

public readonly struct FrameInfo
{
  public required long TimeStamp { get; init; }
  public required long FunctionId { get; init; }
  public required bool IsStart { get; init; }


  public string Serialize(SessionGlobalData? globalData)
  {
    var fqnOrId = globalData?.MethodIdToFqn.GetValueOrDefault(FunctionId) switch
    {
      { } fqn => fqn,
      _ => FunctionId.ToString()
    };
    
    var startOrEnd = IsStart ? "start" : " end ";
    return $"[{TimeStamp}] [{startOrEnd}] {fqnOrId}";
  }
}

public interface IBinaryShadowStacksReader
{
  IShadowStacks ReadStackEvents(string path);
}

[AppComponent]
public class BinaryShadowStacksReaderImpl : IBinaryShadowStacksReader
{
  private readonly IProcfilerLogger myLogger;

  
  public BinaryShadowStacksReaderImpl(IProcfilerLogger logger)
  {
    myLogger = logger;
  }

  
  public IShadowStacks ReadStackEvents(string path) => new ShadowStacksImpl(myLogger, path);
}

