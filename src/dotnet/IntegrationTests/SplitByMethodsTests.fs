module IntegrationTests.SplitByMethodsTests

open System.IO
open NUnit.Framework
open Scripts.Core
open Scripts.Core.ProcfilerScriptsUtils
open Util

let private createConfigInternal solutionPath outputPath: ICommandConfig = {
    SplitByMethods.Config.Base = {
        PathConfig = {
            OutputPath = outputPath
            CsprojPath = solutionPath
        }
        Repeat = 1
        Duration = 10_000
    }

    SplitByMethods.Config.Inline = true
    SplitByMethods.Config.FilterPattern = applicationNameFromCsproj solutionPath
    SplitByMethods.Config.MergeUndefinedThreadEvents = true
}

let source () =
    seq {
        yield TestCaseData("ConsoleApp1", [ "consoleapp1Program.Main.xes";
                                            "consoleapp1Program.Method1.xes";
                                            "consoleapp1Program.Method2.xes" ])
        yield TestCaseData("SystemArrayPooling", [ "systemarraypoolingProgram.Main$.xes" ])
        yield TestCaseData("YieldEnumerator", [ "yieldenumeratorProgram.<<Main>$>g__EnumerateSomeNumbers|0_0.xes"
                                                "yieldenumeratorProgram.<Main>$.xes" ])
        yield TestCaseData("UnsafeFixed", [ "unsafefixedProgram.<Main>$.xes" ])
        yield TestCaseData("TaskTestProject1", [ "tasktestproject1Program+<>c__DisplayClass0_0.<<Main>$>b__1.xes"
                                                 "tasktestproject1Program.<Main>$.xes" ])
        yield TestCaseData("SimpleAsyncAwait", [ "ASYNC_Program+<<Main>$>d__0.xes"
                                                 "simpleasyncawaitProgram+<<<Main>$>g__Foo|0_1>d.MoveNext.xes"
                                                 "simpleasyncawaitProgram+<<Main>$>d__0.MoveNext.xes"
                                                 "simpleasyncawaitProgram+<>c..cctor.xes"
                                                 "simpleasyncawaitProgram.<<Main>$>g__Foo|0_1.xes"
                                                 "simpleasyncawaitProgram.<Main>.xes"
                                                 "simpleasyncawaitProgram.<Main>$.xes" ])
        yield TestCaseData("NotSimpleAsyncAwait", [ "ASYNC_Program+<<Main>$>d__0.xes"
                                                    "ASYNC_Program+<>c__DisplayClass0_0+<<<Main>$>b__1>d.xes"
                                                    "notsimpleasyncawaitProgram+<<Main>$>d__0.MoveNext.xes"
                                                    "notsimpleasyncawaitProgram+<>c__DisplayClass0_0+<<<Main>$>b__1>d.MoveNext.xes"
                                                    "notsimpleasyncawaitProgram+<>c__DisplayClass0_0.<<Main>$>b__1.xes"
                                                    "notsimpleasyncawaitProgram.<<Main>$>g__Allocate|0_0.xes"
                                                    "notsimpleasyncawaitProgram.<Main>.xes"
                                                    "notsimpleasyncawaitProgram.<Main>$.xes" ])
        yield TestCaseData("NotExistingAssemblyLoading", [ "notexistingassemblyloadingProgram+<>c..cctor.xes"
                                                           "notexistingassemblyloadingProgram+<>c.<<Main>$>b__0_1.xes"
                                                           "notexistingassemblyloadingProgram.<Main>$.xes" ])
        yield TestCaseData("LOHAllocations", [ "lohallocationsProgram.<Main>$.xes" ])
        yield TestCaseData("IntensiveThreadPoolUse", [ "intensivethreadpooluseProgram.<Main>$.xes" ])
        yield TestCaseData("FinalizableObject", [ "finalizableobjectClassWithFinalizer..ctor.xes"
                                                  "finalizableobjectClassWithFinalizer.Finalize.xes"
                                                  "finalizableobjectProgram.<Main>$.xes" ])
        yield TestCaseData("FileWriteProject", [ "ASYNC_Program+<<Main>$>d__0.xes"
                                                 "filewriteprojectProgram+<<Main>$>d__0.MoveNext.xes"
                                                 "filewriteprojectProgram.<Main>.xes"
                                                 "filewriteprojectProgram.<Main>$.xes" ])
        yield TestCaseData("FileAsyncOperations", [ "ASYNC_Program+<<Main>$>d__0.xes"
                                                    "fileasyncoperationsProgram+<<Main>$>d__0.MoveNext.xes"
                                                    "fileasyncoperationsProgram.<Main>.xes"
                                                    "fileasyncoperationsProgram.<Main>$.xes" ])
        yield TestCaseData("ExceptionTryCatchFinallyWhen",
                           [ "exceptiontrycatchfinallywhenProgram.<<Main>$>g__Throw|0_0.xes"
                             "exceptiontrycatchfinallywhenProgram.<Main>$.xes" ])
        yield TestCaseData("ExceptionTryCatchFinallyAsync",
                           [ "ASYNC_Program+<<<Main>$>g__Print|0_2>d.xes"
                             "ASYNC_Program+<<<Main>$>g__X|0_1>d.xes"
                             "ASYNC_Program+<<Main>$>d__0.xes"
                             "exceptiontrycatchfinallyasyncProgram+<<<Main>$>g__Print|0_2>d.MoveNext.xes"
                             "exceptiontrycatchfinallyasyncProgram+<<<Main>$>g__X|0_1>d.MoveNext.xes"
                             "exceptiontrycatchfinallyasyncProgram+<<Main>$>d__0.MoveNext.xes"
                             "exceptiontrycatchfinallyasyncProgram+<>c..cctor.xes"
                             "exceptiontrycatchfinallyasyncProgram+<>c.<<Main>$>b__0_0.xes"
                             "exceptiontrycatchfinallyasyncProgram+<>c__DisplayClass0_0.<<Main>$>b__3.xes"
                             "exceptiontrycatchfinallyasyncProgram.<<Main>$>g__Print|0_2.xes"
                             "exceptiontrycatchfinallyasyncProgram.<<Main>$>g__X|0_1.xes"
                             "exceptiontrycatchfinallyasyncProgram.<Main>.xes"
                             "exceptiontrycatchfinallyasyncProgram.<Main>$.xes" ])
        yield TestCaseData("ExceptionTryCatchFinally", [ "exceptiontrycatchfinallyProgram.<<Main>$>g__ThrowException|0_0.xes"
                                                         "exceptiontrycatchfinallyProgram.<Main>$.xes" ])
        yield TestCaseData("DynamicAssemblyLoading", [ "dynamicassemblyloadingProgram.<Main>$.xes" ])
        yield TestCaseData("DynamicAssemblyCreation", [ "dynamicassemblycreationProgram.<Main>$.xes" ])
    }

[<TestCaseSource("source")>]
let SplitByMethodsTest projectName (expectedMethods: List<string>) =
    let doTest tempDir =
        let path = getCsprojPathFromSource projectName
        SplitByMethods.launchProcfiler path tempDir createConfigInternal
        
        let fileNameProcessor (name: string) =
             let chars = name.ToLower().ToCharArray() |> Array.filter (fun c -> c >= 'a' && c <= 'z')
             string(chars)
        
        let files = Directory.GetFiles(tempDir) |> Array.map Path.GetFileName |> Array.map fileNameProcessor |>
                    Set.ofArray
        
        let expectedFileNames = expectedMethods |> List.map fileNameProcessor |> Set.ofList
        Assert.That(Set.isSubset expectedFileNames files, Is.True)
        
    executeTestWithTempFolder doTest