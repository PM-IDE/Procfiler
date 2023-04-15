namespace Procfiler.Utils;

public interface IProcfilerLogger : ILogger
{
  void IncreaseIndent();
  void DecreaseIndent();
}

public class ProcfilerLogger : IProcfilerLogger
{
  private const string Space = "--";
  
  private int myIndent;
  private readonly ILogger myLogger;


  public ProcfilerLogger(ILogger logger)
  {
    myLogger = logger;
  }
  

  public void Log<TState>(
    LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
  {
    string FormatWithIndent(TState localState, Exception? localException)
    {
      return Format(formatter(localState, localException));
    }

    myLogger.Log(logLevel, eventId, state, exception, FormatWithIndent);
  }

  private string Format(string message)
  {
    Debug.Assert(myIndent >= 0);

    if (myIndent == 0) return message;
    
    var sb = new StringBuilder();
    for (var i = 0; i < myIndent; ++i)
    {
      sb.Append(Space);
    }

    return sb.Append(message).ToString();
  }

  public bool IsEnabled(LogLevel logLevel)
  {
    return myLogger.IsEnabled(logLevel);
  }

  public IDisposable? BeginScope<TState>(TState state) where TState : notnull
  {
    return myLogger.BeginScope(state);
  }

  public void IncreaseIndent()
  {
    ++myIndent;
  }

  public void DecreaseIndent()
  {
    Debug.Assert(myIndent > 0);
    --myIndent;
  }
}

public readonly struct ProcfilerLoggerIndentCookie : IDisposable
{
  private readonly IProcfilerLogger myProcfilerLogger;


  public ProcfilerLoggerIndentCookie(IProcfilerLogger procfilerLogger)
  {
    myProcfilerLogger = procfilerLogger;
    procfilerLogger.IncreaseIndent();
  }


  public void Dispose()
  {
    myProcfilerLogger.DecreaseIndent();
  }
}

public static class ExtensionsForIProcfilerLogger
{
  public static ProcfilerLoggerIndentCookie CreateIndentCookie(this IProcfilerLogger logger)
  {
    return new ProcfilerLoggerIndentCookie(logger);
  }
}