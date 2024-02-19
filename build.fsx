#r "nuget:MSBuild.StructuredLogger, 2.2.100"
#r "nuget:Fake.Core.UserInput	    ,6.0.0"
#r "nuget:Fake.Core.Target        ,6.0.0"
#r "nuget:Fake.IO.FileSystem      ,6.0.0"
#r "nuget:Fake.DotNet.Cli	        ,6.0.0"
#r "nuget:Fake.JavaScript.Npm     ,6.0.0"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.JavaScript
open System
open System.IO

#if !FAKE
Environment.GetCommandLineArgs()
|> Array.skip 2
|> Array.toList
|> Fake.Core.Context.FakeExecutionContext.Create false __SOURCE_FILE__
|> Fake.Core.Context.RuntimeContext.Fake
|> Fake.Core.Context.setExecutionContext
#endif

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
  //let npmPath = Path.GetFullPath "packages/jstools/Npm.js/tools"
  //let nodePath = Path.GetFullPath "packages/jstools/Node.js"

  //if Environment.isWindows then
  //  [ npmPath ; nodePath ; Environment.environVar "PATH" ]
  //  |> String.concat ";"
  //  |> Environment.setEnvironVar "PATH"

  //let getNpmFilePath (p : Npm.NpmParams) = 
  //  if Environment.isWindows then Path.Combine(npmPath, "npm.cmd") else p.NpmFilePath

  Npm.install (fun p -> p) //{ p with NpmFilePath = getNpmFilePath p })
  Npm.exec "run-script build" (fun p -> p) //{ p with NpmFilePath = getNpmFilePath p })
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
  let environment = Map.ofList [("LOG_LEVEL", "Info"); ("DISABLE_RECAPTCHA", "true")]
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
  let wwwroot = "wwwroot"
  Shell.mkdir wwwroot
  Shell.cleanDir wwwroot
  Shell.mkdir (wwwroot </> "templates")
  Shell.mkdir (wwwroot </> "web")
  printfn "copying binaries into wwwroot"
  Shell.copyRecursive publishDirectory wwwroot false |> ignore
  printfn "copying templates into wwwroot/templates"
  Shell.copyRecursive "templates" (wwwroot </> "templates") false |> ignore
  printfn "copying web into wwwroot/web"
  Shell.copyRecursive "web" (wwwroot </> "web") false |> ignore
)

Target.create "root" ignore

"root"
==> "minify"
=?> ("download-data-dump", not (File.Exists "data/index.json"))
==> "run"

"clean"
==> "minify"
==> "publish"
==> "deploy"

Target.runOrDefaultWithArguments "deploy"