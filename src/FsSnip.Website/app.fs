module FsSnip.App

open System
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
        Test.webPart

        // REST API and RSS feeds
        Api.webPart
        Rss.webPart

        // Static files and fallback case
        browseStaticFiles
        RequestErrors.NOT_FOUND "Found no handlers." ]

  let fmtLog (ctx : HttpContext) =
    sprintf "%O %s response %O %s" ctx.request.method ctx.request.url.PathAndQuery ctx.response.status.code (ctx.response.status.reason)

  app >=> logWithLevel LogLevel.Info config.logger fmtLog

let defaultHomeDir = Path.Combine(__SOURCE_DIRECTORY__, "../..")

[<EntryPoint>]
let main _ =
  let ipAddress = Environment.GetEnvironmentVariable("IP_ADDRESS", defaultValue = "127.0.0.1")
  let port = Environment.GetEnvironmentVariable("PORT", defaultValue = "5000") |> int
  let logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL", defaultValue = "Info") |> LogLevel.ofString
  let homeDir = Environment.GetEnvironmentVariable("FSSNIP_HOME_DIR", defaultValue = defaultHomeDir)

  Recaptcha.ensureConfigured()

  let serverConfig =
    { Web.defaultConfig with
        homeFolder = Some (Path.GetFullPath homeDir)
        logger = LiterateConsoleTarget([|"Suave"|], logLevel)
        bindings = [ HttpBinding.createSimple HTTP ipAddress port ] }

  let app = createApp serverConfig homeDir
  Web.startWebServer serverConfig app

  0
