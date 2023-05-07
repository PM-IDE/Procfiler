using Procfiler.Core;
using Procfiler.Core.Documentation;
using Procfiler.Core.Exceptions;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Commands.Documentation;

[CommandLineCommand]
public class CreateDocumentationCommand : IVisibleToUserCommand
{
  private readonly Option<string> myOutputPathOption;
  private readonly IDocumentationCreator myDocumentationCreator;
  private readonly IProcfilerLogger myLogger;


  public CreateDocumentationCommand(IDocumentationCreator documentationCreator, IProcfilerLogger logger)
  {
    myDocumentationCreator = documentationCreator;
    myLogger = logger;
    myOutputPathOption = new Option<string>("-o", "The path to the directory into which the documentation will be generated")
    {
      IsRequired = true
    };
  }
  
  
  public int Invoke(InvocationContext context)
  {
    return InvokeAsync(context).GetAwaiter().GetResult();
  }

  public async Task<int> InvokeAsync(InvocationContext context)
  {
    try
    {
      var outputPath = context.ParseResult.GetValueForOption(myOutputPathOption) ??
                       throw new MissingOptionException(myOutputPathOption);
    
      await myDocumentationCreator.CreateDocumentationAsync(outputPath);
      return 0;
    }
    catch (Exception ex)
    {
      myLogger.LogError(ex, "Error occured while creating documentation");
      return 1;
    }
  }

  public Command CreateCommand()
  {
    var command = new Command(
      "create-documentation", "Generate documentation for filters, mutators and other application components");
    
    command.AddOption(myOutputPathOption);
    command.Handler = this;
    return command;
  }
}