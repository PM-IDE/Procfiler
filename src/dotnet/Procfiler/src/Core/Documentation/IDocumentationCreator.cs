using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.Documentation;

public interface IDocumentationCreator
{
  ValueTask CreateDocumentationAsync(string documentationFolder);
}

[AppComponent]
public class DocumentationCreatorImpl(
    IEnumerable<IMarkdownDocumentationProvider> providers, IProcfilerLogger logger) : IDocumentationCreator
{
  public async ValueTask CreateDocumentationAsync(string documentationFolder)
  {
    foreach (var mdDocument in providers.Select(provider => provider.CreateDocumentationFile()))
    {
      var path = Path.Combine(documentationFolder, mdDocument.Name);
      if (!path.EndsWith(".md"))
      {
        path += ".md";
      }

      await using var fs = File.OpenWrite(path);
      fs.SetLength(0);
      await using var sw = new StreamWriter(fs);
      await sw.WriteAsync(mdDocument.Serialize(new StringBuilder()));
    }
  }
}