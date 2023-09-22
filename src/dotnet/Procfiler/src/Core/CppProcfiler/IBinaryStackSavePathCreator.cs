using Procfiler.Core.Processes.Build;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.CppProcfiler;

public interface IBinaryStackSavePathCreator
{
  string CreateSavePath(BuildResult buildResult, CppProfilerMode mode);
  string CreateTempSavePath(CppProfilerMode mode);
}

[AppComponent]
public class BinaryStackSavePathCreatorImpl : IBinaryStackSavePathCreator
{
  private const string BinaryStacksFileName = "bstacks.bin";

  public string CreateSavePath(BuildResult buildResult, CppProfilerMode mode)
  {
    switch (mode)
    {
      case CppProfilerMode.SingleFileBinStack:
      {
        var directory = Path.GetDirectoryName(buildResult.BuiltDllPath);
        Debug.Assert(Directory.Exists(directory));

        return Path.Combine(directory, BinaryStacksFileName);
      }
      case CppProfilerMode.PerThreadBinStacksFiles:
      {
        return Path.GetDirectoryName(buildResult.BuiltDllPath) ??
               throw new DirectoryNotFoundException(buildResult.BuiltDllPath);
      }
      default:
        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
    }
  }

  public string CreateTempSavePath(CppProfilerMode mode) => mode switch
  {
    CppProfilerMode.SingleFileBinStack => Path.Combine(PathUtils.CreateTempFolderPath(), BinaryStacksFileName),
    CppProfilerMode.PerThreadBinStacksFiles => PathUtils.CreateTempFolderPath(),
    _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
  };
}