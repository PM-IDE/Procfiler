namespace Procfiler.Utils;

public readonly struct TempFileCookie : IDisposable
{
  private readonly IProcfilerLogger myLogger;
  
  public string FullFilePath { get; }


  public TempFileCookie(IProcfilerLogger logger)
  {
    myLogger = logger;
    FullFilePath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());
  }

  public TempFileCookie(string alreadyCreatedTempFileFilePath, IProcfilerLogger logger)
  {
    myLogger = logger;
    FullFilePath = alreadyCreatedTempFileFilePath;
  }

  public void Dispose() => PathUtils.ClearPath(FullFilePath, myLogger);
}