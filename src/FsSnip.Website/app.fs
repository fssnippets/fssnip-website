module FsSnip.App

open System.IO
open Suave
open Suave.Operators
open Suave.Filters
open Suave.Logging
open FsSnip
open FsSnip.Utils
open FsSnip.Pages

let createApp (config : SuaveConfig) (homeDir : string) =
  // Configure DotLiquid templates & register filters (in 'filters.fs')
  [ for t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes() do
      if t.Name = "Filters" && not (t.FullName.StartsWith "<") then yield t ]
  |> Seq.last
  |> DotLiquid.registerFiltersByType

  DotLiquid.setTemplatesDir (homeDir + "/templates")

  /// Browse static files in the 'web' subfolder
  let browseStaticFiles ctx = async {
    let root = Path.Combine(ctx.runtime.homeDirectory, "web")
    return! Files.browse root ctx }

  // Handles routing for the server
  let app =
    choose
      [ // API parts that check for specific Accept header
        Api.acceptWebPart

        // Home page, search and author & tag listings
        Home.webPart
        Search.webPart
        Author.webPart
        Tag.webPart

        // Snippet display, like, update & insert
        Snippet.webPart
        Like.webPart
        Update.webPart
        Insert.webPart

        // REST API and RSS feeds
        Api.webPart
        Rss.webPart

        // Static files and fallback case
        browseStaticFiles
        RequestErrors.NOT_FOUND "Found no handlers." ]

  let fmtLog (ctx : HttpContext) =
    sprintf "%O %s response %O %s" ctx.request.method ctx.request.url.PathAndQuery ctx.response.status.code (ctx.response.status.reason)

  app >=> logWithLevel LogLevel.Verbose config.logger fmtLog

// -------------------------------------------------------------------------------------------------
// To run the web site, you can use `build.sh` or `build.cmd` script, which is nice because it
// automatically reloads the script when it changes. But for debugging, you can also use run or
// run with debugger in VS or XS. This runs the code below.
// -------------------------------------------------------------------------------------------------


[<EntryPoint>]
let main _ =
  let homeDir = Path.Combine(__SOURCE_DIRECTORY__ , "../..") |> Path.GetFullPath
  System.Environment.SetEnvironmentVariable("FSSNIP_HOME_DIR", homeDir)
  let port = Some 5000
  let logLevel = 
  #if DEBUG
    LogLevel.Verbose
  #else
    LogLevel.Info
  #endif

  //match args |> Seq.tryPick (fun s ->
  //    if s.StartsWith("port=") then Some(int(s.Substring("port=".Length)))
  //    else Some 5000 ) with
  match port with
  | Some port ->
      let serverConfig =
        { Web.defaultConfig with
            homeFolder = Some homeDir
            logger = LiterateConsoleTarget([|"Suave"|], logLevel)
            bindings = [ HttpBinding.createSimple HTTP "127.0.0.1" port ] }

      let app = createApp serverConfig homeDir
      Web.startWebServer serverConfig app
  | None -> ()

  System.Threading.Tasks.TaskCompletionSource<int>().Task.Result