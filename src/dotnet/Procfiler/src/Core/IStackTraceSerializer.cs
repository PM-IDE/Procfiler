using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core;

public interface IStackTraceSerializer
{
  ValueTask SerializeStackTracesAsync(SessionGlobalData globalData, string directory);
}

[AppComponent]
public class StackTraceSerializer : IStackTraceSerializer
{
  public async ValueTask SerializeStackTracesAsync(SessionGlobalData globalData, string directory)
  {
    var encoding = Encoding.UTF8;
    var sb = new StringBuilder();
    
    foreach (var (managedThreadId, shadowStack) in globalData.Stacks)
    {
      await using var fs = File.OpenWrite(Path.Combine(directory, $"stacks_{managedThreadId}.txt"));
      await fs.WriteAsync(encoding.GetBytes($"{managedThreadId}\n"));

      sb = sb.Clear();
      var indent = 0;
      
      foreach (var frame in shadowStack)
      {
        if (!frame.IsStart)
        {
          --indent;
        }
        
        for (var i = 0; i < indent; ++i)
        {
          sb.AppendSpace();
        }
        
        sb.Append(frame.Serialize(globalData)).AppendNewLine();

        if (frame.IsStart)
        {
          ++indent;
        }
      }

      sb.AppendNewLine();
      await fs.WriteAsync(encoding.GetBytes(sb.ToString()));
    }
  }
}