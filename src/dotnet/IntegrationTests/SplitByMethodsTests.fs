module IntegrationTests.SplitByMethodsTests

open System.IO
open NUnit.Framework
open Scripts.Core
open Scripts.Core.ProcfilerScriptsUtils
open Scripts.Core.SplitByMethods
open Util

let private createConfigInternal solutionPath outputPath : ICommandConfig =
    { Config.Base =
        { PathConfig =
            { OutputPath = outputPath
              CsprojPath = solutionPath }
          Repeat = 1
          Duration = 10_000 }

      Config.Inline = InlineMode.EventsAndMethodsEventsWithFilter
      Config.FilterPattern = ".*"
      Config.TargetMethodsRegex =  applicationNameFromCsproj solutionPath
      Config.MergeUndefinedThreadEvents = true
      Config.OnlineSerialization = true
      Config.DuringRuntimeFiltering = true }

let source () =
    seq {
        yield
            TestCaseData(
                "ConsoleApp1",
                [ "ConsoleApp1.Program.Main[void.."
                  "ConsoleApp1.Program.Method1[void.."
                  "ConsoleApp1.Program..ctor[instance.void.."
                  "ConsoleApp1.Program.Method2[void.." ]
            )

        yield TestCaseData("SystemArrayPooling", [ "SystemArrayPooling.Program.Main[void.." ])

        yield
            TestCaseData(
                "YieldEnumerator",
                [ "YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d..ctor[instance.void.."
                  "YieldEnumerator.Program.Main[void.."
                  "YieldEnumerator.Program..ctor[instance.void.."
                  "YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d.System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator[instance.class.System.Collections.Generic.IEnumerator`1<int32>.."
                  "YieldEnumerator.Program.<Main>g__EnumerateSomeNumbers|0_0[class.System.Collections.Generic.IEnumerable`1<int32>.."
                  "YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d.System.IDisposable.Dispose[instance.void.."
                  "ASYNC_YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d.xes"
                  "YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d.MoveNext[instance.bool.."
                  "YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d.System.Collections.Generic.IEnumerator<System.Int32>.get_Current[instance.int32.." ]
            )

        yield TestCaseData("UnsafeFixed", [ "UnsafeFixed.Program.Main[void.." ])

        yield
            TestCaseData(
                "TaskTestProject1",
                [ "TaskTestProject1.Program+<>c__DisplayClass0_0..ctor[instance.void.."
                  "TaskTestProject1.Program+<>c__DisplayClass0_0.<Main>b__1[instance.void.."
                  "TaskTestProject1.Program.Main[void.."
                  "TaskTestProject1.Program+<>c__DisplayClass0_0.<Main>b__0[instance.int32.." ]
            )

        yield
            TestCaseData(
                "SimpleAsyncAwait",
                [ "SimpleAsyncAwait.Program+<<Main>g__Foo|0_1>d.MoveNext[instance.void.."
                  "SimpleAsyncAwait.Program+<Main>d__0.MoveNext[instance.void.."
                  "SimpleAsyncAwait.Program+<>c.<Main>b__0_0[instance.int32.."
                  "SimpleAsyncAwait.Class1..ctor[instance.void.."
                  "ASYNC_SimpleAsyncAwait.Program+<<Main>g__Foo|0_1>d.xes"
                  "SimpleAsyncAwait.Program+<>c..cctor[void.."
                  "SimpleAsyncAwait.Class3..ctor[instance.void.."
                  "SimpleAsyncAwait.Program.<Main>g__Foo|0_1[class.System.Threading.Tasks.Task`1<int32>.."
                  "SimpleAsyncAwait.Program.<Main>[void.."
                  "SimpleAsyncAwait.Program.Main[class.System.Threading.Tasks.Task.."
                  "ASYNC_SimpleAsyncAwait.Program+<Main>d__0.xes"
                  "SimpleAsyncAwait.Program+<Main>d__0..ctor[instance.void.."
                  "SimpleAsyncAwait.Program+<>c..ctor[instance.void.."
                  "SimpleAsyncAwait.Program+<<Main>g__Foo|0_1>d..ctor[instance.void.."
                  "SimpleAsyncAwait.Class2..ctor[instance.void.." ]
            )

        yield
            TestCaseData(
                "NotSimpleAsyncAwait",
                [ "NotSimpleAsyncAwait.Program.<Main>[void.."
                  "NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0+<<Main>b__1>d.MoveNext[instance.void.."
                  "NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0+<<Main>b__1>d..ctor[instance.void.."
                  "NotSimpleAsyncAwait.Program.Main[class.System.Threading.Tasks.Task.."
                  "NotSimpleAsyncAwait.Class1..ctor[instance.void.."
                  "ASYNC_NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0+<<Main>b__1>d.xes"
                  "NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0.<Main>b__1[instance.class.System.Threading.Tasks.Task.."
                  "NotSimpleAsyncAwait.Class3..ctor[instance.void.."
                  "NotSimpleAsyncAwait.Program+<Main>d__0.MoveNext[instance.void.."
                  "NotSimpleAsyncAwait.Class4..ctor[instance.void.."
                  "NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0..ctor[instance.void.."
                  "ASYNC_NotSimpleAsyncAwait.Program+<Main>d__0.xes"
                  "NotSimpleAsyncAwait.Program.<Main>g__Allocate|0_0[class.System.Object.."
                  "NotSimpleAsyncAwait.Program+<Main>d__0..ctor[instance.void.."
                  "NotSimpleAsyncAwait.Class2..ctor[instance.void.." ]
            )

        yield
            TestCaseData(
                "NotExistingAssemblyLoading",
                [ "NotExistingAssemblyLoading.Program+<>c.<Main>b__0_0[instance.class.System.Reflection.Assembly.."
                  "NotExistingAssemblyLoading.Program+<>c..ctor[instance.void.."
                  "NotExistingAssemblyLoading.Program+<>c..cctor[void.."
                  "NotExistingAssemblyLoading.Program+<>c.<Main>b__0_1[instance.class.System.Reflection.Assembly.."
                  "NotExistingAssemblyLoading.Program.Main[void.." ]
            )

        yield TestCaseData("LOHAllocations", [ "LOHAllocations.Program.Main[void.." ])
        yield TestCaseData("IntensiveThreadPoolUse", [ "IntensiveThreadPoolUse.Program.Main[void.." ])

        yield
            TestCaseData(
                "FinalizableObject",
                [ "FinalizableObject.Program.Main[void.."
                  "FinalizableObject.ClassWithFinalizer.Finalize[instance.void.."
                  "FinalizableObject.ClassWithFinalizer..ctor[instance.void.." ]
            )

        yield
            TestCaseData(
                "FileWriteProject",
                [ "ASYNC_FileWriteProject.Program+<Main>d__0.xes"
                  "FileWriteProject.Program+<Main>d__0.MoveNext[instance.void.."
                  "FileWriteProject.Program+<Main>d__0..ctor[instance.void.."
                  "FileWriteProject.Program.Main[class.System.Threading.Tasks.Task.."
                  "FileWriteProject.Program.<Main>[void.." ]
            )

        yield
            TestCaseData(
                "FileAsyncOperations",
                [ "FileAsyncOperations.Class3..ctor[instance.void.."
                  "FileAsyncOperations.Class1..ctor[instance.void.."
                  "FileAsyncOperations.Class2..ctor[instance.void.."
                  "FileAsyncOperations.Program.Main[class.System.Threading.Tasks.Task.."
                  "FileAsyncOperations.Program.<Main>[void.."
                  "FileAsyncOperations.Program+<Main>d__0..ctor[instance.void.."
                  "FileAsyncOperations.Program+<Main>d__0.MoveNext[instance.void.."
                  "ASYNC_FileAsyncOperations.Program+<Main>d__0.xes" ]
            )

        yield
            TestCaseData(
                "ExceptionTryCatchFinallyWhen",
                [ "ExceptionTryCatchFinallyWhen.Program.<Main>g__Throw|0_0[void.."
                  "ExceptionTryCatchFinallyWhen.Program.Main[void.." ]
            )

        yield
            TestCaseData(
                "ExceptionTryCatchFinallyAsync",
                [ "ExceptionTryCatchFinallyAsync.Program+<>c..ctor[instance.void.."
                  "ExceptionTryCatchFinallyAsync.Program+<>c__DisplayClass0_0.<Main>b__3[instance.void.."
                  "ExceptionTryCatchFinallyAsync.Program+<>c__DisplayClass0_0..ctor[instance.void.."
                  "ExceptionTryCatchFinallyAsync.Program.<Main>g__X|0_1[class.System.Threading.Tasks.Task.."
                  "ExceptionTryCatchFinallyAsync.Program.<Main>g__Print|0_2[class.System.Threading.Tasks.Task.."
                  "ExceptionTryCatchFinallyAsync.Program+<Main>d__0..ctor[instance.void.."
                  "ASYNC_ExceptionTryCatchFinallyAsync.Program+<<Main>g__X|0_1>d.xes"
                  "ExceptionTryCatchFinallyAsync.Program+<Main>d__0.MoveNext[instance.void.."
                  "ExceptionTryCatchFinallyAsync.Program+<<Main>g__Print|0_2>d.MoveNext[instance.void.."
                  "ASYNC_ExceptionTryCatchFinallyAsync.Program+<Main>d__0.xes"
                  "ExceptionTryCatchFinallyAsync.Program+<>c.<Main>b__0_0[instance.class.System.Threading.Tasks.Task.."
                  "ASYNC_ExceptionTryCatchFinallyAsync.Program+<<Main>g__Print|0_2>d.xes"
                  "ExceptionTryCatchFinallyAsync.Program+<>c..cctor[void.."
                  "ExceptionTryCatchFinallyAsync.Program+<<Main>g__X|0_1>d.MoveNext[instance.void.."
                  "ExceptionTryCatchFinallyAsync.Program.Main[class.System.Threading.Tasks.Task.."
                  "ExceptionTryCatchFinallyAsync.Program+<<Main>g__Print|0_2>d..ctor[instance.void.."
                  "ExceptionTryCatchFinallyAsync.Program+<<Main>g__X|0_1>d..ctor[instance.void.."
                  "ExceptionTryCatchFinallyAsync.Program.<Main>[void.." ]
            )

        yield
            TestCaseData(
                "ExceptionTryCatchFinally",
                [ "ExceptionTryCatchFinally.Program.<Main>g__ThrowException|0_0[void.."
                  "ExceptionTryCatchFinally.Program..ctor[instance.void.."
                  "ExceptionTryCatchFinally.Program.Main[void.." ]
            )

        yield TestCaseData("DynamicAssemblyLoading", [ "DynamicAssemblyLoading.Program.Main[void.." ])
        yield TestCaseData("DynamicAssemblyCreation", [ "DynamicAssemblyCreation.Program.Main[void.." ])
    }

[<TestCaseSource("source")>]
let SplitByMethodsTest projectName (expectedMethods: List<string>) =
    let doTest tempDir =
        let path = getCsprojPathFromSource projectName
        launchProcfiler path tempDir createConfigInternal

        let fileNameProcessor (name: string) =
            let chars = name.ToLower().ToCharArray() |> Array.filter (fun c -> (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
            new string(chars)

        let files =
            Directory.GetFiles(tempDir)
            |> Array.map Path.GetFileName
            |> Array.map fileNameProcessor
            |> Set.ofArray

        let expectedFileNames = expectedMethods |> List.map fileNameProcessor |> Set.ofList
        Assert.That(Set.isSubset expectedFileNames files, Is.True)

    executeTestWithTempFolder doTest
