using Procfiler.Utils;

namespace Procfiler.Core.Serialization.XES;

public static class XesSerializersUtil
{
  public static void DisposeWriters(IEnumerable<(string, XmlWriter)> writers, IProcfilerLogger logger)
  {
    foreach (var (path, writer) in writers)
    {
      try
      {
        writer.WriteEndElement();
        writer.Dispose();
      }
      catch (Exception ex)
      {
        logger.LogWarning(ex, "Failed to dispose writer for path {Path}", path);
      }
    }
  }
}