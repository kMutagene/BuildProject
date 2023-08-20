﻿module BasicTasks

open BlackFox.Fake
open Fake.IO
open Fake.DotNet
open Fake.IO.Globbing.Operators

open ProjectInfo

let setPrereleaseTag = BuildTask.create "SetPrereleaseTag" [] {
    printfn "Please enter pre-release package suffix"
    let suffix = System.Console.ReadLine()
    prereleaseSuffix <- suffix
    prereleaseTag <- (sprintf "%s-%s" release.NugetVersion suffix)
    isPrerelease <- true
}

let clean = BuildTask.create "Clean" [] {
    !! "src/**/bin"
    ++ "src/**/obj"
    ++ "tests/**/bin"
    ++ "tests/**/obj"
    ++ "pkg"
    |> Shell.cleanDirs 
}

#if (individuaPackageVersions)

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
                }
            {
                p with 
                    MSBuildParams = msBuildParams
            }
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
                }
            {
                p with 
                    MSBuildParams = msBuildParams
            }
            // Use this if you want to speed up your build. Especially helpful in large projects
            // Ensure that the order in your project list is correct (e.g. projects that are depended on are built first)
            //|> DotNet.Options.withCustomParams (Some "--no-dependencies") 
        )
    )
}

#else

let build = BuildTask.create "Build" [clean] {
    solutionFile
    |> DotNet.build id
}

#endif
