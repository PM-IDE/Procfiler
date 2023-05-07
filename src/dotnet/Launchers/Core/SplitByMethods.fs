namespace Scripts.Core

open System.IO
open Microsoft.FSharp.Core

module SplitByMethods =
    type private Config = {
        OutputPath: string
        CsprojPath: string
        Inline: bool
        FilterPattern: string
        Repeat: int
        Duration: int
        MergeUndefinedThreadEvents: bool
    }
        
    let private createArgumentsList config = [
        "split-by-methods"
        $" -csproj {config.CsprojPath}"
        $" -o {config.OutputPath}"
        $" --inline {config.Inline}"
        $" --filter {config.FilterPattern}"
        $" --repeat {config.Repeat}"
        $" --duration {config.Duration}"
        $" --merge-undefined-events {config.MergeUndefinedThreadEvents}"
    ]

    let private createConfigInternal solutionPath outputPath doInline merge = {
        OutputPath = outputPath
        CsprojPath = solutionPath
        Inline = doInline
        Repeat = 50
        Duration = 10_000
        FilterPattern = ProcfilerScriptsUtils.applicationNameFromCsproj solutionPath
        MergeUndefinedThreadEvents = merge
    }
    
    let private createInlineMerge solutionPath outputPath =
        createConfigInternal solutionPath outputPath true true
        
    let private createNoInlineMerge solutionPath outputPath =
        createConfigInternal solutionPath outputPath false true
        
    let private createInlineNoMerge solutionPath outputPath =
        createConfigInternal solutionPath outputPath true false
        
    let private createNoInlineNoMerge solutionPath outputPath =
        createConfigInternal solutionPath outputPath false false
        
    let private allConfigs = [
        ("inline_merge", createInlineMerge)
        ("no_inline_merge", createNoInlineMerge) 
        ("no_inline_no_merge", createNoInlineNoMerge) 
        ("inline_no_merge", createInlineNoMerge)
    ]
        
    let private launchProcfiler dllPath outputPath configFunc =
        ProcfilerScriptsUtils.launchProcfiler dllPath outputPath configFunc createArgumentsList 
    
    let launchProcfilerOnFolderOfSolutions solutionsFolder outputPath =
        let pathsToDlls = ProcfilerScriptsUtils.getAllCsprojFiles solutionsFolder
        pathsToDlls |> List.iter (fun solution -> launchProcfiler solution outputPath createInlineMerge)
    
    let launchProcfilerOnSolutionsFolderInAllConfigs solutionsFolder outputPath =
        let pathsToDlls = ProcfilerScriptsUtils.getAllCsprojFiles solutionsFolder
        allConfigs |>
        List.iter (fun configInfo ->
            match configInfo with
            | configName, configFunc ->
                let outputPathForConfig = Path.Combine(outputPath, configName)
                ProcfilerScriptsUtils.ensureEmptyDirectory outputPathForConfig |> ignore
                pathsToDlls |> List.iter (fun solution -> launchProcfiler solution outputPathForConfig configFunc)
        )
