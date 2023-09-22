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
        var dirName = Path.GetDirectoryName(buildResult.BuiltDllPath) ?? 
                      throw new DirectoryNotFoundException(buildResult.BuiltDllPath);

        return AdjustBinStacksSavePath(dirName);
      }
      default:
        throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
    }
  }

  private static string AdjustBinStacksSavePath(string path)
  {
    if (!Path.EndsInDirectorySeparator(path))
    {
      path += Path.DirectorySeparatorChar;
    }

    return path;
  }

  public string CreateTempSavePath(CppProfilerMode mode) => mode switch
  {
    CppProfilerMode.SingleFileBinStack => Path.Combine(PathUtils.CreateTempFolderPath(), BinaryStacksFileName),
    CppProfilerMode.PerThreadBinStacksFiles => AdjustBinStacksSavePath(PathUtils.CreateTempFolderPath()),
    _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
  };
}