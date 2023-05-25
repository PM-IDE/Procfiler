namespace Scripts.Core

open System.Text
open System
open System.Diagnostics
open System.IO


module ProcfilerScriptsUtils =
    let net7 = "net7.0"
    let net6 = "net6.0"
    
    type PathConfigBase =
        { CsprojPath: string
          OutputPath: string }
        
        member this.AddArguments list =
            list @ [ $" -csproj {this.CsprojPath}"; $" -o {this.OutputPath}" ]
    
    type ConfigBase =
        { PathConfig: PathConfigBase
          Duration: int
          Repeat: int }
        
        member this.AddArguments list =
            let toAdd = [ $" --repeat {this.Repeat}"; $" --duration {this.Duration}" ]
            this.PathConfig.AddArguments list @ toAdd
    
    type ICommandConfig =
        abstract member CreateArguments : unit -> string list
    
    let createDefaultConfigBase csprojPath outputPath = {
        PathConfig = {
            CsprojPath = csprojPath
            OutputPath = outputPath
        }
        
        Duration = 10_000
        Repeat = 50 
    }

    let private createProcess fileName args workingDirectory =
        let startInfo = ProcessStartInfo(fileName, args)
        startInfo.WorkingDirectory <- workingDirectory
        new Process(StartInfo=startInfo)
        
    let buildProjectFromSolution solutionDirectory projectName =
        let args = $"msbuild /t:{projectName} /p:Configuration=\"Release\""
        let buildProcess = createProcess "dotnet" args solutionDirectory
        
        match buildProcess.Start() with
        | false ->
            printfn $"Build process for solution {solutionDirectory} failed to start"
        | true ->
            buildProcess.WaitForExit()
            match buildProcess.ExitCode with
            | 0 -> printfn $"Successfully built {solutionDirectory}"
            | _ ->
                printfn $"Error happened when building solution {solutionDirectory}:"
                
    let buildProcfiler =
        let parentDirectory = Directory.GetCurrentDirectory() |> Directory.GetParent
        let dir = parentDirectory.Parent.Parent.Parent.Parent.FullName
        let dotnetSourcePath = Path.Combine(dir, "dotnet")
        
        let framework = net7
        printfn "Started building ProcfilerBuildTasks"
        buildProjectFromSolution dotnetSourcePath "ProcfilerBuildTasks"
        
        printfn "Started building whole Procfiler solution"
        buildProjectFromSolution dotnetSourcePath "Procfiler"
        Path.Combine(dotnetSourcePath, "Procfiler", "bin", "Release", framework, "Procfiler.dll")
        
    let getAllCsprojFiles solutionsDirectory =
        Directory.GetDirectories(solutionsDirectory) |>
        List.ofArray |>
        List.map (fun dir -> Path.Combine(dir, Path.GetFileName(dir) + ".csproj"))
        
    let ensureEmptyDirectory path =
        match Directory.Exists path with
        | true ->
            Directory.Delete(path, true)
            Directory.CreateDirectory path
        | false ->
            Directory.CreateDirectory path
            
    let applicationNameFromCsproj (dllPath: string) =
        let csprojName = Path.GetFileName(dllPath)
        csprojName.AsSpan().Slice(0, csprojName.IndexOf('.')).ToString()
        
    let getAllSolutionsFrom directory =
        directory |> Directory.GetDirectories |> List.ofArray
    
    let createOutputDirectoryForSolution csprojPath outputFolder =
        let appName = applicationNameFromCsproj csprojPath
        let outputPathForSolution = Path.Combine(outputFolder, appName)
        ensureEmptyDirectory outputPathForSolution |> ignore
        outputPathForSolution
    
    let createArgumentsString solutionPath outputFolder (createConfigFunc: string -> string -> ICommandConfig) =
        let config = createConfigFunc solutionPath outputFolder
        let sb = StringBuilder()
        config.CreateArguments() |> List.iter (fun (arg: string) -> sb.Append arg |> ignore)
        sb.ToString()
        
    let launchProcfiler csprojPath outputFolder createConfig =
        let args = createArgumentsString csprojPath outputFolder createConfig
        let workingDirectory = Path.GetDirectoryName csprojPath
        let procfilerProcess = createProcess "dotnet" $"{buildProcfiler} {args}" workingDirectory
        
        match procfilerProcess.Start() with
        | true ->
            let appName = applicationNameFromCsproj csprojPath
            printfn $"Started procfiler for {appName}"
        | false -> printfn "Failed to start procfiler"
        
        procfilerProcess.WaitForExit()
        
        match procfilerProcess.ExitCode with
        | 0 ->
            let appName = applicationNameFromCsproj csprojPath
            printfn $"Finished executing procfiler for {appName}"
        | _ -> ()