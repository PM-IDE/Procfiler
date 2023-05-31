namespace Procfiler.Core.CppProcfiler;

public interface ICppShadowStacks : IShadowStacks
{
  IEnumerable<ICppShadowStack> EnumerateStacks();
  ICppShadowStack? FindShadowStack(long managedThreadId);
}