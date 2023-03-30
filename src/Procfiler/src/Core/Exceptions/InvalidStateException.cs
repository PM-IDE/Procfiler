namespace Procfiler.Core.Exceptions;

public class InvalidStateException : ProcfilerException
{
  public InvalidStateException(string message) : base(message)
  {
  }
}