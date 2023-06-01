using Procfiler.Utils;

namespace Procfiler.Core.CppProcfiler.ShadowStacks;

public class CppShadowStacksImpl : ICppShadowStacks
{
  private readonly object mySync = new();
  private readonly IProcfilerLogger myLogger;
  private readonly string myPathToBinaryStacksFile;
  private readonly IDictionary<long, long> myManagedThreadsToOffsets;
  
  private bool myIsInitialized;
  
  
  public CppShadowStacksImpl(IProcfilerLogger logger, string pathToBinaryStacksFile)
  {
    myLogger = logger;
    myPathToBinaryStacksFile = pathToBinaryStacksFile;
    myManagedThreadsToOffsets = new Dictionary<long, long>();
  }

  
  public IEnumerable<ICppShadowStack> EnumerateStacks()
  {
    using var fs = PathUtils.OpenReadWithRetryOrThrow(myLogger, myPathToBinaryStacksFile);
    using var br = new BinaryReader(fs);

    foreach (var (_, position) in EnumerateShadowStacksInternal(br))
    {
      yield return new CppShadowStackImpl(myLogger, myPathToBinaryStacksFile, position);
    }
  }

  private static IEnumerable<(long ManagedThreadId, long Position)> EnumerateShadowStacksInternal(BinaryReader br)
  {
    var fs = br.BaseStream;
    var position = 0L;
    while (position < fs.Length)
    {
      fs.Seek(position, SeekOrigin.Begin);
      CppShadowStackHelpers.ReadManagedThreadIdAndFramesCount(br, out var threadId, out var framesCount);
      yield return (threadId, position);

      position += CppShadowStackHelpers.CalculateByteLength(framesCount);
    }
  }

  public ICppShadowStack? FindShadowStack(long managedThreadId)
  {
    InitializeThreadIdsToOffsetsIfNeeded();
    
    if (!myManagedThreadsToOffsets.TryGetValue(managedThreadId, out var offset))
    {
      myLogger.LogWarning("The shadow stack for {ManagedThreadId}", managedThreadId);
      return null;
    }

    var foundShadowStack = new CppShadowStackImpl(myLogger, myPathToBinaryStacksFile, offset);
    Debug.Assert(foundShadowStack.ManagedThreadId == managedThreadId);

    return foundShadowStack;
  }

  private void InitializeThreadIdsToOffsetsIfNeeded()
  {
    if (myIsInitialized) return;

    lock (mySync)
    {
      if (myIsInitialized) return;

      try
      {
        using var fs = PathUtils.OpenReadWithRetryOrThrow(myLogger, myPathToBinaryStacksFile);
        using var br = new BinaryReader(fs);

        foreach (var shadowStack in EnumerateShadowStacksInternal(br))
        {
          myManagedThreadsToOffsets[shadowStack.ManagedThreadId] = shadowStack.Position;
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