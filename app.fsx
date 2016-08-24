#r "System.Xml.Linq.dll"
#r "packages/Suave/lib/net40/Suave.dll"
#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "packages/DotLiquid/lib/NET45/DotLiquid.dll"
#r "packages/Suave.DotLiquid/lib/net40/Suave.DotLiquid.dll"
#r "packages/Chessie/lib/net40/Chessie.dll"
#r "packages/Paket.Core/lib/net45/Paket.Core.dll"
#r "packages/FSharp.Compiler.Service/lib/net45/FSharp.Compiler.Service.dll"
#if INTERACTIVE
#load "packages/FSharp.Azure.StorageTypeProvider/StorageTypeProvider.fsx"
#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"
#endif
open System
open System.Web
open System.IO
open Suave
open Suave.Web
open Suave.Operators
open Suave.Filters
open FSharp.Data
open FSharp.Azure.StorageTypeProvider

// -------------------------------------------------------------------------------------------------
// Loading the FsSnip.WebSite project files
// -------------------------------------------------------------------------------------------------

#load "code/common/storage/azure.fs"
#load "code/common/storage/local.fs"
#load "code/common/utils.fs"
#load "code/common/filters.fs"
#load "code/common/data.fs"
#load "code/common/rssfeed.fs"
#load "code/common/parser.fs"
#load "code/pages/home.fs"
#load "code/pages/error.fs"
#load "code/pages/recaptcha.fs"
#load "code/pages/insert.fs"
#load "code/pages/snippet.fs"
#load "code/pages/update.fs"
#load "code/pages/search.fs"
#load "code/pages/like.fs"
#load "code/pages/author.fs"
#load "code/pages/tag.fs"
#load "code/pages/rss.fs"
#load "code/api.fs"
open FsSnip
open FsSnip.Data
open FsSnip.Utils
open FsSnip.Pages

// -------------------------------------------------------------------------------------------------
// Server entry-point and routing
// -------------------------------------------------------------------------------------------------

// Home directory is directory of 'app.fsx' (in FSI) or compiled app (in Azure)
let homeDir = 
  if System.Reflection.Assembly.GetExecutingAssembly().IsDynamic then __SOURCE_DIRECTORY__ else
    let binDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
    Path.GetFullPath(Path.Combine(binDir, ".."))

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
    [ // Home page, search and author & tag listings
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

// -------------------------------------------------------------------------------------------------
// To run the web site, you can use `build.sh` or `build.cmd` script, which is nice because it
// automatically reloads the script when it changes. But for debugging, you can also use run or
// run with debugger in VS or XS. This runs the code below.
// -------------------------------------------------------------------------------------------------

// When port was specified, we start the app (in Azure), 
// otherwise we do nothing (it is hosted by 'build.fsx')
#if INTERACTIVE
#else
match System.Environment.GetCommandLineArgs() |> Seq.tryPick (fun s ->
    if s.StartsWith("port=") then Some(int(s.Substring("port=".Length)))
    else None ) with
| Some port ->
    let serverConfig =
      { Web.defaultConfig with
          logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Warn
          homeFolder = Some homeDir
          bindings = [ HttpBinding.mkSimple HTTP "127.0.0.1" port ] }
    Web.startWebServer serverConfig app
| _ -> ()
#endif
