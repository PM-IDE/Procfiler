namespace Scripts.Core

module SerializeUndefinedThreadEvents =
    type private Config = {
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
    
    let private createConfig dllPath outputPath = {
        OutputPath = outputPath
        CsprojPath = dllPath
        Repeat = 50
        Duration = 10_000
    }        
    
    let private launchProcfiler dllPath outputPath =
        ProcfilerScriptsUtils.launchProcfiler dllPath outputPath createConfig createArgumentsList

    let launchProcfilerOnFolderOfSolutions pathToFolderWithSolutions outputPath =
        let pathsToCsprojFiles = ProcfilerScriptsUtils.getAllCsprojFiles pathToFolderWithSolutions
        pathsToCsprojFiles |> List.iter (fun csprojPath -> launchProcfiler csprojPath outputPath)