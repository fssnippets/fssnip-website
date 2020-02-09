#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"
#if !NETCOREAPP
#r "System.IO.Compression.FileSystem.dll"
#endif

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.JavaScript
open System.IO

let project = "src/FsSnip.Website"
let publishDirectory = "artifacts"

// FAKE 5 does not apply cli args to environment before module initialization, delay
// override syntax: dotnet fake run build.fsx -e foo=bar -e foo=bar -t target
let config () = DotNet.BuildConfiguration.fromEnvironVarOrDefault "configuration" DotNet.BuildConfiguration.Release
let runtime () = Environment.environVarOrNone "runtime"

System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

//// -------------------------------------------------------------------------------------
//// Minifying JS for better performance 
//// This is using built in NPMHelper and other things are getting done by node js
//// -------------------------------------------------------------------------------------

Target.create "minify" (fun _ -> 
  Trace.trace "Node js web compilation thing"

  // Use nuget tools if windows, require already installed otherwise
  let npmPath = Path.GetFullPath "packages/jstools/Npm.js/tools"
  let nodePath = Path.GetFullPath "packages/jstools/Node.js"

  if Environment.isWindows then
    [ npmPath ; nodePath ; Environment.environVar "PATH" ]
    |> String.concat ";"
    |> Environment.setEnvironVar "PATH"

  let getNpmFilePath (p : Npm.NpmParams) = 
    if Environment.isWindows then Path.Combine(npmPath, "npm.cmd") else p.NpmFilePath

  Npm.install (fun p -> { p with NpmFilePath = getNpmFilePath p })
  Npm.exec "run-script build" (fun p -> { p with NpmFilePath = getNpmFilePath p })
)

Target.create "clean" (fun _ ->
  Shell.cleanDirs [publishDirectory]
)

let dataDumpLocation = System.Uri "https://github.com/fssnippets/fssnip-data/archive/master.zip"

Target.create "download-data-dump" (fun _ ->
    Directory.delete "data"
    let tmpfile = Path.ChangeExtension(Path.GetTempFileName(), ".zip")
    use client = new System.Net.WebClient()
    client.DownloadFile(dataDumpLocation, tmpfile)
    System.IO.Compression.ZipFile.ExtractToDirectory(tmpfile, ".")
    Directory.Move("fssnip-data-master", "data")
)

Target.create "run" (fun _ ->
  let environment = Map.ofList [("LOG_LEVEL", "Debug"); ("DISABLE_RECAPTCHA", "true")]
  DotNet.exec (fun p ->
    { p with WorkingDirectory = project ; Environment = environment }) "run" (sprintf "-c %O" (config()))
  |> ignore
)

Target.create "publish" (fun _ ->
    DotNet.publish (fun p ->
        { p with 
            Configuration = config ()
            Runtime = runtime ()
            OutputPath = Some publishDirectory
        }) project
)

let newName prefix f = 
  Seq.initInfinite (sprintf "%s_%d" prefix) |> Seq.skipWhile (f >> not) |> Seq.head

Target.create "deploy" (fun _ ->
  // Pick a subfolder that does not exist
  let wwwroot = "../wwwroot"
  let subdir = newName "deploy" (fun sub -> not (Directory.Exists(wwwroot </> sub)))
  let deployroot = wwwroot </> subdir
  
  // Clean & Deploy everything into new empty folder
  Shell.cleanDir deployroot
  Shell.cleanDir (deployroot </> "bin")
  Shell.cleanDir (deployroot </> "templates")
  Shell.cleanDir (deployroot </> "web")

  Shell.copyRecursive publishDirectory (deployroot </> "bin") false |> ignore
  Shell.copyRecursive "templates" (deployroot </> "templates") false |> ignore
  Shell.copyRecursive "web" (deployroot </> "web") false |> ignore
  let config = File.ReadAllText("web.config").Replace("%DEPLOY_SUBDIRECTORY%", subdir)
  File.WriteAllText(wwwroot </> "web.config", config)

  // Try to delete previous folders, but ignore failures
  for dir in Directory.GetDirectories(wwwroot) do
    if Path.GetFileName(dir) <> subdir then 
      try Shell.cleanDir dir; Shell.deleteDir dir with _ -> ()
)

"minify"
=?> ("download-data-dump", not (File.Exists "data/index.json"))
==> "run"

"clean"
==> "minify"
==> "publish"
==> "deploy"

Target.runOrDefaultWithArguments "run"