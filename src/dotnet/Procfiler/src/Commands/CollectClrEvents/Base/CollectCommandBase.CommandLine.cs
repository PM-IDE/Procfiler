using System.CommandLine.Binding;
using Procfiler.Commands.CollectClrEvents.Context;
using Procfiler.Core.Exceptions;
using Procfiler.Utils;

namespace Procfiler.Commands.CollectClrEvents.Base;

public abstract partial class CollectCommandBase
{
  public Task<int> InvokeAsync(InvocationContext context) => Task.Run(() => Invoke(context));

  public int Invoke(InvocationContext context)
  {
    var parseResult = context.ParseResult;
    if (CheckForParserErrors(parseResult)) return -1;
    
    CheckForPidOrExePathOrThrow(parseResult);
    
    try
    {
      Execute(CreateCollectClrContextFrom(parseResult));
    }
    catch (Exception ex)
    {
      Logger.LogError(ex.Message);
      return -1;
    }

    return 0;
  }

  public Command CreateCommand()
  {
    var command = CreateCommandInternal();
    command.Handler = this;
    AddCommonOptions(command);
    return command;
  }

  protected abstract Command CreateCommandInternal();

  private CollectingClrEventsCommonContext CreateCommonContext(ParseResult parseResult)
  {
    if (parseResult.GetValueForOption(OutputPathOption) is not { } outputPath)
    {
      throw new MissingOptionException(OutputPathOption);
    }
    
    var fileFormat = parseResult.GetValueForOption(OutputFileFormat);
    var duration = parseResult.GetValueForOption(DurationOption);
    var timeout = parseResult.GetValueForOption(TimeoutOption);
    var clearBefore = parseResult.GetValueForOption(ClearPathBefore);
    var category = parseResult.GetValueForOption(ProvidersCategory);
    var arguments = parseResult.GetValueForOption(ArgumentsOption) ?? string.Empty;
    var printOutput = parseResult.GetValueForOption(PrintProcessOutputOption);
    
    var serializationCtx = new SerializationContext(fileFormat);
    var parseResultInfoProvider = new ParseResultInfoProviderImpl(parseResult);

    return new CollectingClrEventsCommonContext(
      outputPath, serializationCtx, parseResultInfoProvider, arguments, category, clearBefore, duration, timeout, 
      printOutput);
  }
  
  private CollectClrEventsContext CreateCollectClrContextFrom(ParseResult parseResult)
  {
    var commonContext = CreateCommonContext(parseResult);
    if (parseResult.HasOption(ProcessIdOption))
    {
      if (parseResult.HasOption(RepeatOption))
      {
        Logger.LogWarning("The repeat option was specified when attaching to already running process, for now this option will be ignored");
      }
      
      var pid = parseResult.GetValueForOption(ProcessIdOption); 
      return new CollectClrEventsFromRunningProcessContext(pid, commonContext);
    }

    if (parseResult.HasOption(PathToCsprojOption))
    {
      var pathToCsproj = parseResult.GetValueForOption(PathToCsprojOption);
      if (!File.Exists(pathToCsproj))
      {
        throw new ArgumentOutOfRangeException(nameof(pathToCsproj));
      }

      var projectBuildInfo = CreateProjectBuildInfo(parseResult, pathToCsproj);
      if (parseResult.HasOption(RepeatOption) && 
          parseResult.GetValueForOption(RepeatOption) > 1 && 
          parseResult.HasOption(ArgumentsFileOption) &&
          !Equals(parseResult.GetValueForOption(ArgumentsFileOption), ArgumentsFileOption.GetDefaultValue()))
      {
        Logger.LogError("Executing with arguments file and repeat count is not yet supported");
        throw new ArgumentOutOfRangeException();
      }
      
      if (parseResult.HasOption(ArgumentsFileOption))
      {
        var filePath = parseResult.GetValueForOption(ArgumentsFileOption);
        if (!Equals(filePath, ArgumentsFileOption.GetDefaultValue()))
        {
          if (!File.Exists(filePath))
          {
            Logger.LogError("Invalid path to arguments file: {Path}", filePath);
            throw new FileNotFoundException("Failed to find argument file", filePath);
          }

          var arguments = File.ReadAllLines(filePath);
          return new CollectClrEventsFromExeWithArguments(projectBuildInfo, commonContext, arguments); 
        }
      }
      
      if (parseResult.HasOption(RepeatOption))
      {
        var repeatCount = parseResult.GetValueForOption(RepeatOption);
        if (repeatCount < 1)
        {
          throw new ArgumentOutOfRangeException(nameof(RepeatOption), "The -repeat must be greater or equal than 1");
        }

        return new CollectClrEventsFromExeWithRepeatContext(projectBuildInfo, repeatCount, commonContext);
      }
      
      return new CollectClrEventsFromExeContext(projectBuildInfo, commonContext);
    }

    throw new ArgumentOutOfRangeException();
  }

  private ProjectBuildInfo CreateProjectBuildInfo(ParseResult parseResult, string pathToCsproj)
  {
    var tfm = parseResult.GetValueForOption(TfmOption);
    Debug.Assert(tfm is { });

    var buildConfiguration = parseResult.GetValueForOption(ConfigurationOption);
    var instrumentationKind = parseResult.GetValueForOption(InstrumentCodeOption);
    var selfContained = parseResult.GetValueForOption(SelfContainedOption);
    
    var tempPath = parseResult.GetValueForOption(TempPathOption);
    if (Equals(tempPath, ((IValueDescriptor) TempPathOption).GetDefaultValue()))
    {
      tempPath = null;
    }

    var removeTemp = parseResult.GetValueForOption(RemoveTempFolder);
    return new ProjectBuildInfo(
      pathToCsproj, tfm, buildConfiguration, instrumentationKind, removeTemp, tempPath, selfContained);
  }
  
  private void CheckForPidOrExePathOrThrow(ParseResult parseResult)
  {
    if (!parseResult.HasOption(ProcessIdOption) && !parseResult.HasOption(PathToCsprojOption))
    {
      throw new OneOfFollowingOptionsMustBeSpecifiedException(ProcessIdOption, PathToCsprojOption);
    }
  }

  private bool CheckForParserErrors(ParseResult parseResult)
  {
    var errors = parseResult.Errors;
    if (errors.Count <= 0) return false;
    
    foreach (var error in errors)
    {
      Logger.LogError(error.Message);
    }

    return true;
  }
}