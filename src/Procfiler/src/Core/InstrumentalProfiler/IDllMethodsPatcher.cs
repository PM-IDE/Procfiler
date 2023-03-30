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
    try
    {
      if (instrumentationKind == InstrumentationKind.None) return;
      
      var directory = Path.GetDirectoryName(dllPath);
      Debug.Assert(directory is { });
      var cache = new SelfContainedTypeCache(myLogger, directory);
      var assembly = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters
      {
        InMemory = true,
        ReadSymbols = true,
        ReadWrite = true
      });

      var assemblyWithPath = new AssemblyDefWithPath(assembly, dllPath);
      
      switch (instrumentationKind)
      {
        case InstrumentationKind.OnlyMainAssembly:
          PatchAssemblyMethods(assemblyWithPath, cache);
          break;
        case InstrumentationKind.MainAssemblyAndAllReferences:
          PatchAssemblyMethodsWithReferences(assemblyWithPath, cache);
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(instrumentationKind), instrumentationKind, null);
      }
    }
    catch (Exception ex) when (ex is not ArgumentOutOfRangeException)
    {
      myLogger.LogError(ex, "Failed to instrument code");
    }
  }

  private void PatchAssemblyMethodsWithReferences(AssemblyDefWithPath assemblyDefWithPath, SelfContainedTypeCache cache)
  {
    var visited = new HashSet<string>();
    var queue = new Queue<AssemblyDefWithPath>();
    queue.Enqueue(assemblyDefWithPath);

    while (queue.Count > 0)
    {
      var currentAssembly = queue.Dequeue();
      if (visited.Contains(currentAssembly.Assembly.FullName)) continue;

      visited.Add(currentAssembly.Assembly.FullName);
      
      PatchAssemblyMethods(currentAssembly, cache);
      
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

  private void PatchAssemblyMethods(AssemblyDefWithPath assemblyDefWithPath, SelfContainedTypeCache cache)
  {
    var (assembly, physicalPath) = assemblyDefWithPath;
    if ((assembly.MainModule.Attributes & ModuleAttributes.ILOnly) == 0)
    {
      myLogger.LogWarning("Will not patch ");
      return;
    }
    
    try
    {
      myLogger.LogInformation("Patching assembly: {Name}", assembly.Name);
      cache.ProcessAssembly(new AssemblyDefWithPath(assembly, physicalPath), true);

      var consoleType = cache.Types[DotNetConstants.SystemConsole];
      var consoleWriteLineMethod = consoleType.Methods.FirstOrDefault(IsConsoleWriteLineMethod);

      Debug.Assert(consoleWriteLineMethod is { });
      foreach (var moduleDefinition in assembly.Modules)
      {
        var consoleWriteLineReference = moduleDefinition.ImportReference(consoleWriteLineMethod);
        foreach (var type in moduleDefinition.Types)
        {
          foreach (var method in type.Methods)
          {
            if (!method.IsManaged) continue;
            if (!method.HasBody) continue;
            if (!CanPatchMethod(method)) continue;

            var processor = method.Body.GetILProcessor();
            for (var i = method.Body.Instructions.Count - 1; i >= 0; i--)
            {
              if (ShouldInsertExitLogging(method.Body.Instructions, i))
              {
                InsertProcfilerLogBefore(i, processor, consoleWriteLineReference, false);
              }
            }

            InsertProcfilerLogBefore(0, processor, consoleWriteLineReference, true);
          }
        }
      }

      assembly.Write(physicalPath);
    }
    catch (Exception ex)
    {
      myLogger.LogError(ex, "Failed to instrument code of {Name}", assembly.Name);
    }
  }

  private static bool CanPatchMethod(MethodDefinition methodDefinition)
  {
    foreach (var instruction in methodDefinition.Body.Instructions)
    {
      if (instruction.OpCode == OpCodes.Throw || instruction.OpCode == OpCodes.Leave ||
          instruction.OpCode == OpCodes.Leave_S)
      {
        //ToDo: need proper support for exceptions
        return false;
      }
    }

    return true;
  }

  private static bool IsConsoleWriteLineMethod(MethodDefinition methodDefinition)
  {
    return methodDefinition.Name == DotNetConstants.WriteLine && 
           methodDefinition.Parameters.Count == 1 && 
           methodDefinition.Parameters[0].ParameterType.FullName == DotNetConstants.SystemString;
  }

  private bool ShouldInsertExitLogging(Collection<Instruction> instructions, int index) => 
    instructions[index].OpCode == OpCodes.Ret;

  private static void InsertProcfilerLogBefore(
    int index, ILProcessor processor, MethodReference methodReference, bool entering)
  {
    var messageStart = entering ? "Entering" : "Exiting";
    var message = $"{messageStart} {processor.Body.Method.FullName}";
    var loadStringInstruction = Instruction.Create(OpCodes.Ldstr, message);
    
    var instruction = processor.Body.Instructions[index];

    if (index == 0 || processor.Body.Instructions[index - 1].OpCode != OpCodes.Nop)
    {
      processor.InsertBefore(instruction, Instruction.Create(OpCodes.Nop));
    }
    
    processor.InsertBefore(instruction, loadStringInstruction);
    var call = Instruction.Create(OpCodes.Call, methodReference);
    processor.InsertAfter(loadStringInstruction, call);
          
    processor.InsertAfter(call, Instruction.Create(OpCodes.Nop));
  }
}