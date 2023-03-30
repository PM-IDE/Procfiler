using Procfiler.Core.Constants.TraceEvents;
using Procfiler.Core.EventRecord;
using Procfiler.Core.EventsProcessing.Mutators.Core;
using Procfiler.Utils;

namespace Procfiler.Core.EventsProcessing.Mutators;

public static class MutatorsUtil
{
  public static string CreateUnknownEventNamePartAndLog(string name, IProcfilerLogger logger)
  {
    logger.LogError("There is no suitable transformation for {Reason}", name);
    return TraceEventsConstants.Undefined;
  }

  public static void LogAbsenceOfMetadata(this IProcfilerLogger logger, string eventName, string metadataName) =>
    logger.LogError("The {MetadataName} was not present at {EventName}", metadataName, eventName);
  
  public static void DevastateMetadataValuesAndAppendToName(
    IProcfilerLogger logger,
    EventRecordWithMetadata eventRecord,
    IEnumerable<MetadataKeysWithTransform> keysWithTransformation,
    bool removeFromProperties = true)
  {
    var orderedTransforms = keysWithTransformation.OrderBy(transform => transform.EventClassKind).ToList();
    if (orderedTransforms.Count == 0) return; 

    var sb = new StringBuilder();
    sb.Append(eventRecord.EventName);

    var firstEventClass = orderedTransforms.First().EventClassKind;
    var currentSeparator = firstEventClass switch
    {
      EventClassKind.NotEventClass => string.Empty,
      _ => $"{TraceEventsConstants.Underscore}"
    };

    foreach (var (metadataKey, transformation, eventClass) in orderedTransforms)
    {
      if (eventClass != firstEventClass)
      {
        currentSeparator += TraceEventsConstants.Underscore;
      }
      
      var metadata = eventRecord.Metadata;
      if (!metadata.TryGetValue(metadataKey, out var value))
      {
        logger.LogAbsenceOfMetadata(eventRecord.EventClass, metadataKey);
        continue;
      }

      if (removeFromProperties)
      {
        metadata.Remove(metadataKey);
      }

      var namePart = transformation(value);
      sb.Append(currentSeparator).Append(namePart);
    }

    eventRecord.EventName = string.Intern(sb.ToString());
  }

  public static string TransformMethodLikeNameForEventNameConcatenation(string fullMethodName)
  {
    StringBuilder sb = new(fullMethodName);
    if (fullMethodName.IndexOf('!') is var index and >= 0)
    {
      sb.Remove(0, index + 1);
    }

    for (int i = 0; i < sb.Length; i++)
    {
      if (sb[i] == ' ')
      {
        sb[i] = TraceEventsConstants.Underscore;
      }

      sb[i] = char.ToUpper(sb[i]);
    }

    return string.Intern(sb.ToString());
  }
  
  public static string TransformTypeLikeNameForEventNameConcatenation(string typeName)
  {
    return TransformTypeLikeNameForEventNameConcatenation(new StringBuilder(typeName));
  }

  private static string TransformTypeLikeNameForEventNameConcatenation(StringBuilder sb)
  {
    sb.Replace('.', '_');

    for (var i = 0; i < sb.Length; ++i)
    {
      sb[i] = char.ToUpper(sb[i]);
    }

    return string.Intern(sb.ToString());
  }

  public static string TransformCamelCaseForEventNameConcatenation(string name)
  {
    StringBuilder sb = new(name);
    for (var i = sb.Length - 1; i >= 0; i--)
    {
      if (char.IsUpper(sb[i]))
      {
        sb.Insert(i - 1, TraceEventsConstants.Underscore);
        --i;
      }
    }

    for (var i = 0; i < sb.Length; i++)
    {
      sb[i] = char.ToUpper(sb[i]);
    }

    return string.Intern(sb.ToString());
  }

  public static string TransformAssemblyNameForEventNameConcatenation(string assemblyName)
  {
    var index = assemblyName.IndexOf(',', StringComparison.Ordinal);
    if (index == -1) return assemblyName;

    StringBuilder sb = new(assemblyName);
    sb.Remove(index, sb.Length - index);
    
    return TransformTypeLikeNameForEventNameConcatenation(sb);
  }

  public static string TransformModuleFileNameForEventNameConcatenation(string fileName)
  {
    var indexOfExtension = fileName.IndexOf(".dll", StringComparison.Ordinal);
    if (indexOfExtension == -1) return fileName;

    StringBuilder sb = new(fileName);
    sb.Remove(indexOfExtension, sb.Length - indexOfExtension);
    
    return TransformTypeLikeNameForEventNameConcatenation(sb);
  }

  public static string TransformDomainModuleFilePathForEventNameConcatenation(string filePath)
  {
    var indexOfLastSeparator = filePath.IndexOf("/", StringComparison.Ordinal);
    if (indexOfLastSeparator == -1) return filePath;
    
    StringBuilder sb = new(filePath);
    sb.Remove(0, indexOfLastSeparator + 1);

    return TransformModuleFileNameForEventNameConcatenation(sb.ToString());
  }

  public static string ConcatenateMethodDetails(string methodName, string methodNamespace, string signature)
  {
    return string.Intern(methodNamespace + methodName);
  }
}