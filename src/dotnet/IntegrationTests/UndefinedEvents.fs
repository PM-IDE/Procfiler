module IntegrationTests.UndefinedEvents

open System.IO
open NUnit.Framework
open Scripts.Core
open Util
    
let createCustomConfig csprojPath outputPath: SerializeUndefinedThreadEvents.Config = {
    OutputPath = outputPath
    CsprojPath = csprojPath
    Repeat = 1
    Duration = 10_000
}

let source () = knownProjectsNamesTestCaseSource

[<TestCaseSource("source")>]
let UndefinedEventsTest projectName =
    let doTest tempDir =
        let path = getCsprojPathFromSource projectName
        SerializeUndefinedThreadEvents.launchProcfilerCustomConfig path tempDir createCustomConfig
        
        let files = Directory.GetFiles(tempDir)
        
        Assert.That(files.Length, Is.EqualTo 1)
        Assert.That(Path.GetExtension files[0], Is.EqualTo ".xes")
        Assert.That(Path.GetFileNameWithoutExtension files[0], Is.EqualTo "UndefinedEvents")
        Assert.That(FileInfo(files[0]).Length, Is.GreaterThan(0))
        
    executeTestWithTempFolder doTest