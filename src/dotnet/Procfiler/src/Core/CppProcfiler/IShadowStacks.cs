using Procfiler.Utils;

namespace Procfiler.Core.CppProcfiler;

public interface IShadowStacks
{
  IEnumerable<IShadowStack> EnumerateStacks();
  IShadowStack? FindShadowStack(long managedThreadId);
}

public class EmptyShadowStacks : IShadowStacks
{
  public static EmptyShadowStacks Instance { get; } = new();
  

  private EmptyShadowStacks()
  {
  }
  
  
  public IEnumerable<IShadowStack> EnumerateStacks() => EmptyCollections<IShadowStack>.EmptyList;
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

  
  public IEnumerable<IShadowStack> EnumerateStacks()
  {
    using var fs = PathUtils.OpenReadWithRetryOrThrow(myLogger, myPathToBinaryStacksFile);
    using var br = new BinaryReader(fs);

    foreach (var shadowStack in EnumerateShadowStacksInternal(br))
    {
      yield return shadowStack;
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