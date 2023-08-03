namespace Procfiler.Utils;

public readonly struct TempFileCookie(string alreadyCreatedTempFileFilePath, IProcfilerLogger logger) : IDisposable
{
  public string FullFilePath { get; } = alreadyCreatedTempFileFilePath;


  public TempFileCookie(IProcfilerLogger logger) : this(PathUtils.CreateTempFilePath(), logger)
  {
  }

  public void Dispose() => PathUtils.ClearPath(FullFilePath, logger);
}