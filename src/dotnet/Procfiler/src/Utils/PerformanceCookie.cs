namespace Procfiler.Utils;

public readonly struct PerformanceCookie : IDisposable
{
  private readonly string myActivityName;
  private readonly IProcfilerLogger myLogger;
  private readonly Stopwatch myStopwatch;
  private readonly ProcfilerLoggerIndentCookie myProcfilerLoggerIndentCookie;


  public PerformanceCookie(string activityName, IProcfilerLogger logger)
  {
    myActivityName = activityName;
    myLogger = logger;
    myStopwatch = Stopwatch.StartNew();
    myLogger.LogInformation("Started activity {Name}", activityName);
    myProcfilerLoggerIndentCookie = myLogger.CreateIndentCookie();
  }


  public void Dispose()
  {
    myStopwatch.Stop();
    myProcfilerLoggerIndentCookie.Dispose();
    var elapsed = myStopwatch.Elapsed.TotalMilliseconds;
    myLogger.LogInformation("Activity {Name} finished in {Time}ms", myActivityName, elapsed);
  }
}