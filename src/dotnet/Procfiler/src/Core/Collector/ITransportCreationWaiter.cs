using System.Runtime.InteropServices;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Collector;

public interface ITransportCreationWaiter
{
  void WaitUntilTransportIsCreatedOrThrow(int processId);
}

[AppComponent]
public class TransportCreationWaiterImpl : ITransportCreationWaiter
{
  private readonly IProcfilerLogger myLogger;


  public TransportCreationWaiterImpl(IProcfilerLogger logger)
  {
    myLogger = logger;
  }


  public void WaitUntilTransportIsCreatedOrThrow(int processId)
  {
    var name = $"{GetType().Name}::{nameof(WaitUntilTransportIsCreatedOrThrow)}::{processId}";
    using var _ = new PerformanceCookie(name, myLogger);
    var rootPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"\\.\pipe\" : Path.GetTempPath();
    var transportPath = OperatingSystem.IsWindows() switch
    {
      true => $"dotnet-diagnostic-{processId}",
      false => $"dotnet-diagnostic-{processId}-*-socket"
    };

    const int TimeoutMs = 10_000;
    if (!SpinWait.SpinUntil(() => Directory.GetFiles(rootPath, transportPath).Length != 0, TimeoutMs))
    {
      throw new FileNotFoundException($"The transport file {transportPath} was not created after {TimeoutMs}ms");
    }
  }
}  