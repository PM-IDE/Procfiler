using Procfiler.Core.Exceptions;
using Procfiler.Utils;

namespace Procfiler.Core.CppProcfiler;

public interface IShadowStacks
{
  IEnumerable<IShadowStack> EnumerateStacks(bool disposeStack);
  IShadowStack? FindShadowStack(long managedThreadId);
}

public class EmptyShadowStacks : IShadowStacks
{
  public static EmptyShadowStacks Instance { get; } = new();
  

  private EmptyShadowStacks()
  {
  }
  
  
  public IEnumerable<IShadowStack> EnumerateStacks(bool disposeStack) => EmptyCollections<IShadowStack>.EmptyList;
  public IShadowStack? FindShadowStack(long managedThreadId) => null;
}

public class ShadowStacksImpl : IShadowStacks
{
  private readonly object mySync = new();
  private readonly IProcfilerLogger myLogger;
  private readonly string myPathToBinaryStacksFile;
  private readonly IDictionary<long, long> myManagedThreadsToOffsets;
  
  private bool myIsInitialized;
  
  
  public ShadowStacksImpl(IProcfilerLogger logger, string pathToBinaryStacksFile)
  {
    myLogger = logger;
    myPathToBinaryStacksFile = pathToBinaryStacksFile;
    myManagedThreadsToOffsets = new Dictionary<long, long>();
  }

  
  public IEnumerable<IShadowStack> EnumerateStacks(bool disposeStack)
  {
    var fs = PathUtils.OpenReadWithRetryOrThrow(myLogger, myPathToBinaryStacksFile);
    var br = new BinaryReader(fs);

    foreach (var shadowStack in EnumerateShadowStacksInternal(br))
    {
      try
      {
        yield return shadowStack;
      }
      finally
      {
        if (disposeStack)
        {
          shadowStack.Dispose(); 
        }
      }
    }
  }

  private static IEnumerable<IShadowStack> EnumerateShadowStacksInternal(BinaryReader br)
  {
    var fs = br.BaseStream;
    while (fs.Position < fs.Length)
    {
      var shadowStack = new ShadowStackImpl(br, fs.Position);
      yield return shadowStack;

      fs.Seek(shadowStack.BytesLength, SeekOrigin.Current);
    }
  }

  public IShadowStack? FindShadowStack(long managedThreadId)
  {
    InitializeThreadIdsToOffsetsIfNeeded();
    
    if (!myManagedThreadsToOffsets.TryGetValue(managedThreadId, out var offset))
    {
      myLogger.LogWarning("The shadow stack for {ManagedThreadId}", managedThreadId);
      return null;
    }

    var fs = PathUtils.OpenReadWithRetryOrThrow(myLogger, myPathToBinaryStacksFile);
    var binaryReader = new BinaryReader(fs);
    var foundShadowStack = new ShadowStackImpl(binaryReader, offset);
    Debug.Assert(foundShadowStack.ManagedThreadId == managedThreadId);

    return foundShadowStack;
  }

  private void InitializeThreadIdsToOffsetsIfNeeded()
  {
    if (!myIsInitialized)
    {
      lock (mySync)
      {
        if (!myIsInitialized)
        {
          try
          {
            using var fs = PathUtils.OpenReadWithRetryOrThrow(myLogger, myPathToBinaryStacksFile);
            using var br = new BinaryReader(fs);

            foreach (var shadowStack in EnumerateShadowStacksInternal(br))
            {
              myManagedThreadsToOffsets[shadowStack.ManagedThreadId] = fs.Position;
            }

            myIsInitialized = true;
          }
          catch (Exception ex)
          {
            myLogger.LogError(ex, "Failed to initialize shadow-stacks position");
          }
        }
      }
    }
  }
}

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
      throw new InvalidStateException("Can not enumerate while another enumeration is in progress");
    }
    
    myIsEnumerating = true;
    try
    {
      myReader.BaseStream.Seek(myStartPosition, SeekOrigin.Begin);
      myReader.ReadInt64();
      myReader.ReadUInt64();

      for (long i = 0; i < FramesCount; i++)
      {
        yield return new FrameInfo
        {
          IsStart = myReader.ReadByte() == 1,
          TimeStamp = myReader.ReadInt64(),
          FunctionId = myReader.ReadInt64()
        };
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