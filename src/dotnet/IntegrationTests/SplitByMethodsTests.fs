module IntegrationTests.SplitByMethodsTests

open System.IO
open NUnit.Framework
open Scripts.Core
open Util

let private createConfigInternal solutionPath outputPath: SplitByMethods.Config = {
    OutputPath = outputPath
    CsprojPath = solutionPath
    Inline = true
    Repeat = 1
    Duration = 10_000
    FilterPattern = ProcfilerScriptsUtils.applicationNameFromCsproj solutionPath
    MergeUndefinedThreadEvents = true
}

let source () =
    seq {
        yield TestCaseData("ConsoleApp1", [ "consoleapp1Program.Main.xes";
                                            "consoleapp1Program.Method1.xes";
                                            "consoleapp1Program.Method2.xes" ])
        yield TestCaseData("SystemArrayPooling", [ "systemarraypoolingProgram.Main$.xes" ])
    }

[<TestCaseSource("source")>]
let SplitByMethodsTest projectName (expectedMethods: List<string>) =
    let doTest tempDir =
        let path = getCsprojPathFromSource projectName
        SplitByMethods.launchProcfiler path tempDir createConfigInternal
        
        let expectedMethodsSet = expectedMethods |> Set.ofList
        let files = Directory.GetFiles(tempDir) |> Array.map Path.GetFileName |> Set.ofArray
        
        Assert.That(Set.isSubset expectedMethodsSet files, Is.True)
        
    executeTestWithTempFolder doTest
    