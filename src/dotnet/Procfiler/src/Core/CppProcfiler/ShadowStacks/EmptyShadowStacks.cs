using Procfiler.Utils;

namespace Procfiler.Core.CppProcfiler;

public class EmptyShadowStacks : IShadowStacks
{
  public static EmptyShadowStacks Instance { get; } = new();
  

  private EmptyShadowStacks()
  {
  }
  
  
  public IEnumerable<ICppShadowStack> EnumerateStacks() => EmptyCollections<ICppShadowStack>.EmptyList;
  public ICppShadowStack? FindShadowStack(long managedThreadId) => null;
}