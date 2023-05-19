using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Utils.Container;

namespace Procfiler.Core.CppProcfiler;

public interface IBinaryStackSavePathCreator
{
  string CreateSavePath(ProjectBuildInfo projectBuildInfo);
}

[AppComponent]
public class BinaryStackSavePathCreatorImpl : IBinaryStackSavePathCreator
{
  private const string BinaryStacksFileName = "bstacks.bin";
  
  public string CreateSavePath(ProjectBuildInfo projectBuildInfo)
  {
    Debug.Assert(Directory.Exists(projectBuildInfo.TempPath));

    return Path.Combine(projectBuildInfo.TempPath, BinaryStacksFileName);
  }
}