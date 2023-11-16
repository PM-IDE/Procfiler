using Procfiler.Commands.CollectClrEvents.Split;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsCollection;
using Procfiler.Core.SplitByMethod;
using Procfiler.Utils;

namespace Procfiler.Core.Serialization.XES.Online;

public abstract class OnlineMethodsSerializerBase<TState>(
  string outputDirectory,
  Regex? targetMethodsRegex,
  IFullMethodNameBeautifier methodNameBeautifier,
  IProcfilerEventsFactory factory,
  IProcfilerLogger logger,
  bool writeAllEventMetadata) : IOnlineMethodsSerializer, IDisposable
{
  protected readonly string OutputDirectory = outputDirectory;
  protected readonly Regex? TargetMethodsRegex = targetMethodsRegex;
  protected readonly IFullMethodNameBeautifier FullMethodNameBeautifier = methodNameBeautifier;
  protected readonly IProcfilerEventsFactory Factory = factory;
  protected readonly IProcfilerLogger Logger = logger;
  protected readonly bool WriteAllEventMetadata = writeAllEventMetadata;

  protected readonly List<string> MethodNames = new();
  protected readonly Dictionary<string, PathWriterStateWithLastEvent> States = new();


  public IReadOnlyList<string> AllMethodNames => MethodNames; 
  
  
  public void SerializeThreadEvents(IEnumerable<EventRecordWithPointer> events, string filterPattern, InlineMode inlineMode)
  {
    var splitter = new CallbackBasedSplitter<TState>(
      logger, events, filterPattern, inlineMode, TryCreateState, HandleUpdate);
    
    splitter.Split();
  }

  protected abstract TState? TryCreateState(EventRecordWithMetadata contextEvent);
  protected abstract void HandleUpdate(EventUpdateBase<TState> update);

  public abstract void Dispose();
}