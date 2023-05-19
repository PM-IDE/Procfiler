using Procfiler.Utils.Container;

namespace Procfiler.Core.CppProcfiler;

public interface ICppProcfilerLocator
{
  string FindCppProcfilerPath();
}

[AppComponent]
public class CppProcfilerLocatorImpl : ICppProcfilerLocator
{
  public string FindCppProcfilerPath()
  {
    return "/Users/aero/Programming/pmide/Procfiler/src/cpp/cmake-build-release/libProcfiler.dylib";
  }
}