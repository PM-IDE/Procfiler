using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.CppProcfiler;

public interface ICppProcfilerLocator
{
  string FindCppProcfilerPath();
}

[AppComponent]
public class CppProcfilerLocatorImpl : ICppProcfilerLocator
{
  private readonly IProcfilerLogger myLogger;

  
  public CppProcfilerLocatorImpl(IProcfilerLogger logger)
  {
    myLogger = logger;
  }

  
  public string FindCppProcfilerPath()
  {
    var procfilerAssemblyLocation = Path.GetDirectoryName(GetType().Assembly.Location);
    if (procfilerAssemblyLocation is null)
    {
      myLogger.LogError("The Procfiler.dll has no path: {Path}", procfilerAssemblyLocation);
      throw new FileNotFoundException();
    }
    
    var path = Path.Combine(procfilerAssemblyLocation, "CppProcfiler.dll");
    if (!File.Exists(path))
    {
      myLogger.LogError("The CppProcfiler.dll does not exist here: {Path}", path);
      throw new FileNotFoundException();
    }
    
    myLogger.LogInformation("The cpp Procfiler is located at {Path}", path);
    return path;
  }
}