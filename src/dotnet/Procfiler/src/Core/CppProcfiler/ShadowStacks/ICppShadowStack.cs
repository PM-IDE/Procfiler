namespace Procfiler.Core.CppProcfiler;

public interface ICppShadowStack : IEnumerable<FrameInfo>
{
  long ManagedThreadId { get; }
  long FramesCount { get; }
}