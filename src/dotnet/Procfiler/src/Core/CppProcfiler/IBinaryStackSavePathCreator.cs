using Procfiler.Core.Processes.Build;
using Procfiler.Utils.Container;

namespace Procfiler.Core.CppProcfiler;

public interface IBinaryStackSavePathCreator
{
  string CreateSavePath(BuildResult buildResult);
}

[AppComponent]
public class BinaryStackSavePathCreatorImpl : IBinaryStackSavePathCreator
{
  private const string BinaryStacksFileName = "bstacks.bin";
  
  public string CreateSavePath(BuildResult buildResult)
  {
    var directory = Path.GetDirectoryName(buildResult.BuiltDllPath);
    Debug.Assert(Directory.Exists(directory));

    return Path.Combine(directory, BinaryStacksFileName);
  }
}