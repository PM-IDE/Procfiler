namespace Procfiler.Commands.CollectClrEvents.Context;

public interface IParseResultInfoProvider
{
  T? TryGetOptionValue<T>(Option<T> option);
}

public class ParseResultInfoProviderImpl : IParseResultInfoProvider
{
  private readonly ParseResult myParseResult;

  
  public ParseResultInfoProviderImpl(ParseResult parseResult)
  {
    myParseResult = parseResult;
  }

  
  public T? TryGetOptionValue<T>(Option<T> option)
  {
    return myParseResult.GetValueForOption(option);
  }
}