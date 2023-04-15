namespace Procfiler.Core.Exceptions;

public class MissingOptionException : ProcfilerException
{
  public MissingOptionException(Option option) : base($"Missing option {option.Name}")
  {
  }
}

public class OneOfFollowingOptionsMustBeSpecifiedException : Exception
{
  public OneOfFollowingOptionsMustBeSpecifiedException(params Option[] options) 
    : base($"One of following options must be specified: {string.Join(", ", options.Select(o => o.Name))}")
  {
  }
}