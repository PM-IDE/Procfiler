using Procfiler.Core.Collector;
using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core;

public interface IStackTraceSerializer
{
  ValueTask SerializeStackTracesAsync(SessionGlobalData globalData, string directory);
  ValueTask SerializeStackAsync(IReadOnlyList<FrameInfo> stack, SessionGlobalData globalData, string savePath);
}

[AppComponent]
public class StackTraceSerializer : IStackTraceSerializer
{
  public async ValueTask SerializeStackTracesAsync(SessionGlobalData globalData, string directory)
  {
    foreach (var (managedThreadId, shadowStack) in globalData.Stacks)
    {
      var path = Path.Combine(directory, $"stacks_{managedThreadId}.txt");
      await SerializeStackAsync(shadowStack, globalData, path);
    }
  }

  public async ValueTask SerializeStackAsync(
    IReadOnlyList<FrameInfo> shadowStack, SessionGlobalData globalData, string savePath)
  {
    var encoding = Encoding.UTF8;
    var sb = new StringBuilder();

    await using var fs = File.OpenWrite(savePath);

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