using Procfiler.Utils.Container;

namespace Procfiler.Core.InstrumentalProfiler.DepsJson;

public interface IDepsJsonPatcher
{
  Task AddAssemblyReferenceAsync(string depsJsonPath, string assemblyName, Version version);
}

[AppComponent]
public class DepsJsonPatcherImpl : IDepsJsonPatcher
{
  private readonly IDepsJsonReaderWriter myDepsJsonReaderWriter;

  
  public DepsJsonPatcherImpl(IDepsJsonReaderWriter depsJsonReaderWriter)
  {
    myDepsJsonReaderWriter = depsJsonReaderWriter;
  }

  
  public Task AddAssemblyReferenceAsync(string depsJsonPath, string assemblyName, Version version)
  {
    throw new NotImplementedException();
  }
}