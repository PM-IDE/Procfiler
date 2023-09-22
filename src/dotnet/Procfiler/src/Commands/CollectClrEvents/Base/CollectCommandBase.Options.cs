﻿using Procfiler.Core.Collector;
using Procfiler.Core.CppProcfiler;
using Procfiler.Core.InstrumentalProfiler;
using Procfiler.Utils;

namespace Procfiler.Commands.CollectClrEvents.Base;

public partial class CollectCommandBase
{
  protected void AddCommonOptions(Command command)
  {
    command.AddOption(ProcessIdOption);
    command.AddOption(CommandNameOption);
    command.AddOption(PathToCsprojOption);
    command.AddOption(OutputPathOption);
    command.AddOption(DurationOption);
    command.AddOption(OutputFileFormatOption);
    command.AddOption(TimeoutOption);
    command.AddOption(ClearPathBeforeOption);
    command.AddOption(MergeFromUndefinedThreadOption);
    command.AddOption(TfmOption);
    command.AddOption(ConfigurationOption);
    command.AddOption(ProvidersCategoryOption);
    command.AddOption(InstrumentCodeOption);
    command.AddOption(SelfContainedOption);
    command.AddOption(TempPathOption);
    command.AddOption(ClearArtifactsOption);
    command.AddOption(ArgumentsOption);
    command.AddOption(ArgumentsFileOption);
    command.AddOption(PrintProcessOutputOption);
    command.AddOption(FilterOption);
    command.AddOption(AdditionalBuildArgsOption);
    command.AddOption(ProcessWaitTimeoutOption);
    command.AddOption(UseCppProfilerOption);
    command.AddOption(UseDuringRuntimeMethodsFiltering);
    command.AddOption(UseCppProfilerConsoleLogging);
  }

  private Option<bool> SelfContainedOption { get; } =
    new("--self-contained", static () => false, "Whether to build application in a self-contained mode");

  private Option<bool> PrintProcessOutputOption { get; } =
    new("--print-process-output", static () => true, "Whether to print the output of a profiled application");

  private Option<string> ArgumentsFileOption { get; } =
    new("--arguments-file", static () => string.Empty, "File containing list of arguments which will be passed to program");

  private Option<string> ArgumentsOption { get; } =
    new("--arguments", static () => string.Empty, "Arguments which will be passed when launching the program");

  private Option<string> TempPathOption { get; } =
    new("--temp", static () => string.Empty, "Folder which will be used for temp artifacts of events collection");

  private Option<bool> ClearArtifactsOption { get; } =
    new("--clear-artifacts", static () => true, "Whether to remove temp directory for artifacts after finishing work");

  private Option<InstrumentationKind> InstrumentCodeOption { get; } =
    new("--instrument", static () => InstrumentationKind.None, "Kind of instrumentation to be used");

  private Option<ProvidersCategoryKind> ProvidersCategoryOption { get; } =
    new("--providers", static () => ProvidersCategoryKind.All, "Providers which will be used for collecting events");

  protected Option<int> RepeatOption { get; } =
    new("--repeat", static () => 1, "The number of times launching of the program should be repeated");

  private Option<string> CommandNameOption { get; } = new("-command", "The command to be executed");

  private Option<string> PathToCsprojOption { get; } =
    new("-csproj", "The path to the .csproj file of the project to be executed");

  private Option<string> TfmOption { get; } =
    new("--tfm", static () => "net6.0", "The target framework identifier, the project will be built for specified tfm");

  private Option<BuildConfiguration> ConfigurationOption { get; } =
    new("--c", static () => BuildConfiguration.Debug, "Build configuration which will be used during project build");

  private Option<int> ProcessIdOption { get; } = new("-p", "The process id from which we should collect CLR events");

  private Option<string> OutputPathOption { get; } = new("-o", "The output path")
  {
    IsRequired = true
  };

  private Option<int> DurationOption { get; } =
    new("--duration", static () => 60_000, "The amount of time to spend collecting CLR events");

  private Option<int> TimeoutOption { get; } =
    new("--timeout", static () => 10_000, "The timeout (ms) which we want to wait until processing all events");

  private Option<int> ProcessWaitTimeoutOption { get; } =
    new("--process-wait-timeout", static () => 10_000, "The timeout (ms) which we will wait until process naturally exits");

  private Option<FileFormat> OutputFileFormatOption { get; } =
    new("--format", static () => FileFormat.Csv, "The output file(s) format");

  private Option<bool> ClearPathBeforeOption { get; } =
    new("--clear-before", static () => true, "Clear (delete) output folder (file) before profiling session");

  protected Option<string> FilterOption { get; } =
    new("--filter", static () => string.Empty, "Regex to filter methods");

  private Option<string> AdditionalBuildArgsOption { get; } =
    new("--additional-build-args", static () => string.Empty, "Additional arguments for an application build command");
  
  protected Option<bool> MergeFromUndefinedThreadOption { get; } =
    new("--merge-undefined-events", static () => true, "Should we merge events from undefined thread to managed thread events");

  private Option<CppProfilerMode> UseCppProfilerOption { get; } =
    new("--cpp-profiler-mode", static () => CppProfilerMode.SingleFileBinStack, "Should we load cpp profiler");

  private Option<bool> UseDuringRuntimeMethodsFiltering { get; } = 
    new("--use-during-runtime-filtering", static () => false, "Whether to use during runtime methods filtering");

  private Option<bool> UseCppProfilerConsoleLogging { get; } =
    new("--cpp-profiler-use-console-logging", static () => false, "Enable console logging in cpp profiler");
}