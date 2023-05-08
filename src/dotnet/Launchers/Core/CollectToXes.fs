namespace Scripts.Core

open Scripts.Core.ProcfilerScriptsUtils

module CollectToXes =
    type Config = {
        Base: ConfigBase
    }
    
    let private createArgumentsList config = [
        "collect-to-xes"
        $" -csproj {config.Base.CsprojPath}"
        $" -o {config.Base.OutputPath}"
        $" --repeat {config.Base.Repeat}"
        $" --duration {config.Base.Duration}"
    ]
    
    let private createConfig csprojPath outputPath = {
        Base = createDefaultConfigBase csprojPath outputPath
    }

    let launchProcfilerCustomConfig csprojPath outputPath createConfig =
        launchProcfiler csprojPath outputPath createConfig createArgumentsList
        
    let launchProcfiler csprojPath outputPath =
        launchProcfilerCustomConfig csprojPath outputPath