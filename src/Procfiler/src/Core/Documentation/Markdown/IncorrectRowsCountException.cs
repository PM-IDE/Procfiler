namespace Procfiler.Core.Documentation.Markdown;

public class IncorrectRowsCountException : Exception
{
  public IncorrectRowsCountException(int actualRowsCount, int expectedRowsCount)
    : base($"Expected {expectedRowsCount}, but got {actualRowsCount}")
  {
  }
}