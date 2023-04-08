using Procfiler.Utils.Container;

namespace Procfiler.Core.InstrumentalProfiler.DepsJson;

public interface IDepsJsonPatcher
{
  Task AddAssemblyReferenceAsync(string depsJsonPath, string assemblyName, Version version);
}

[AppComponent]
public class DepsJsonPatcherImpl : IDepsJsonPatcher
{
  private readonly IDepsJsonReader myDepsJsonReader;
  private readonly IDepsJsonWriter myDepsJsonWriter;

  
  public DepsJsonPatcherImpl(IDepsJsonReader depsJsonReader, IDepsJsonWriter depsJsonWriter)
  {
    myDepsJsonReader = depsJsonReader;
    myDepsJsonWriter = depsJsonWriter;
  }

  
  public async Task AddAssemblyReferenceAsync(string depsJsonPath, string assemblyName, Version version)
  {
    var depsJsonFile = await myDepsJsonReader.ReadOrThrowAsync(depsJsonPath);
    await myDepsJsonWriter.WriteAsync(depsJsonPath, depsJsonFile);
  }
}