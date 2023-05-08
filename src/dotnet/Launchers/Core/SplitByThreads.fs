namespace Scripts.Core

open Scripts.Core.ProcfilerScriptsUtils

module SplitByThreads =
    type Config = {
        PathConfig: PathConfigBase
    }
    
    let private createArgumentsList config = [
        "split-by-threads"
        $" -csproj {config.PathConfig.CsprojPath}"
        $" -o {config.PathConfig.OutputPath}"
    ]
    
    let private createConfig csprojPath outputPath = {
        PathConfig = {
            CsprojPath = csprojPath
            OutputPath = outputPath 
        }
    }

    let launchProcfilerCustomConfig csprojPath outputPath createConfig =
        launchProcfiler csprojPath outputPath createConfig createArgumentsList
        
    let launchProcfiler csprojPath outputPath =
        launchProcfilerCustomConfig csprojPath outputPath