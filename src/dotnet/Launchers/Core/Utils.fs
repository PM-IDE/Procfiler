namespace Scripts.Core

open System.Text
open System
open System.Diagnostics
open System.IO

module ProcfilerScriptsUtils =
    let net7 = "net7.0"
    let net6 = "net6.0"
    
    type ConfigBase = {
        CsprojPath: string
        OutputPath: string
        Duration: int
        Repeat: int
    }
    
    let createDefaultConfigBase csprojPath outputPath = {
        CsprojPath = csprojPath
        OutputPath = outputPath
        Duration = 10_000
        Repeat = 50 
    }

    let private createProcess fileName args =
        let startInfo = ProcessStartInfo(fileName, args)
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        new Process(StartInfo=startInfo)
        
    let buildSolutionFolder tfm solutionFolder =
        let buildProcess = createProcess "dotnet" $"build {solutionFolder} --framework {tfm} -c Release"
        match buildProcess.Start() with
        | false ->
            printfn $"Build process for solution {solutionFolder} failed to start"
        | true ->
            buildProcess.WaitForExit()
            match buildProcess.ExitCode with
            | 0 -> printfn $"Successfully built {solutionFolder}"
            | _ ->
                printfn $"Error happened when building solution {solutionFolder}:"
                buildProcess.StandardOutput.ReadToEnd() |> Console.WriteLine
                
    let buildProcfiler =
        let parentDirectory = Directory.GetCurrentDirectory() |> Directory.GetParent
        parentDirectory.FullName |> Console.WriteLine
        let dir = parentDirectory.Parent.Parent.Parent.Parent.FullName
        let procfilerSolutionPath = Path.Combine(dir, "dotnet", "Procfiler")
        
        let framework = net7
        printfn $"Started building Procfiler at {procfilerSolutionPath}"
        buildSolutionFolder framework procfilerSolutionPath
        Path.Combine(procfilerSolutionPath, "bin", "Release", framework, "Procfiler.dll")
        
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
        let csprojName = Path.GetFileName(dllPath).ToLower()
        csprojName.AsSpan().Slice(0, csprojName.IndexOf('.')).ToString()
        
    let getAllSolutionsFrom directory =
        directory |> Directory.GetDirectories |> List.ofArray
    
    let createOutputDirectoryForSolution csprojPath outputFolder =
        let appName = applicationNameFromCsproj csprojPath
        let outputPathForSolution = Path.Combine(outputFolder, appName)
        ensureEmptyDirectory outputPathForSolution |> ignore
        outputPathForSolution
    
    let createArgumentsString solutionPath outputFolder createConfigFunc createArgsFunc =
        let config = createConfigFunc solutionPath outputFolder
        let sb = StringBuilder()
        createArgsFunc config |> List.iter (fun (arg: string) -> sb.Append arg |> ignore)
        sb.ToString()
        
    let launchProcfiler csprojPath outputFolder createConfig createArgsList =
        let args = createArgumentsString csprojPath outputFolder createConfig createArgsList
        let procfilerProcess = createProcess "dotnet" $"{buildProcfiler} {args}"
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
        
        printfn $"Output: {procfilerProcess.StandardOutput.ReadToEnd()}"