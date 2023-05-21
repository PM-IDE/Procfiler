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
    if (globalData?.MethodIdToFqn.GetValueOrDefault(FunctionId) is { } fqn)
    {
      var startOrEnd = IsStart ? "start" : " end ";
      return $"[{TimeStamp}] [{startOrEnd}] {fqn}";
    }

    return $"[{TimeStamp}] {FunctionId}";
  }
}

public interface IBinaryShadowStacksReader
{
  Task<IReadOnlyDictionary<long, IReadOnlyList<FrameInfo>>> ReadStackEventsAsync(string path);
}

[AppComponent]
public class BinaryShadowStacksReaderImpl : IBinaryShadowStacksReader
{
  private readonly IProcfilerLogger myLogger;

  
  public BinaryShadowStacksReaderImpl(IProcfilerLogger logger)
  {
    myLogger = logger;
  }

  
  public async Task<IReadOnlyDictionary<long, IReadOnlyList<FrameInfo>>> ReadStackEventsAsync(string path)
  {
    await using var fs = await PathUtils.OpenReadWithRetryOrThrowAsync(myLogger, path);

    if (fs.Length == 0) return new Dictionary<long, IReadOnlyList<FrameInfo>>();

    using var br = new BinaryReader(fs);
    var stacks = new Dictionary<long, IReadOnlyList<FrameInfo>>();
    
    while (fs.Position < fs.Length)
    {
      var frames = new List<FrameInfo>();
      var managedThreadId = br.ReadInt64();
      var framesCount = br.ReadUInt64();

      for (ulong i = 0; i < framesCount; ++i)
      {
        frames.Add(new FrameInfo
        {
          IsStart = br.ReadByte() == 1,
          TimeStamp = br.ReadInt64(),
          FunctionId = br.ReadInt64()
        });
      }

      stacks[managedThreadId] = frames;
    }

    return stacks;
  }
}
