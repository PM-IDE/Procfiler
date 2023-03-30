namespace Procfiler.Core.Exceptions;

public class NotExpectedStateException : ProcfilerException
{
  public NotExpectedStateException(Type expectedType, Type actualType)
    : base($"Expected {expectedType.Name} but found {actualType.Name}")
  {
    
  }
}