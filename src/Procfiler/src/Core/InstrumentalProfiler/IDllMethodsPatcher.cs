using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Procfiler.Core.Constants;
using Procfiler.Utils;
using Procfiler.Utils.Container;

namespace Procfiler.Core.InstrumentalProfiler;

public enum InstrumentationKind
{
  None,
  OnlyMainAssembly,
  MainAssemblyAndAllReferences
}

public interface IDllMethodsPatcher
{
  void PatchMethodStartEnd(string dllPath, InstrumentationKind instrumentationKind);
}

[AppComponent]
public class DllMethodsPatcher : IDllMethodsPatcher
{
  private readonly IProcfilerLogger myLogger;

  
  public DllMethodsPatcher(IProcfilerLogger logger)
  {
    myLogger = logger;
  }

  
  public void PatchMethodStartEnd(string dllPath, InstrumentationKind instrumentationKind)
  {
    if (instrumentationKind == InstrumentationKind.None) return;
    
    try
    {
      var directory = Path.GetDirectoryName(dllPath);
      Debug.Assert(directory is { });
      
      var cache = new SelfContainedTypeCache(myLogger, directory);
      cache.Initialize();
      
      var assembly = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters
      {
        InMemory = true,
        ReadSymbols = true,
        ReadWrite = true
      });

      var assemblyWithPath = new AssemblyDefWithPath(assembly, dllPath);
      var patchedAssemblies = new List<AssemblyDefWithPath>();
      switch (instrumentationKind)
      {
        case InstrumentationKind.OnlyMainAssembly:
          PatchAssemblyMethods(assemblyWithPath, cache, patchedAssemblies);
          break;
        case InstrumentationKind.MainAssemblyAndAllReferences:
          PatchAssemblyMethodsWithReferences(assemblyWithPath, cache, patchedAssemblies);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(instrumentationKind), instrumentationKind, null);
      }

