namespace Procfiler.Utils;

public readonly struct TempFolderCookie : IDisposable
{
  private readonly IProcfilerLogger myLogger;
  
  public string FolderPath { get; }
  
  
  public TempFolderCookie(IProcfilerLogger logger)
  {
    myLogger = logger;
    FolderPath = Directory.CreateTempSubdirectory().FullName;
  }
  
  
  public void Dispose() => PathUtils.DeleteDirectory(FolderPath, myLogger);
}