namespace Procfiler.Utils;

public static class PathUtils
{
  public static void ThrowIfNotExists(string path)
  {
    if (!Path.Exists(path))
    {
      throw new FileNotFoundException(path);
    }
  }
  
  public static void CheckIfDirectoryOrThrow(string path)
  {
    if (!File.GetAttributes(path).HasFlag(FileAttributes.Directory))
      throw new DirectoryNotFoundException();
  }

  public static void EnsureEmptyDirectory(string path, IProcfilerLogger logger)
  {
    try
    {
      EnsureEmptyDirectoryOrThrow(path);
    }
    catch (Exception ex)
    {
      logger.LogError(ex.Message);
    }
  }

  public static void EnsureEmptyDirectoryOrThrow(string path)
  {
    if (Directory.Exists(path))
    {
      Directory.Delete(path, true);
    }

    Directory.CreateDirectory(path);
  }
  
  public static void ClearPath(string path, IProcfilerLogger logger)
  {
    try
    {
      if (Path.Exists(path) && (File.GetAttributes(path) & FileAttributes.Directory) != 0)
      {
        EnsureEmptyDirectoryOrThrow(path);
      }
      else
      {
        if (File.Exists(path))
        {
          File.Delete(path);
        }
      }
    }
    catch (Exception ex)
    {
      logger.LogError(ex, ex.Message);
    }
  }

  public static void DeleteDirectory(string path, IProcfilerLogger logger)
  {
    if (!Directory.Exists(path)) return;

    try
    {
      Directory.Delete(path, recursive: true);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Failed to delete directory {Path}", path);
    }
  }
}