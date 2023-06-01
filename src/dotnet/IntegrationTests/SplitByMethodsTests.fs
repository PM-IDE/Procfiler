module IntegrationTests.SplitByMethodsTests

open System.IO
open NUnit.Framework
open Scripts.Core
open Scripts.Core.ProcfilerScriptsUtils
open Scripts.Core.SplitByMethods
open Util

let private createConfigInternal solutionPath outputPath: ICommandConfig = {
    Config.Base = {
        PathConfig = {
            OutputPath = outputPath
            CsprojPath = solutionPath
        }
        Repeat = 1
        Duration = 10_000
    }

    Config.Inline = InlineMode.EventsAndMethodsEventsWithFilter
    Config.FilterPattern = applicationNameFromCsproj solutionPath
    Config.MergeUndefinedThreadEvents = true
}

let source () =
    seq {
        yield TestCaseData("ConsoleApp1", [ "ConsoleApp1.Program..ctor";
                                            "ConsoleApp1.Program.Main";
                                            "ConsoleApp1.Program.Method1";
                                            "ConsoleApp1.Program.Method2" ])
        yield TestCaseData("SystemArrayPooling", [ "SystemArrayPooling.Program.Main" ])
        yield TestCaseData("YieldEnumerator", [ "YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d..ctor";
                                                "YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d.MoveNext";
                                                "YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d.System.Collections.Generic.IEnumerable<System.Int32>.GetEnumerator";
                                                "YieldEnumerator.Program+<<Main>g__EnumerateSomeNumbers|0_0>d.System.IDisposable.Dispose";
                                                "YieldEnumerator.Program..ctor";
                                                "YieldEnumerator.Program.<Main>g__EnumerateSomeNumbers|0_0";
                                                "YieldEnumerator.Program.Main" ])
        yield TestCaseData("UnsafeFixed", [ "UnsafeFixed.Program.Main" ])
        yield TestCaseData("TaskTestProject1", [ "TaskTestProject1.Program+<>c__DisplayClass0_0..ctor";
                                                 "TaskTestProject1.Program+<>c__DisplayClass0_0.<Main>b__0";
                                                 "TaskTestProject1.Program+<>c__DisplayClass0_0.<Main>b__1";
                                                 "TaskTestProject1.Program.Main" ])
        yield TestCaseData("SimpleAsyncAwait", [ "ASYNC_SimpleAsyncAwait.Program+<<Main>g__Foo|0_1>d";
                                                 "ASYNC_SimpleAsyncAwait.Program+<Main>d__0";
                                                 "SimpleAsyncAwait.Class1..ctor";
                                                 "SimpleAsyncAwait.Class2..ctor";
                                                 "SimpleAsyncAwait.Class3..ctor";
                                                 "SimpleAsyncAwait.Program+<<Main>g__Foo|0_1>d..ctor";
                                                 "SimpleAsyncAwait.Program+<<Main>g__Foo|0_1>d.MoveNext";
                                                 "SimpleAsyncAwait.Program+<>c..cctor";
                                                 "SimpleAsyncAwait.Program+<>c..ctor";
                                                 "SimpleAsyncAwait.Program+<>c.<Main>b__0_0";
                                                 "SimpleAsyncAwait.Program+<Main>d__0..ctor";
                                                 "SimpleAsyncAwait.Program+<Main>d__0.MoveNext";
                                                 "SimpleAsyncAwait.Program.<Main>";
                                                 "SimpleAsyncAwait.Program.<Main>g__Foo|0_1";
                                                 "SimpleAsyncAwait.Program.Main" ])
        yield TestCaseData("NotSimpleAsyncAwait", [ "ASYNC_NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0+<<Main>b__1>d";
                                                    "ASYNC_NotSimpleAsyncAwait.Program+<Main>d__0";
                                                    "NotSimpleAsyncAwait.Class1..ctor";
                                                    "NotSimpleAsyncAwait.Class2..ctor";
                                                    "NotSimpleAsyncAwait.Class3..ctor";
                                                    "NotSimpleAsyncAwait.Class4..ctor";
                                                    "NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0+<<Main>b__1>d..ctor";
                                                    "NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0+<<Main>b__1>d.MoveNext";
                                                    "NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0..ctor";
                                                    "NotSimpleAsyncAwait.Program+<>c__DisplayClass0_0.<Main>b__1";
                                                    "NotSimpleAsyncAwait.Program+<Main>d__0..ctor";
                                                    "NotSimpleAsyncAwait.Program+<Main>d__0.MoveNext";
                                                    "NotSimpleAsyncAwait.Program.<Main>";
                                                    "NotSimpleAsyncAwait.Program.<Main>g__Allocate|0_0";
                                                    "NotSimpleAsyncAwait.Program.Main" ])
        yield TestCaseData("NotExistingAssemblyLoading", [ "NotExistingAssemblyLoading.Program+<>c..cctor";
                                                           "NotExistingAssemblyLoading.Program+<>c..ctor";
                                                           "NotExistingAssemblyLoading.Program+<>c.<Main>b__0_0";
                                                           "NotExistingAssemblyLoading.Program+<>c.<Main>b__0_1";
                                                           "NotExistingAssemblyLoading.Program.Main" ])
        yield TestCaseData("LOHAllocations", [ "LOHAllocations.Program.Main" ])
        yield TestCaseData("IntensiveThreadPoolUse", [ "IntensiveThreadPoolUse.Program.Main" ])
        yield TestCaseData("FinalizableObject", [ "FinalizableObject.ClassWithFinalizer..ctor";
                                                  "FinalizableObject.ClassWithFinalizer.Finalize";
                                                  "FinalizableObject.Program.Main" ])
        yield TestCaseData("FileWriteProject", [ "ASYNC_FileWriteProject.Program+<Main>d__0";
                                                 "FileWriteProject.Program+<Main>d__0..ctor";
                                                 "FileWriteProject.Program+<Main>d__0.MoveNext";
                                                 "FileWriteProject.Program.<Main>";
                                                 "FileWriteProject.Program.Main" ])
        yield TestCaseData("FileAsyncOperations", [ "ASYNC_FileAsyncOperations.Program+<Main>d__0";
                                                    "FileAsyncOperations.Class1..ctor";
                                                    "FileAsyncOperations.Class2..ctor";
                                                    "FileAsyncOperations.Class3..ctor";
                                                    "FileAsyncOperations.Program+<Main>d__0..ctor";
                                                    "FileAsyncOperations.Program+<Main>d__0.MoveNext";
                                                    "FileAsyncOperations.Program.<Main>";
                                                    "FileAsyncOperations.Program.Main" ])
        yield TestCaseData("ExceptionTryCatchFinallyWhen", [ "ExceptionTryCatchFinallyWhen.Program.<Main>g__Throw|0_0";
                                                             "ExceptionTryCatchFinallyWhen.Program.Main" ])
        yield TestCaseData("ExceptionTryCatchFinallyAsync", [ "ASYNC_ExceptionTryCatchFinallyAsync.Program+<<Main>g__Print|0_2>d"
                                                              "ASYNC_ExceptionTryCatchFinallyAsync.Program+<<Main>g__X|0_1>d"
                                                              "ASYNC_ExceptionTryCatchFinallyAsync.Program+<Main>d__0"
                                                              "ExceptionTryCatchFinallyAsync.Program+<<Main>g__Print|0_2>d..ctor"
                                                              "ExceptionTryCatchFinallyAsync.Program+<<Main>g__Print|0_2>d.MoveNext"
                                                              "ExceptionTryCatchFinallyAsync.Program+<<Main>g__X|0_1>d..ctor"
                                                              "ExceptionTryCatchFinallyAsync.Program+<<Main>g__X|0_1>d.MoveNext"
                                                              "ExceptionTryCatchFinallyAsync.Program+<>c..cctor"
                                                              "ExceptionTryCatchFinallyAsync.Program+<>c..ctor"
                                                              "ExceptionTryCatchFinallyAsync.Program+<>c.<Main>b__0_0"
                                                              "ExceptionTryCatchFinallyAsync.Program+<>c__DisplayClass0_0..ctor"
                                                              "ExceptionTryCatchFinallyAsync.Program+<>c__DisplayClass0_0.<Main>b__3"
                                                              "ExceptionTryCatchFinallyAsync.Program+<Main>d__0..ctor"
                                                              "ExceptionTryCatchFinallyAsync.Program+<Main>d__0.MoveNext"
                                                              "ExceptionTryCatchFinallyAsync.Program.<Main>"
                                                              "ExceptionTryCatchFinallyAsync.Program.<Main>g__Print|0_2"
                                                              "ExceptionTryCatchFinallyAsync.Program.<Main>g__X|0_1"
                                                              "ExceptionTryCatchFinallyAsync.Program.Main" ])
        yield TestCaseData("ExceptionTryCatchFinally", [ "ExceptionTryCatchFinally.Program..ctor"
                                                         "ExceptionTryCatchFinally.Program.<Main>g__ThrowException|0_0"
                                                         "ExceptionTryCatchFinally.Program.Main" ])
        yield TestCaseData("DynamicAssemblyLoading", [ "DynamicAssemblyLoading.Program.Main" ])
        yield TestCaseData("DynamicAssemblyCreation", [ "DynamicAssemblyCreation.Program.Main" ])
    }

[<TestCaseSource("source")>]
let SplitByMethodsTest projectName (expectedMethods: List<string>) =
    let doTest tempDir =
        let path = getCsprojPathFromSource projectName
        launchProcfiler path tempDir createConfigInternal
        
        let fileNameProcessor (name: string) =
             let chars = name.ToLower().ToCharArray() |> Array.filter (fun c -> c >= 'a' && c <= 'z')
             string(chars)
        
        let files = Directory.GetFiles(tempDir) |> Array.map Path.GetFileName |> Array.map fileNameProcessor |>
                    Set.ofArray
        
        let expectedFileNames = expectedMethods |> List.map fileNameProcessor |> Set.ofList
        Assert.That(Set.isSubset expectedFileNames files, Is.True)
        
    executeTestWithTempFolder doTest