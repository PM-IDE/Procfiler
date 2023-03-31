using Procfiler.Core.Collector;
using Procfiler.Core.InstrumentalProfiler;
using Procfiler.Utils;

namespace Procfiler.Commands.CollectClrEvents.Base;

public partial class CollectCommandBase
{
  protected void AddCommonOptions(Command command)
  {
    command.AddOption(ProcessIdOption);
    command.AddOption(PathToCsprojOption);
    command.AddOption(OutputPathOption);
    command.AddOption(DurationOption);
    command.AddOption(OutputFileFormat);
    command.AddOption(TimeoutOption);
    command.AddOption(ClearPathBefore);
    command.AddOption(MergeFromUndefinedThread);
    command.AddOption(TfmOption);
    command.AddOption(ConfigurationOption);
    command.AddOption(ProvidersCategory);
    command.AddOption(InstrumentCodeOption);
    command.AddOption(TempPathOption);
    command.AddOption(RemoveTempFolder);
  }

  private Option<string> TempPathOption { get; } =
    new("--temp", static () => string.Empty, "Folder which will be used for temp artifacts of events collection");

  private Option<bool> RemoveTempFolder { get; } =
    new("--remove-temp", static () => true, "Whether to remove temp directory for artifacts after finishing work");

  private Option<InstrumentationKind> InstrumentCodeOption { get; } =
    new("--instrument", static () => InstrumentationKind.OnlyMainAssembly, "Kind of instrumentation to be used");

  private Option<ProvidersCategoryKind> ProvidersCategory { get; } =
    new("--providers", static () => ProvidersCategoryKind.All, "Providers which will be used for collecting events");

  protected Option<int> RepeatOption { get; } =
    new("--repeat", static () => 1, "The number of times launching of the program should be repeated");

  private Option<string> PathToCsprojOption { get; } = new("-csproj", "The path to the .csproj file of the project to be executed");

  private Option<string> TfmOption { get; } =
    new("-tfm", static () => "net6.0", "The target framework identifier, the project will be built for specified tfm");

  private Option<BuildConfiguration> ConfigurationOption { get; } =
    new("-c", static () => BuildConfiguration.Debug, "Build configuration which will be used during project build");

  private Option<int> ProcessIdOption { get; } = new("-p", "The process id from which we should collect CLR events");

  private Option<string> OutputPathOption { get; } = new("-o", "The output path")
  {
    IsRequired = true
  };

  private Option<int> DurationOption { get; } = 
    new("--duration", static () => 10_000, "The amount of time to spend collecting CLR events");

  private Option<int> TimeoutOption { get; } =
    new("--timeout", static () => 10_000, "The timeout which we want to wait until processing all events");

  private Option<FileFormat> OutputFileFormat { get; } = 
    new("--format", static () => FileFormat.Csv, "The output file(s) format");

  private Option<bool> ClearPathBefore { get; } = 
    new("--clear-before", static () => true, "Clear (delete) output folder (file) before profiling session");
  
  protected Option<bool> MergeFromUndefinedThread { get; } =
    new("--merge-undefined-events", static () => true, "Should we merge events from undefined thread to managed thread events");
}