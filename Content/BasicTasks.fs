﻿module BasicTasks

open BlackFox.Fake
open Fake.IO
open Fake.DotNet
open Fake.IO.Globbing.Operators

open ProjectInfo

let clean = BuildTask.create "Clean" [] {
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "tests/**/bin"
    ++ "tests/**/obj"
    ++ "pkg"
    |> Shell.cleanDirs 
}

#if (individual-package-versions)

/// Buildtask for setting a prerelease tag (also sets the mutable isPrerelease to true, and the PackagePrereleaseTag of all project infos accordingly.)
let setPrereleaseTag =
    BuildTask.create "SetPrereleaseTag" [] {
        printfn "Please enter pre-release package suffix"
        let suffix = System.Console.ReadLine()
        prereleaseSuffix <- suffix
        isPrerelease <- true
        projects
        |> List.iter (fun p ->
            p.PackagePrereleaseTag <- (sprintf "%s-%s" p.PackageVersionTag suffix)
        )
        // 
        prereleaseTag <- (sprintf "%s-%s" CoreProject.PackageVersionTag suffix)
    }

/// builds the solution file (dotnet build solution.sln)
let buildSolution =
    BuildTask.create "BuildSolution" [ clean ] { 
        solutionFile 
        |> DotNet.build (fun p ->
            let msBuildParams =
                {p.MSBuildParams with 
                    Properties = ([
                        "warnon", "3390"
                    ])
                    DisableInternalBinLog = true
                }
            {
                p with 
                    MSBuildParams = msBuildParams
                    
            }
#if( target-framework == "net8.0" )
            |> DotNet.Options.withCustomParams (Some "-tl")
#endif
        )
    }

/// builds the individual project files (dotnet build project.*proj)
///
/// The following MSBuild params are set for each project accordingly to the respective ProjectInfo:
///
/// - AssemblyVersion
///
/// - AssemblyInformationalVersion
///
/// - warnon:3390 for xml doc formatting warnings on compilation

let build = BuildTask.create "Build" [clean] {
    projects
    |> List.iter (fun pInfo ->
        let proj = pInfo.ProjFile
        proj
        |> DotNet.build (fun p ->
            let msBuildParams =
                {p.MSBuildParams with 
                    Properties = ([
                        "AssemblyVersion", pInfo.AssemblyVersion
                        "InformationalVersion", pInfo.AssemblyInformationalVersion
                        "warnon", "3390"
                    ])
                    DisableInternalBinLog = true
                }
            {
                p with 
                    MSBuildParams = msBuildParams
            }
            // Use this if you want to speed up your build. Especially helpful in large projects
            // Ensure that the order in your project list is correct (e.g. projects that are depended on are built first)
#if( target-framework == "net8.0" )
            |> DotNet.Options.withCustomParams (Some "--no-dependencies -tl")
#else
            |> DotNet.Options.withCustomParams (Some "--no-dependencies")
#endif
        )
    )
}

#else

let setPrereleaseTag = BuildTask.create "SetPrereleaseTag" [] {
    printfn "Please enter pre-release package suffix"
    let suffix = System.Console.ReadLine()
    prereleaseSuffix <- suffix
    prereleaseTag <- (sprintf "%s-%s" release.NugetVersion suffix)
    isPrerelease <- true
}

/// builds the solution file (dotnet build solution.sln)
let buildSolution =
    BuildTask.create "BuildSolution" [ clean ] { 
        solutionFile 
        |> DotNet.build (fun p ->
            { p with MSBuildParams = { p.MSBuildParams with DisableInternalBinLog = true }}
#if( target-framework == "net8.0" )
            |> DotNet.Options.withCustomParams (Some "-tl")
#endif
        )
    }

#endif