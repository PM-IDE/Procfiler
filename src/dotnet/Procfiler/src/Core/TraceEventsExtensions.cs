using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventRecord;

namespace Procfiler.Core;

internal static class TraceEventsExtensions
{
  public static Dictionary<string, string> CreateMetadataMap(this TraceEvent traceEvent)
  {
    var metadata = new Dictionary<string, string>();
    for (var index = 0; index < traceEvent.PayloadNames.Length; index++)
    {
      var name = traceEvent.PayloadNames[index];
      metadata[name] = string.Intern(traceEvent.PayloadString(index));
    }

    return metadata;
  }

  public static bool IsThreadStartMethod(string frame, out int threadId)
  {
    threadId = -1;
    if (!frame.Contains(TraceEventsConstants.ThreadFrameTemplate)) return false;
    
    var from = TraceEventsConstants.ThreadFrameTemplate.Length;
    var to = frame.IndexOf(')');
    if (!int.TryParse(frame.AsSpan(from, to - from), out threadId))
    {
      threadId = -1;
    }

    return true;
  }
}