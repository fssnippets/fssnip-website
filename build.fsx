// --------------------------------------------------------------------------------------
// A simple FAKE build script that:
//  1) Hosts Suave server locally & reloads web part that is defined in 'app.fsx'
//  2) Deploys the web application to Azure web sites when called with 'build deploy'
// --------------------------------------------------------------------------------------

#r "paket: groupref build //"
#load "./.fake/build.fsx/intellisense.fsx"

open Fake.Core
open Fake.Core.TargetOperators
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.DotNet
open Fake.JavaScript

open System
open System.IO

let project = "src/FsSnip.Website"
let publishDirectory = "artifacts"
let config = DotNet.BuildConfiguration.fromEnvironVarOrDefault "Configuration" DotNet.BuildConfiguration.Release
let runtime = Environment.environVarOrNone "Runtime"

// --------------------------------------------------------------------------------------
// The following uses FileSystemWatcher to look for changes in 'app.fsx'. When
// the file changes, we run `#load "app.fsx"` using the F# Interactive service
// and then get the `App.app` value (top-level value defined using `let app = ...`).
// The loaded WebPart is then hosted at localhost:8083.
// --------------------------------------------------------------------------------------

System.Environment.CurrentDirectory <- __SOURCE_DIRECTORY__

//let sbOut = new Text.StringBuilder()
//let sbErr = new Text.StringBuilder()

//let fsiSession =
//  let inStream = new StringReader("")
//  let outStream = new StringWriter(sbOut)
//  let errStream = new StringWriter(sbErr)
//  let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
//  let argv = Array.append [|"/fake/fsi.exe"; "--quiet"; "--noninteractive"; "-d:DO_NOT_START_SERVER"|] [||]
//  FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, errStream)

//let reportFsiError (e:exn) =
//  traceError "Reloading app.fsx script failed."
//  traceError (sprintf "Message: %s\nError: %s" e.Message (sbErr.ToString().Trim()))
//  sbErr.Clear() |> ignore

//let reloadScript () =
//  try
//    traceImportant "Minifying JS script"
//    Run "minify"
    
//    //Reload application
//    traceImportant "Reloading app.fsx script..."
//    let appFsx = __SOURCE_DIRECTORY__ @@ "app.fsx"
//    fsiSession.EvalInteraction(sprintf "#load @\"%s\"" appFsx)
//    fsiSession.EvalInteraction("open App")
//    match fsiSession.EvalExpression("app") with
//    | Some app -> Some(app.ReflectionValue :?> WebPart)
//    | None -> failwith "Couldn't get 'app' value"
//  with e -> reportFsiError e; None

//// --------------------------------------------------------------------------------------
//// Suave server that redirects all request to currently loaded version
//// --------------------------------------------------------------------------------------

//let currentApp = ref (fun _ -> async { return None })

//let rec findPort port =
//  try
//    let tcpListener = System.Net.Sockets.TcpListener(System.Net.IPAddress.Parse("127.0.0.1"), port)
//    tcpListener.Start()
//    tcpListener.Stop()
//    port
//  with :? System.Net.Sockets.SocketException as ex ->
//    findPort (port + 1)

//let getLocalServerConfig port =
//  { defaultConfig with
//      homeFolder = Some __SOURCE_DIRECTORY__
//      logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Debug
//      bindings = [ HttpBinding.mkSimple HTTP  "127.0.0.1" port ] }

//let reloadAppServer (changedFiles: string seq) =
//  traceImportant <| sprintf "Changes in %s" (String.Join(",",changedFiles))
//  reloadScript() |> Option.iter (fun app ->
//    currentApp.Value <- app
//    traceImportant "Refreshed app." )

//Target "run" (fun _ ->
//  let app ctx = currentApp.Value ctx
//  let port = findPort 8083
//  let _, server = startWebServerAsync (getLocalServerConfig port) app

//  // Start Suave to host it on localhost
//  reloadAppServer ["app.fsx"]
//  Async.Start(server)
//  // Open web browser with the loaded file
//  System.Diagnostics.Process.Start(sprintf "http://localhost:%d" port) |> ignore
  
//  // Watch for changes & reload when app.fsx changes
//  let sources = 
//    { BaseDirectory = __SOURCE_DIRECTORY__
//      Includes = [ "**/*.fsx"; "**/*.fs" ; "**/*.fsproj"; "web/content/app/*.js" ]; 
//      Excludes = [] }
      
//  use watcher = sources |> WatchChanges (Seq.map (fun x -> x.FullPath) >> reloadAppServer)  
//  traceImportant "Waiting for app.fsx edits. Press any key to stop."
//  Console.ReadLine() |> ignore
//)

//// -------------------------------------------------------------------------------------
//// Minifying JS for better performance 
//// This is using built in NPMHelper and other things are getting done by node js
//// -------------------------------------------------------------------------------------

Target.create "minify" (fun _ -> 
  Trace.trace "Node js web compilation thing"
  Npm.install (fun p -> { p with WorkingDirectory = __SOURCE_DIRECTORY__ })
  Npm.exec "run-script build" (fun p -> { p with WorkingDirectory = __SOURCE_DIRECTORY__ })
)

Target.create "clean" (fun _ ->
  Shell.cleanDirs [publishDirectory]
)

Target.create "build" (fun _ ->
  DotNet.build (fun p ->
    { p with Configuration = config }) project
)

Target.create "run" (fun _ ->
  DotNet.exec (fun p ->
    { p with WorkingDirectory = project }) "run" (sprintf "--no-build -c %O" config)
  |> ignore
)

Target.create "publish" (fun _ ->
    DotNet.publish (fun p ->
        { p with 
            Configuration = config
            Runtime = runtime
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
==> "build"
==> "run"

"clean"
==> "minify"
==> "publish"
==> "deploy"

Target.runOrDefault "run"