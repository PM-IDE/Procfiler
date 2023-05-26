using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core;

public interface IStackTraceSerializer
{
  ValueTask SerializeStackTracesAsync(SessionGlobalData globalData, string directory);
  ValueTask SerializeStackAsync(IShadowStack stack, SessionGlobalData globalData, string savePath);
}

[AppComponent]
public class StackTraceSerializer : IStackTraceSerializer
{
  public async ValueTask SerializeStackTracesAsync(SessionGlobalData globalData, string directory)
  {
    foreach (var shadowStack in globalData.Stacks.EnumerateStacks())
    {
      var path = Path.Combine(directory, $"stacks_{shadowStack.ManagedThreadId}.txt");
      await SerializeStackAsync(shadowStack, globalData, path);
    }
  }

  public async ValueTask SerializeStackAsync(
    IShadowStack shadowStack, SessionGlobalData globalData, string savePath)
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