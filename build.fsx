// include Fake libs
#r "./packages/build/FAKE/tools/FakeLib.dll"
#r "./packages/build/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "System.Xml.Linq.dll"


open FSharp.Data
open Fake.FileUtils
open Fake
open Fake.AssemblyInfoFile

cd "C:\Projects\mustached-nemesis-people"

printf "%s" System.Environment.CurrentDirectory

//type Nuspec = XmlProvider<"./src/People.Messages/People.Messages.nuspec">

let getProjectDir projectFilePath =
    let fileInfo = directoryInfo projectFilePath
    fileInfo.Parent.FullName

let getFile directory pattern = 
    let files = directory |> filesInDirMatching pattern
    let file = Seq.exactlyOne files
    file

let buildConfig = "Debug"
let authors = ["ConnectDevelop"]

// Directories
let artifacts = "artifacts"
let buildDir  = "./artifacts/build/"
let deployDir = "./artifacts/deploy/"

// MSBuild
MSBuildDefaults <- {
    MSBuildDefaults with
        ToolsVersion = Some "14.0"
        Verbosity = Some MSBuildVerbosity.Minimal }

// Filesets
let appReferences  =
    !! "./src/**/*.csproj"
    ++ "./src/**/*.fsproj"

// FindProjects
let projectsToPackage =
    !! "./src/**/paket.template" //*.nuspec

type Project = {
    Name: string;
    Directory: System.IO.DirectoryInfo;
    Version: string;
    Template: string
}

let projects =
    projectsToPackage
    |> Seq.map (fun path -> 
        let dir = (directoryInfo path).Parent
        let projFile = getFile dir "*.*proj"

        let getVersion path = 
            StringHelper.ReadFile path
            |> Seq.where (fun line -> line.StartsWith("Version"))
            |> Seq.map (fun line -> line.Replace("Version", "").Trim())
            |> Seq.exactlyOne

        {
            Name = projFile.Name;
            Directory = dir;
            Version = getVersion path;
            Template = path
        })


// Targets
Target "Clean" (fun _ ->
    CleanDirs [artifacts]
)

Target "Build" (fun _ ->  
    for project in projects do  
        CreateFSharpAssemblyInfo (project.Directory.FullName @@ "AssemblyVersionInfo.fs")
            [Attribute.Version project.Version; Attribute.FileVersion project.Version]

    // compile all projects below src/app/
    MSBuild null "Build" ["Configuration", buildConfig] appReferences
        |> Log "AppBuild-Output: "
)

Target "Package" (fun _ ->
 
    CreateDir buildDir
    CreateDir deployDir

    for project in projects do
        Paket.Pack (fun p -> 
            { p with
                BuildConfig = buildConfig;
                Version = project.Version;
                TemplateFile = project.Template;
                WorkingDir = buildDir;
                OutputPath = "../.." @@ deployDir;
                IncludeReferencedProjects = true;
                BuildPlatform = "AnyCPU"
            }
        )
)

// Build order
"Clean"
    ==> "Build"
    ==> "Package"

// start build
RunTargetOrDefault "Package" //"Build"
