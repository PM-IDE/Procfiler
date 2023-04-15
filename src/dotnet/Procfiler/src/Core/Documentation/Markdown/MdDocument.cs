using Procfiler.Utils;

namespace Procfiler.Core.Documentation.Markdown;

public class MdDocument : IEnumerable, IMdDocumentPart
{
  private readonly List<IMdDocumentPart> myParts;

  
  public string Name { get; }

  
  public MdDocument(string name)
  {
    Name = name;
    myParts = new List<IMdDocumentPart>();
  }


  public void Add(IMdDocumentPart documentPart)
  {
    myParts.Add(documentPart);
  }

  public IEnumerator GetEnumerator() => myParts.GetEnumerator();
  
  public StringBuilder Serialize(StringBuilder sb)
  {
    foreach (var part in myParts)
    {
      part.Serialize(sb).AppendNewLine();
    }

    return sb;
  }
}