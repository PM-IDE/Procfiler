using Mono.Cecil;
using Procfiler.Core.Exceptions;
using Procfiler.Utils;

namespace Procfiler.Core.InstrumentalProfiler;

public record AssemblyDefWithPath(AssemblyDefinition Assembly, string PhysicalPath);

public class SelfContainedTypeCache
{
  private readonly IProcfilerLogger myLogger;
  private readonly Dictionary<string, TypeDefinition> myCache;
  private readonly Dictionary<string, AssemblyDefWithPath> myAssemblies;
  private readonly FolderBasedAssemblyResolver myResolver;


  public IReadOnlyDictionary<string, AssemblyDefWithPath> Assemblies => myAssemblies;
  public IReadOnlyDictionary<string, TypeDefinition> Types => myCache;


  public SelfContainedTypeCache(IProcfilerLogger logger, string contextFolder)
  {
    myLogger = logger;
    myCache = new Dictionary<string, TypeDefinition>();
    myResolver = new FolderBasedAssemblyResolver(logger, contextFolder);
    myAssemblies = new Dictionary<string, AssemblyDefWithPath>();
  }

  
  public void ProcessAssembly(AssemblyDefWithPath assemblyDefWithPath, bool processReferences)
  {
    var (assemblyDefinition, _) = assemblyDefWithPath;
    if (myAssemblies.ContainsKey(assemblyDefinition.FullName)) return;

    myAssemblies[assemblyDefinition.FullName] = assemblyDefWithPath;

    foreach (var module in assemblyDefinition.Modules)
    {
      foreach (var type in module.Types)
      {
        if (myCache.TryGetValue(type.FullName, out var existingType))
        {
          myLogger.LogError("Already have type def for {FullName}: {TypeDef}", type.FullName, existingType);
          continue;
        }
        
        myCache[type.FullName] = type;
      }

      if (processReferences)
      {
        foreach (var reference in module.AssemblyReferences)
        {
          try
          {
            ProcessAssembly(myResolver.ResolveWithPath(reference, new ReaderParameters
            {
              ReadWrite = true,
              ReadSymbols = false,
              InMemory = true,
              AssemblyResolver = myResolver
            }), true);
          }
          catch (FailedToResolveAssemblyException ex)
          {
            myLogger.LogWarning(ex, "Failed to resolve assembly {Name}", reference.FullName);
          }
        }
      }
    }
  }
}

public class FailedToResolveAssemblyException : ProcfilerException
{
  public FailedToResolveAssemblyException(IAssemblyResolver resolver, AssemblyNameReference reference)
    :base($"Failed to resolve {reference.FullName} from resolver: {resolver.GetType().Name}")
  {
  }
}