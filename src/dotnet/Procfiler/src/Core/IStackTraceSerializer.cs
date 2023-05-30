using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Utils.Container;

namespace Procfiler.Core;

public interface IStackTraceSerializer
{
  void SerializeStackTraces(SessionGlobalData globalData, string directory);
  void SerializeStack(IShadowStack stack, SessionGlobalData globalData, string savePath);
}

[AppComponent]
public class StackTraceSerializer : IStackTraceSerializer
{
  public void SerializeStackTraces(SessionGlobalData globalData, string directory)
  {
    foreach (var shadowStack in globalData.Stacks.EnumerateStacks())
    {
      var path = Path.Combine(directory, $"stacks_{shadowStack.ManagedThreadId}.txt");
      SerializeStack(shadowStack, globalData, path);
    }
  }

  public void SerializeStack(
    IShadowStack shadowStack, SessionGlobalData globalData, string savePath)
  {
    using var fs = File.OpenWrite(savePath);
    using var sw = new StreamWriter(fs);
    
    var indent = 0;
      
    foreach (var frame in shadowStack)
    {
      if (!frame.IsStart)
      {
        --indent;
      }
        
      for (var i = 0; i < indent; ++i)
      {
        sw.Write(' ');
      }

      sw.Write(frame.Serialize(globalData));
      sw.Write('\n');

      if (frame.IsStart)
      {
        ++indent;
      }
    }
  }
}