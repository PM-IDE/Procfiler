using Procfiler.Core.Collector;
using Procfiler.Utils;

namespace Procfiler.Core.CppProcfiler.ShadowStacks;

public class EmptyShadowStacks : IShadowStacks
{
  public static EmptyShadowStacks Instance { get; } = new();
  

  private EmptyShadowStacks()
  {
  }
  
  
  public IEnumerable<ICppShadowStack> EnumerateStacks() => EmptyCollections<ICppShadowStack>.EmptyList;
  public ICppShadowStack? FindShadowStack(long managedThreadId) => null;
}