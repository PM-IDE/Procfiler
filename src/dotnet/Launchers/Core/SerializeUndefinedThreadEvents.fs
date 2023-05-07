namespace Scripts.Core

module SerializeUndefinedThreadEvents =
    type Config = {
        OutputPath: string
        CsprojPath: string
        Repeat: int
        Duration: int
    }
    
    let private createArgumentsList config = [
        "undefined-events-to-xes"
        $" -csproj {config.CsprojPath}"
        $" -o {config.OutputPath}"
        $" --repeat {config.Repeat}"
        $" --duration {config.Duration}"
    ]
    
    let private createConfig csprojPath outputPath = {
        OutputPath = outputPath
        CsprojPath = csprojPath
        Repeat = 50
        Duration = 10_000
    }
    
    let launchProcfilerCustomConfig csprojPath outputPath createCustomConfig =
        ProcfilerScriptsUtils.launchProcfiler csprojPath outputPath createCustomConfig createArgumentsList
        
    let launchProcfiler csprojPath outputPath =
        launchProcfilerCustomConfig csprojPath outputPath createConfig
        
    let launchProcfilerOnFolderOfSolutions pathToFolderWithSolutions outputPath =
        let pathsToCsprojFiles = ProcfilerScriptsUtils.getAllCsprojFiles pathToFolderWithSolutions
        pathsToCsprojFiles |> List.iter (fun csprojPath ->
            let patchedOutputPath = ProcfilerScriptsUtils.createOutputDirectoryForSolution csprojPath outputPath
            launchProcfiler csprojPath patchedOutputPath)