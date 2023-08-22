using Procfiler.Utils.Container;

namespace Procfiler.Core.SplitByMethod;

public interface IFullMethodNameBeautifier
{
  string Beautify(string fullMethodName);
}

[AppComponent]
public class FullMethodNameBeautifierImpl : IFullMethodNameBeautifier
{
  private static readonly Regex ourGenericPartFilter = new(@"\[*\]");


  public string Beautify(string fullMethodName)
  {
    foreach (var invalidChar in Path.GetInvalidFileNameChars())
    {
      fullMethodName = fullMethodName.Replace(invalidChar.ToString(), string.Empty);
    }

    fullMethodName = ourGenericPartFilter.Replace(fullMethodName, string.Empty).Replace("!", string.Empty);
    var indexOfOpenBrace = fullMethodName.IndexOf('(', StringComparison.Ordinal);
    if (indexOfOpenBrace < 0) return fullMethodName;

    fullMethodName = fullMethodName[..indexOfOpenBrace];

    const int MaxMethodNameLength = 250;
    if (fullMethodName.Length > MaxMethodNameLength)
    {
      fullMethodName = fullMethodName[^MaxMethodNameLength..];
    }

    return fullMethodName;
  }
}