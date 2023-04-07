using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.InstrumentalProfiler;

public interface IDepsJsonPatcher
{
  Task AddAssemblyReferenceAsync(string depsJsonPath, string assemblyName, Version version);
}

public class DepsJsonFile
{
  public TopLevelRuntimeTarget RuntimeTarget { get; set; }
  public CompilationOptions CompilationOptions { get; set; }
  public Targets Targets { get; set; }
  public Libraries Libraries { get; set; }
  public Runtimes Runtimes { get; set; }
}

public class CompilationOptions
{
  public string Name { get; set; }
}

public class TopLevelRuntimeTarget
{
  public NameWithVersion NameWithVersion { get; set; }
  public string Signature { get; set; }
}

public class Targets
{
  public List<TargetItem> TargetsList { get; init; }
}

public class Libraries
{
  public List<LibraryEntry> LibrariesList { get; }
}

public class LibraryEntry
{
  public string Type { get; set; }
  public string Path { get; set; }
  public bool Serviceable { get; set; }
  public string Sha512 { get; set; }
  public string HashPath { get; set; }
}

public class Runtimes
{
  public List<Runtime> RuntimesList { get; }
}

public class Runtime
{
  public string Rid { get; set; }
  public List<string> Fallbacks { get; }
}

public class TargetItem
{
  public NameWithVersion NameWithVersion { get; set; }
  public List<TargetDependency> Dependencies { get; init; }
}

public class TargetDependency
{
  public List<DependencyOfTargetDependency> Dependencies { get; init; }
  public List<RuntimeTarget> RuntimeTargets { get; init; }
  public List<FileInfo> Runtime { get; init; }
  public List<FileInfo> Native { get; init; }
}

public class NameWithVersion
{
  public static NameWithVersion Parse(string rawName) => new()
  {
    Name = rawName
  };
  
  
  public string Name { get; init; }
  public string? Version { get; init; }
}

public class DependencyOfTargetDependency
{
  public NameWithVersion NameWithVersion { get; init; }
  public Version Version { get; set; }
}

public class FileInfo
{
  public string Name { get; set; }
  public Version AssemblyVersion { get; set; }
  public Version? FileVersion { get; set; }
}

public class RuntimeTarget
{
  public string Name { get; set; }
  public string Rid { get; set; }
  public string AssetType { get; set; }
  public Version AssemblyVersion { get; set; }
}

public static class DepsJsonConstants
{
  public const string RuntimeTarget = "runtimeTarget";
  public const string CompilationOptions = "compilationOptions";
  public const string Targets = "targetsc";
  public const string Libraries = "libraries";
  public const string Runtimes = "runtimes";
  public const string Runtime = "runtime";
  public const string Native = "native";
  public const string RuntimeTargets = "runtimeTargets";
  public const string Type = "type";
  public const string Path = "path";
  public const string Serviceable = "serviceable";
  public const string Sha512 = "sha512";
  public const string HashPath = "hashPath";
  public const string Name = "name";
  public const string Signature = "signature";
  public const string Dependencies = "dependencies";
  public const string AssemblyVersion = "assemblyVersion";
  public const string FileVersion = "fileVersion";
  public const string Rid = "rid";
  public const string AssetType = "assetType";
}

public static class DepsJsonReaderWriter
{
  public Task<DepsJsonFile> ParseAsync(string path)
  {
    var document = JsonDocument.Parse(path);
    var file = new DepsJsonFile();
    foreach (var element in document.RootElement.EnumerateObject())
    {
      switch (element.Name)
      {
        case DepsJsonConstants.RuntimeTarget:
        {
          file.RuntimeTarget = new TopLevelRuntimeTarget
          {
            Signature = element.Value.GetProperty(DepsJsonConstants.Signature).GetString(),
            NameWithVersion = NameWithVersion.Parse(element.Value.GetProperty(DepsJsonConstants.Name).GetString())
          };
          break;
        }
        case DepsJsonConstants.Targets:
        {
          file.Targets = new Targets
          {
            TargetsList = element.Value.EnumerateObject().Select(t =>
            {
              return new TargetItem
              {
                NameWithVersion = NameWithVersion.Parse(t.Name),
                Dependencies = t.Value.EnumerateObject().Select(d =>
                {
                  JsonElement? dependencies = null;
                  JsonElement? runtime = null;
                  JsonElement? runtimeTargets = null;
                  JsonElement? native = null;

                  foreach (var property in d.Value.EnumerateObject())
                  {
                    switch (property.Name)
                    {
                      case DepsJsonConstants.Dependencies:
                        dependencies = property.Value;
                        break;
                      case DepsJsonConstants.Runtime:
                        runtime = property.Value;
                        break;
                      case DepsJsonConstants.Native:
                        native = property.Value;
                        break;
                      case DepsJsonConstants.RuntimeTargets:
                        runtimeTargets = property.Value;
                        break;
                    }
                  }

                  return new TargetDependency
                  {
                    Dependencies = dependencies?.EnumerateObject().Select(d => new DependencyOfTargetDependency
                    {
                      NameWithVersion = NameWithVersion.Parse(d.Name),
                      Version = Version.Parse(d.Value.GetString())
                    }).ToList() ?? EmptyCollections<DependencyOfTargetDependency>.EmptyList,
                    Native = native?.EnumerateObject().Select(n => new FileInfo
                    {
                      Name = n.Name,
                      AssemblyVersion =
                        Version.Parse(n.Value.GetProperty(DepsJsonConstants.AssemblyVersion).GetString()),
                      FileVersion = Version.Parse(n.Value.GetProperty(DepsJsonConstants.FileVersion).GetString())
                    }).ToList() ?? EmptyCollections<FileInfo>.EmptyList,
                    Runtime = runtime?.EnumerateObject().Select(n => new FileInfo
                    {
                      Name = n.Name,
                      AssemblyVersion =
                        Version.Parse(n.Value.GetProperty(DepsJsonConstants.AssemblyVersion).GetString()),
                      FileVersion = Version.Parse(n.Value.GetProperty(DepsJsonConstants.FileVersion).GetString())
                    }).ToList() ?? EmptyCollections<FileInfo>.EmptyList,
                    RuntimeTargets = runtimeTargets?.EnumerateObject().Select(rt => new RuntimeTarget
                    {
                      Name = rt.Name,
                      Rid = rt.Value.GetProperty(DepsJsonConstants.Rid).GetString(),
                      AssetType = rt.Value.GetProperty(DepsJsonConstants.AssetType).GetString(),
                      AssemblyVersion =
                        Version.Parse(rt.Value.GetProperty(DepsJsonConstants.AssemblyVersion).GetString())
                    }).ToList() ?? EmptyCollections<RuntimeTarget>.EmptyList,
                  };
                }).ToList(),
              };
            }).ToList()
          };
          
          break;
        }
      }
    }
  }
}

[AppComponent]
public class DepsJsonPatcherImpl : IDepsJsonPatcher
{
  private readonly IProcfilerLogger myLogger;

  
  public DepsJsonPatcherImpl(IProcfilerLogger logger)
  {
    myLogger = logger;
  }

  
  public Task AddAssemblyReferenceAsync(string depsJsonPath, string assemblyName, Version version)
  {
    try
    {
      var json = JsonDocument.Parse(depsJsonPath);
      foreach (var property in json.RootElement.EnumerateObject())
      {
        if (property.NameEquals("targets"))
        {
          json.RootElement[0]. 
        }
      }
    }
    catch (Exception ex)
    {
      myLogger.LogError(ex, "");
    }
  }
  
  private 
}