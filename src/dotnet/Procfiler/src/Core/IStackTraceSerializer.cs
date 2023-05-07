using Procfiler.Core.EventRecord;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core;

public interface IStackTraceSerializer
{
  ValueTask SerializeStackTracesAsync(IEnumerable<StackTraceInfo> stacks, Stream stream);
}

[AppComponent]
public class StackTraceSerializer : IStackTraceSerializer
{
  public async ValueTask SerializeStackTracesAsync(IEnumerable<StackTraceInfo> stacks, Stream stream)
  {
    var encoding = Encoding.UTF8;
    var sb = new StringBuilder();
    
    foreach (var stack in stacks)
    {
      await stream.WriteAsync(encoding.GetBytes($"{stack.StackTraceId}\n"));

      sb.Clear();
      foreach (var frame in stack.Frames)
      {
        sb.AppendTab().Append(frame).AppendNewLine();
      }

      sb.AppendNewLine();
      await stream.WriteAsync(encoding.GetBytes(sb.ToString()));
    }
  }
}