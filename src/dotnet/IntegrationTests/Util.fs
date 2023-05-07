module IntegrationTests.Util

open System.IO
open NUnit.Framework
open TestsUtil

let getCsprojPathFromSource solutionName =
    Path.Combine(TestPaths.CreatePathToSolutionsSource(), solutionName, $"{solutionName}.csproj")


let knownProjectsNamesTestCaseSource =
    KnownSolution.AllSolutions |> Seq.map (fun solution -> solution.Name)
    
let executeTestWithTempFolder test =
    let tempDir = Directory.CreateTempSubdirectory().FullName
    
    try
        test tempDir
    finally
        Directory.Delete(tempDir, true)