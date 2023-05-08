module IntegrationTests.CollectToXesTests

open System.IO
open NUnit.Framework
open Scripts.Core
open Util

let private createConfigInternal solutionPath outputPath: CollectToXes.Config = {
    Base = {
        OutputPath = outputPath
        CsprojPath = solutionPath
        Repeat = 1
        Duration = 10_000
    }
}

let source () = knownProjectsNamesTestCaseSource

[<TestCaseSource("source")>]
let CollectToXesTest projectName =
    let doTest tempDir =
        let csprojPath = getCsprojPathFromSource projectName
        let outputFileName = "data"
        let outputFilePath = Path.Combine(tempDir, $"{outputFileName}.xes")
        CollectToXes.launchProcfilerCustomConfig csprojPath outputFilePath createConfigInternal
        
        doAssertionsForOneFile tempDir outputFileName
    
    executeTestWithTempFolder doTest