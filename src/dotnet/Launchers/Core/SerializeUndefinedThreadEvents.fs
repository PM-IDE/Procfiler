namespace Scripts.Core

open Scripts.Core.ProcfilerScriptsUtils

module SerializeUndefinedThreadEvents =
    type Config = {
        Base: ConfigBase
    }
    
    let private createArgumentsList config = [
        "undefined-events-to-xes"
        $" -csproj {config.Base.CsprojPath}"
        $" -o {config.Base.OutputPath}"
        $" --repeat {config.Base.Repeat}"
        $" --duration {config.Base.Duration}"
    ]
    
    let private createConfig csprojPath outputPath = {
        Base = createDefaultConfigBase csprojPath outputPath
    }
    
    let launchProcfilerCustomConfig csprojPath outputPath createCustomConfig =
        ensureEmptyDirectory outputPath |> ignore
        launchProcfiler csprojPath outputPath createCustomConfig createArgumentsList
        
    let launchProcfiler csprojPath outputPath =
        launchProcfilerCustomConfig csprojPath outputPath createConfig
        
    let launchProcfilerOnFolderOfSolutions pathToFolderWithSolutions outputPath =
        let pathsToCsprojFiles = getAllCsprojFiles pathToFolderWithSolutions
        pathsToCsprojFiles |> List.iter (fun csprojPath ->
            let patchedOutputPath = createOutputDirectoryForSolution csprojPath outputPath
            launchProcfiler csprojPath patchedOutputPath)