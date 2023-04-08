using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.InstrumentalProfiler.DepsJson;

public interface IDepsJsonReaderWriter
{
  Task<DepsJsonFile> ReadOrThrowAsync(string path);
  Task WriteAsync(string path, DepsJsonFile file);
}

[AppComponent]
public class DepsJsonReaderWriter : IDepsJsonReaderWriter
{
  public Task WriteAsync(string path, DepsJsonFile file)
  {
    throw new NotImplementedException();
  }

  public async Task<DepsJsonFile> ReadOrThrowAsync(string path)
  {
    await using var fs = File.OpenRead(path);
    var document = await JsonDocument.ParseAsync(fs);
    
    var file = new DepsJsonFile();
    foreach (var element in document.RootElement.EnumerateObject())
    {
      switch (element.Name)
      {
        case DepsJsonConstants.RuntimeTarget:
        {
          file.RuntimeTarget = new TopLevelRuntimeTarget
          {
            Signature = element.Value.GetPropertyStringValueOrThrow(DepsJsonConstants.Signature),
            NameWithVersion = NameWithVersion.Parse(element.Value.GetPropertyStringValueOrThrow(DepsJsonConstants.Name))
          };
          
          break;
        }
        case DepsJsonConstants.Targets:
        {
          file.Targets = new Targets
          {
            TargetsList = element.Value.EnumerateObject().Select(ParseTargetItem).ToList()
          };
          
          break;
        }
        case DepsJsonConstants.Libraries:
        {
          file.Libraries = new Libraries
          {
            LibrariesList = element.Value.EnumerateObject().Select(ParseLibrary).ToList()
          };

          break;
        }
        case DepsJsonConstants.Runtimes:
        {
          file.Runtimes = new Runtimes
          {
            RuntimesList = element.Value.EnumerateObject().Select(ParseRuntime).ToList()
          };
          
          break;
        }
        case DepsJsonConstants.CompilationOptions:
        {
          file.CompilationOptions = new CompilationOptions
          {
            Options = element.Value.EnumerateObject().Select(obj => obj.Value.GetStringValueOrThrow()).ToList()
          };

          break;
        }
      }
    }

    return file;
  }

  private static Runtime ParseRuntime(JsonProperty property) => new()
  {
    Rid = property.Name,
    Fallbacks = property.Value.EnumerateArray().Select(el => el.GetStringValueOrThrow()).ToList()
  };

  private static LibraryEntry ParseLibrary(JsonProperty property) => new()
  {
    Name = property.Name,
    Path = property.Value.TryGetPropertyStringValue(DepsJsonConstants.Path),
    Serviceable = property.Value.TryGetPropertyBoolValue(DepsJsonConstants.Serviceable),
    Sha512 = property.Value.TryGetPropertyStringValue(DepsJsonConstants.Sha512),
    Type = property.Value.TryGetPropertyStringValue(DepsJsonConstants.Type),
    HashPath = property.Value.TryGetPropertyStringValue(DepsJsonConstants.HashPath)
  };

  private static TargetItem ParseTargetItem(JsonProperty property)
  {
    return new TargetItem
    {
      NameWithVersion = NameWithVersion.Parse(property.Name),
      Dependencies = property.Value.EnumerateObject().Select(dependency =>
      {
        JsonElement? dependencies = null;
        JsonElement? runtime = null;
        JsonElement? runtimeTargets = null;
        JsonElement? native = null;
        
        foreach (var property in dependency.Value.EnumerateObject())
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
          Dependencies = SelectFrom(dependencies, ParseDependencyOfTargetDependency),
          Native = SelectFrom(native, ParseFileInfo).ToList(),
          Runtime = SelectFrom(runtime, ParseFileInfo).ToList(),
          RuntimeTargets = SelectFrom(runtimeTargets, ParseRuntimeTargets).ToList(),
        };
      }).ToList(),
    };
  }

  private static List<T> SelectFrom<T>(JsonElement? element, Func<JsonProperty, T> selector)
  {
    return element?.EnumerateObject().Select(selector).ToList() ??
           EmptyCollections<T>.EmptyList;
  } 

  private static DependencyOfTargetDependency ParseDependencyOfTargetDependency(JsonProperty property) => new()
  {
    NameWithVersion = NameWithVersion.Parse(property.Name),
    Version = ParseVersionOrThrow(property.Value)
  };

  private static RuntimeTarget ParseRuntimeTargets(JsonProperty property) => new()
  {
    Name = property.Name,
    Rid = property.Value.GetPropertyStringValueOrThrow(DepsJsonConstants.Rid),
    AssetType = property.Value.GetPropertyStringValueOrThrow(DepsJsonConstants.AssetType),
    AssemblyVersion = ParseVersionOrThrow(property.Value.GetProperty(DepsJsonConstants.AssemblyVersion))
  };
  
  private static FileInfo ParseFileInfo(JsonProperty property) => new()
  {
    Name = property.Name,
    AssemblyVersion = TryParseVersion(property.Value.GetProperty(DepsJsonConstants.AssemblyVersion)),
    FileVersion = TryParseVersion(property.Value.GetProperty(DepsJsonConstants.FileVersion))
  };

  private static Version ParseVersionOrThrow(JsonElement? element)
  {
    if (TryParseVersion(element) is not { } version)
    {
      throw new FormatException(element?.GetString());
    }

    return version;
  }
  
  private static Version? TryParseVersion(JsonElement? element)
  {
    if (element is null) return null;

    return Version.TryParse(element.Value.GetString(), out var version) ? version : null;
  }
}