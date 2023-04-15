using Mono.Cecil;
using Procfiler.Core.Constants;
using Procfiler.Utils;

namespace Procfiler.Core.InstrumentalProfiler;

public interface IAssembliesProvider
{
  IReadOnlyDictionary<string, AssemblyDefWithPath> Assemblies { get; }

  void Initialize();
}

public class FolderBasedAssemblyResolver : IAssemblyResolver, IAssembliesProvider
{
  private readonly IProcfilerLogger myLogger;
  private readonly string myContextFolder;
  private readonly Dictionary<string, AssemblyDefWithPath> myContextAssembliesToPaths;

  private bool myContextAssembliesInitialized;


  public IReadOnlyDictionary<string, AssemblyDefWithPath> Assemblies => myContextAssembliesToPaths;


  public FolderBasedAssemblyResolver(IProcfilerLogger logger, string contextFolder)
  {
    myLogger = logger;
    myContextFolder = contextFolder;
    myContextAssembliesToPaths = new Dictionary<string, AssemblyDefWithPath>();
  }


  public AssemblyDefWithPath ResolveWithPath(AssemblyNameReference reference, ReaderParameters parameters)
  {
    InitializeContextAssembliesIfNeeded(parameters);
    if (myContextAssembliesToPaths.TryGetValue(reference.FullName, out var assemblyDefinition))
    {
      return assemblyDefinition;
    }

    throw new FailedToResolveAssemblyException(this, reference);
  }
  
  public AssemblyDefinition Resolve(AssemblyNameReference reference) => Resolve(reference, CreateReaderParameters());

  public AssemblyDefinition Resolve(AssemblyNameReference reference, ReaderParameters parameters) =>
    ResolveWithPath(reference, parameters).Assembly;

  private ReaderParameters CreateReaderParameters() => new()
  {
    ReadWrite = false,
    InMemory = true,
    ReadSymbols = false,
    AssemblyResolver = this
  };
  
  public void Initialize() => InitializeContextAssembliesIfNeeded(CreateReaderParameters());

  private void InitializeContextAssembliesIfNeeded(ReaderParameters readerParameters)
  {
    if (myContextAssembliesInitialized) return;

    foreach (var filePath in Directory.GetFiles(myContextFolder))
    {
      if (!filePath.EndsWith(DotNetConstants.DllExtension)) continue;
      
      try
      {
        var assembly = AssemblyDefinition.ReadAssembly(filePath, readerParameters);
        if (assembly.FullName is null)
        {
          myLogger.LogWarning("Full name for assembly {Path} was null, skipping it", filePath);
          continue;
        }

        myContextAssembliesToPaths[assembly.FullName] = new AssemblyDefWithPath(assembly, filePath);
      }
      catch (Exception)
      {
        myLogger.LogWarning("Failed to read assembly {Path}", filePath);
      }
    }
    
    myContextAssembliesInitialized = true;
  }
  
  public void Dispose()
  {
  }
}