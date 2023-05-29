using Procfiler.Core.Exceptions;

namespace Procfiler.Core.CppProcfiler;


public interface IShadowStack : IEnumerable<FrameInfo>, IDisposable
{
  long ManagedThreadId { get; }
  long FramesCount { get; }
  long BytesLength { get; }
}

public class ShadowStackImpl : IShadowStack
{
  private readonly BinaryReader myReader;
  private readonly long myStartPosition;

  private bool myIsEnumerating;

  public long ManagedThreadId { get; }
  public long FramesCount { get; }


  public ShadowStackImpl(BinaryReader reader, long startPosition)
  {
    myReader = reader;
    myStartPosition = startPosition;
    
    reader.BaseStream.Seek(startPosition, SeekOrigin.Begin);
    
    ManagedThreadId = reader.ReadInt64();
    FramesCount = reader.ReadInt64();
    
    reader.BaseStream.Seek(startPosition, SeekOrigin.Begin);
  }

  
  public long BytesLength 
  {
    get
    {
      //start or end indicator + timestamp + function id
      const long OneFrameLength = 1 + 8 + 8;
      //managed thread id + framesCount
      const long HeaderSize = 8 + 8;

      return FramesCount * OneFrameLength + HeaderSize;
    }
  }

  public IEnumerator<FrameInfo> GetEnumerator()
  {
    if (myIsEnumerating)
    {
      //throw new InvalidStateException("Can not enumerate while another enumeration is in progress");
    }
    
    myIsEnumerating = true;
    try
    {
      myReader.BaseStream.Seek(myStartPosition, SeekOrigin.Begin);
      myReader.ReadInt64();
      myReader.ReadUInt64();

      var frameInfo = new FrameInfo();
      for (long i = 0; i < FramesCount; i++)
      {
        frameInfo.IsStart = myReader.ReadByte() == 1;
        frameInfo.TimeStamp = myReader.ReadInt64();
        frameInfo.FunctionId = myReader.ReadInt64();

        yield return frameInfo;
      }
    }
    finally
    {
      myIsEnumerating = false;
    }
  }

  IEnumerator IEnumerable.GetEnumerator()
  {
    return GetEnumerator();
  }

  public void Dispose()
  {
    myReader.Dispose();
  }
}