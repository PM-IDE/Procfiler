using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core;

public interface IStackTraceSerializer
{
  ValueTask SerializeStackTracesAsync(SessionGlobalData globalData, Stream stream);
}

[AppComponent]
public class StackTraceSerializer : IStackTraceSerializer
{
  public async ValueTask SerializeStackTracesAsync(SessionGlobalData globalData, Stream stream)
  {
    var encoding = Encoding.UTF8;
    var sb = new StringBuilder();
    
    foreach (var (managedThreadId, shadowStack) in globalData.Stacks)
    {
      await stream.WriteAsync(encoding.GetBytes($"{managedThreadId}\n"));

      sb = sb.Clear();
      foreach (var frame in shadowStack)
      {
        sb.AppendTab().Append(frame.Serialize(globalData)).AppendNewLine();
      }

      sb.AppendNewLine();
      await stream.WriteAsync(encoding.GetBytes(sb.ToString()));
    }
  }
}