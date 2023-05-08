﻿module IntegrationTests.SplitByNamesTests

open System.IO
open NUnit.Framework
open Scripts.Core
open Scripts.Core.ProcfilerScriptsUtils
open Util

let private createConfigInternal solutionPath outputPath: ICommandConfig = {
    SplitByNames.Config.PathConfig = {
        OutputPath = outputPath
        CsprojPath = solutionPath
    }
}

let source () = knownProjectsNamesTestCaseSource

[<TestCaseSource("source")>]
let SplitByNamesTest projectName =
    let doTest tempDir =
        let csprojPath = getCsprojPathFromSource projectName
        SplitByNames.launchProcfilerCustomConfig csprojPath tempDir createConfigInternal
        
        let files = Directory.GetFiles(tempDir)
        files |> Array.iter (fun filePath -> Assert.That(FileInfo(filePath).Length, Is.GreaterThan 0))
        
        let fileNamesSet = files |> Array.map Path.GetFileNameWithoutExtension |> Set.ofSeq
        Assert.That(fileNamesSet.Count, Is.GreaterThan 20)
        
    executeTestWithTempFolder doTest