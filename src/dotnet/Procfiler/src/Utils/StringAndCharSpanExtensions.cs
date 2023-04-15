namespace Procfiler.Utils;

public static class StringAndCharSpanExtensions
{
  private const int Pow = 31;
  private const int Mod = (int)1e9 + 7;

  public static int CalculateHash(this ReadOnlySpan<char> str)
  {
    var currentPower = 1;
    var hash = 0;
    foreach (var c in str)
    {
      hash = (hash + c * currentPower) % Mod;
      currentPower = (currentPower * Pow) % Mod;
    }

    return hash;
  }

  public static string RemoveRn(this string text) => text.Replace("\r\n", "\n");
}