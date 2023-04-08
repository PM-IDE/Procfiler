namespace Procfiler.Utils.Xml;

internal readonly struct StartEndElementCookie : IAsyncDisposable
{
  public static async Task<StartEndElementCookie> CreateStartElementAsync(
    XmlWriter xmlWriter, string? prefix, string tagName, string? @namespace)
  {
    await xmlWriter.WriteStartElementAsync(prefix, tagName, @namespace);
    return new StartEndElementCookie(xmlWriter);
  }
  
  
  private readonly XmlWriter myXmlWriter;

  
  private StartEndElementCookie(XmlWriter xmlWriter)
  {
    myXmlWriter = xmlWriter;
  }

  
  public ValueTask DisposeAsync()
  {
    return new ValueTask(myXmlWriter.WriteEndElementAsync());
  }
}