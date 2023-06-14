using Procfiler.Core.Collector;
using Procfiler.Core.EventRecord;

namespace Procfiler.Core.CppProcfiler.ShadowStacks;

public interface ICppShadowStack : IEnumerable<FrameInfo>
{
  long ManagedThreadId { get; }
  long FramesCount { get; }
}

public static class ExtensionsForICppShadowStack
{
  public static IEnumerable<EventRecordWithMetadata> EnumerateMethods(
    this ICppShadowStack shadowStack, IProcfilerEventsFactory eventsFactory, SessionGlobalData globalData)
  {
    foreach (var frameInfo in shadowStack)
    {
      var creationContext = new FromFrameInfoCreationContext
      {
        FrameInfo = frameInfo,
        GlobalData = globalData,
        ManagedThreadId = shadowStack.ManagedThreadId
      };

      var createdMethodEvent = eventsFactory.CreateMethodEvent(creationContext);
      yield return createdMethodEvent;
    }
  }
}