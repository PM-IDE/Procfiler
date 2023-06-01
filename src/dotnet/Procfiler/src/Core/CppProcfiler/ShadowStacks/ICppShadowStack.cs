namespace Procfiler.Core.CppProcfiler.ShadowStacks;

public interface ICppShadowStack : IEnumerable<FrameInfo>
{
  long ManagedThreadId { get; }
  long FramesCount { get; }
}