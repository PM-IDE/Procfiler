using Procfiler.Utils;

namespace Procfiler.Core.CppProcfiler;


public interface IShadowStack : IEnumerable<FrameInfo>
{
  long ManagedThreadId { get; }
  long FramesCount { get; }
  long BytesLength { get; }
}

public class ShadowStackImpl : IShadowStack
{
  private readonly IProcfilerLogger myLogger;
  private readonly string myBinStackFilePath;
  private readonly long myStartPosition;
  

  public long ManagedThreadId { get; }
  public long FramesCount { get; }


  public ShadowStackImpl(IProcfilerLogger logger, string filePath, long startPosition)
  {
    myLogger = logger;
    myBinStackFilePath = filePath;
    myStartPosition = startPosition;

    using var fs = PathUtils.OpenReadWithRetryOrThrow(myLogger, myBinStackFilePath);
    using var reader = new BinaryReader(fs);
    reader.BaseStream.Seek(startPosition, SeekOrigin.Begin);
    
    ShadowStackHelpers.ReadManagedThreadIdAndFramesCount(reader, out var threadId, out var framesCount);
    ManagedThreadId = threadId;
    FramesCount = framesCount;
  }


  public long BytesLength => ShadowStackHelpers.CalculateByteLength(FramesCount);

  public IEnumerator<FrameInfo> GetEnumerator()
  {
    using var fs = PathUtils.OpenReadWithRetryOrThrow(myLogger, myBinStackFilePath);
    using var reader = new BinaryReader(fs);
    
    ShadowStackHelpers.SeekToPositionAndSkipHeader(reader, myStartPosition);
    
    var frameInfo = new FrameInfo();
    for (long i = 0; i < FramesCount; i++)
    {
      frameInfo.IsStart = reader.ReadByte() == 1;
      frameInfo.TimeStamp = reader.ReadInt64();
      frameInfo.FunctionId = reader.ReadInt64();

      yield return frameInfo;
    }
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }
}

public static class ShadowStackHelpers
{
  public static void ReadManagedThreadIdAndFramesCount(BinaryReader reader, out long managedThreadId, out long framesCount)
  {
    managedThreadId = reader.ReadInt64();
    framesCount = reader.ReadInt64();
  }

  public static long CalculateByteLength(long framesCount)
  {
    //start or end indicator + timestamp + function id
    const long OneFrameLength = 1 + 8 + 8;
    //managed thread id + framesCount
    const long HeaderSize = 8 + 8;

    return framesCount * OneFrameLength + HeaderSize;
  }

  public static void SeekToPositionAndSkipHeader(BinaryReader reader, long position)
  {
    reader.BaseStream.Seek(position, SeekOrigin.Begin);
    reader.ReadInt64();
    reader.ReadUInt64();
  }
}