      foreach (var (patchedAssembly, physicalPath) in patchedAssemblies)
      {
        patchedAssembly.Write(physicalPath);
      }
    }
    catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
    {
      myLogger.LogError(ex, "Failed to instrument code of {DllPath} or one of it's dependencies", dllPath);
    }
  }

  private void PatchAssemblyMethodsWithReferences(
    AssemblyDefWithPath assemblyDefWithPath, 
    SelfContainedTypeCache cache,
    List<AssemblyDefWithPath> patchedAssemblies)
  {
    var visited = new HashSet<string>();
    var queue = new Queue<AssemblyDefWithPath>();
    queue.Enqueue(assemblyDefWithPath);

    while (queue.Count > 0)
    {
      var currentAssembly = queue.Dequeue();
      if (visited.Contains(currentAssembly.Assembly.FullName)) continue;

      visited.Add(currentAssembly.Assembly.FullName);
      
      PatchAssemblyMethods(currentAssembly, cache, patchedAssemblies);
      
      foreach (var module in currentAssembly.Assembly.Modules)
      {
        foreach (var reference in module.AssemblyReferences)
        {
          if (cache.Assemblies.TryGetValue(reference.FullName, out var referenceAssembly))
          {
            queue.Enqueue(referenceAssembly);
          }
        }
      }
    }
  }

  private void PatchAssemblyMethods(
    AssemblyDefWithPath assemblyDefWithPath,
    SelfContainedTypeCache cache,
    List<AssemblyDefWithPath> patchedAssemblies)
  {
    var (assembly, _) = assemblyDefWithPath;
    if ((assembly.MainModule.Attributes & ModuleAttributes.ILOnly) == 0)
    {
      myLogger.LogWarning("Will not patch ");
      return;
    }
    
    try
    {
      myLogger.LogInformation("Patching assembly: {Name}", assembly.Name);
      var loggerType = cache.Types[InstrumentalProfilerConstants.MethodStartEndEventSourceType];
      
      var methodStartedLogger = loggerType.Methods.FirstOrDefault(IsLogMethodStartedMethod);
      var methodFinishedLogger = loggerType.Methods.FirstOrDefault(IsLogMethodFinishedMethod);

      Debug.Assert(methodStartedLogger is { });
      Debug.Assert(methodFinishedLogger is { });
      
      foreach (var moduleDefinition in assembly.Modules)
      {
        var methodStartLogReference = moduleDefinition.ImportReference(methodStartedLogger);
        var methodFinishedLogReference = moduleDefinition.ImportReference(methodFinishedLogger);
        
        foreach (var type in moduleDefinition.Types)
        {
          foreach (var method in type.Methods)
          {
            PatchMethodIfNeeded(method, methodStartLogReference, methodFinishedLogReference);
          }
        }
      }

      patchedAssemblies.Add(assemblyDefWithPath);
    }
    catch (Exception ex)
    {
      myLogger.LogError(ex, "Failed to instrument code of {Name}", assembly.Name);
    }
  }

  private void PatchMethodIfNeeded(
    MethodDefinition method, MethodReference methodStartedLogReference, MethodReference methodFinishedLogReference)
  {
    if (!method.IsManaged) return;
    if (!method.HasBody) return;

    PatchMethod(method, methodStartedLogReference, methodFinishedLogReference);
  }

  private void PatchMethod(
    MethodDefinition method, MethodReference methodStartedLogReference, MethodReference methodFinishedLogReference)
  {
    var processor = method.Body.GetILProcessor();
    var instructions = method.Body.Instructions;

    for (var i = instructions.Count - 1; i >= 0; i--)
    {
      if (ShouldInsertExitLogging(instructions, i))
      {
        InsertProcfilerLogBefore(i, processor, methodFinishedLogReference, false);
      }
    }

    InsertProcfilerLogBefore(0, processor, methodStartedLogReference, true);

    Instruction GetStartOfInsertedInstructions(Instruction context)
    {
      return context.Previous.Previous.Previous.Previous;
    }
    
    foreach (var instruction in instructions)
    {
      if (instruction.Operand is Instruction target && ShouldInsertExitLogging(target))
      {
        instruction.Operand = GetStartOfInsertedInstructions(target);
      }
    }
    
    foreach (var handler in method.Body.ExceptionHandlers)
    {
      if (ShouldInsertExitLogging(handler.HandlerEnd))
      {
        handler.HandlerEnd = GetStartOfInsertedInstructions(handler.HandlerEnd);
      }

      if (ShouldInsertExitLogging(handler.TryEnd))
      {
        handler.TryEnd = GetStartOfInsertedInstructions(handler.TryEnd);
      }
    }
  }

  private static bool IsLogMethodStartedMethod(MethodDefinition methodDefinition) =>
    methodDefinition.Name == InstrumentalProfilerConstants.LogMethodStartedMethodName &&
    IsMethodWithOneStringParameter(methodDefinition);

  private static bool IsLogMethodFinishedMethod(MethodDefinition methodDefinition) =>
    methodDefinition.Name == InstrumentalProfilerConstants.LogMethodStartedMethodName &&
    IsMethodWithOneStringParameter(methodDefinition);

  private static bool IsMethodWithOneStringParameter(MethodDefinition methodDefinition) =>
    methodDefinition.Parameters.Count == 1 &&
    methodDefinition.Parameters[0].ParameterType.FullName == DotNetConstants.SystemString;

  private static bool IsConsoleWriteLineMethod(MethodDefinition methodDefinition) =>
    methodDefinition.Name == DotNetConstants.WriteLine && 
    IsMethodWithOneStringParameter(methodDefinition);
  
  private static bool ShouldInsertExitLogging(Instruction instruction) =>
    instruction.OpCode == OpCodes.Ret;
  
  private static bool ShouldInsertExitLogging(Collection<Instruction> instructions, int index) => 
    ShouldInsertExitLogging(instructions[index]);

  private static void InsertProcfilerLogBefore(
    int index, ILProcessor processor, MethodReference methodReference, bool entering)
  {
    var messageStart = entering ? "Entering" : "Exiting";
    var message = $"{messageStart} {processor.Body.Method.FullName}";
    
    var instruction = processor.Body.Instructions[index];
    if (index == 0 || processor.Body.Instructions[index - 1].OpCode != OpCodes.Nop)
    {
      processor.InsertBefore(instruction, Instruction.Create(OpCodes.Nop));
    }

    var loadStringInstruction = Instruction.Create(OpCodes.Ldstr, message);

    processor.InsertBefore(instruction, loadStringInstruction);
    var call = Instruction.Create(OpCodes.Call, methodReference);
    processor.InsertAfter(loadStringInstruction, call);
          
    processor.InsertAfter(call, Instruction.Create(OpCodes.Nop));
  }
}