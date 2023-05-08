namespace Scripts.Core

open System.IO
open Microsoft.FSharp.Core
open Scripts.Core.ProcfilerScriptsUtils

module SplitByMethods =
    type Config =
        { Base: ConfigBase
          Inline: bool
          FilterPattern: string
          MergeUndefinedThreadEvents: bool }
        
        interface ICommandConfig with
            member this.CreateArguments () =
                let args = [ "split-by-methods" ]
                this.Base.AddArguments args @ [ $" --filter {this.FilterPattern}"
                                                $" --inline {this.Inline}"
                                                $" --merge-undefined-events {this.MergeUndefinedThreadEvents}" ]

    
    let private createConfigInternal csprojPath outputPath doInline merge = {
        Base = createDefaultConfigBase csprojPath outputPath
        Inline = doInline
        FilterPattern = applicationNameFromCsproj csprojPath
        MergeUndefinedThreadEvents = merge
    }
    
    let private createInlineMerge solutionPath outputPath: ICommandConfig =
        createConfigInternal solutionPath outputPath true true
        
    let private createNoInlineMerge solutionPath outputPath: ICommandConfig =
        createConfigInternal solutionPath outputPath false true
        
    let private createInlineNoMerge solutionPath outputPath: ICommandConfig =
        createConfigInternal solutionPath outputPath true false
        
    let private createNoInlineNoMerge solutionPath outputPath: ICommandConfig =
        createConfigInternal solutionPath outputPath false false
        
    let private allConfigs = [
        ("inline_merge", createInlineMerge)
        ("no_inline_merge", createNoInlineMerge) 
        ("no_inline_no_merge", createNoInlineNoMerge) 
        ("inline_no_merge", createInlineNoMerge)
    ]
        
    let launchProcfiler csprojPath outputPath configFunc =
        ensureEmptyDirectory outputPath |> ignore
        launchProcfiler csprojPath outputPath configFunc
    
    let launchProcfilerOnFolderOfSolutions solutionsFolder outputPath =
        ensureEmptyDirectory outputPath |> ignore
        let pathsToDlls = getAllCsprojFiles solutionsFolder
        pathsToDlls |> List.iter (fun solution -> launchProcfiler solution outputPath createInlineMerge)
    
    let launchProcfilerOnSolutionsFolderInAllConfigs solutionsFolder outputPath =
        let pathsToDlls = getAllCsprojFiles solutionsFolder
        allConfigs |>
        List.iter (fun configInfo ->
            match configInfo with
            | configName, configFunc ->
                let outputPathForConfig = Path.Combine(outputPath, configName)
                ensureEmptyDirectory outputPathForConfig |> ignore
                pathsToDlls |> List.iter (fun solution -> launchProcfiler solution outputPathForConfig configFunc)
        )
