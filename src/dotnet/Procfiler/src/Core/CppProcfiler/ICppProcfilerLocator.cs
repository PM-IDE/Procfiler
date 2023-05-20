using Procfiler.Utils.Container;

namespace Procfiler.Core.CppProcfiler;

public interface ICppProcfilerLocator
{
  string FindCppProcfilerPath();
}

[AppComponent]
public class CppProcfilerLocatorImpl : ICppProcfilerLocator
{
  public string FindCppProcfilerPath() => Path.Combine(Directory.GetCurrentDirectory(), "CppProcfiler.dll");
}