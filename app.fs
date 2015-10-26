module FsSnip.App

open System.IO
open Suave
open Suave.Http
open Suave.Types
open Suave.Http.Applicatives

open FsSnip
open FsSnip.Data
open FsSnip.Utils
open FsSnip.Pages

// -------------------------------------------------------------------------------------------------
// Server entry-point and routing
// -------------------------------------------------------------------------------------------------

let browseStaticFiles ctx = async {
  let root = Path.Combine(ctx.runtime.homeDirectory, "web")
  return! Files.browse root ctx }

// Configure DotLiquid templates & register filters (in 'filters.fs')
[ for t in System.Reflection.Assembly.GetExecutingAssembly().GetTypes() do
    if t.Name = "Filters" && not (t.FullName.StartsWith "<") then yield t ]
|> Seq.last
|> DotLiquid.registerFiltersByType

DotLiquid.setTemplatesDir (__SOURCE_DIRECTORY__ + "/templates")

// Handles routing for the server
let app =
  choose
    [ path "/" >>= Home.showHome
      pathScan "/%s/%d" (fun (id, r) -> Snippet.showSnippet id (Revision r))
      pathWithId "/%s" (fun id -> Snippet.showSnippet id Latest)
      pathWithId "/%s/update" (fun id ctx -> Update.updateSnippet id ctx)
      pathScan "/raw/%s/%d" (fun (id, r) -> Snippet.showRawSnippet id (Revision r))
      pathWithId "/raw/%s" (fun id -> Snippet.showRawSnippet id Latest)
      path "/pages/insert" >>= Insert.insertSnippet
      path "/pages/insert/check" >>= Insert.checkSnippet
      path "/authors/" >>= Author.showAll
      pathScan "/authors/%s" Author.showSnippets
      path "/tags/" >>= Tag.showAll
      pathScan "/test/%s" (fun s -> Successful.OK s)
      pathScan "/tags/%s" Tag.showSnippets
      PUT >>= path "/api/1/snippet" >>= Insert.Api.putSnippet
      GET >>= path "/api/1/snippet" >>=
        request (fun x -> cond (x.queryParam "all") (fun _ -> Snippet.Api.allPublicSnippets) never)
      GET >>= pathWithId "/api/1/snippet/%s" (fun id -> Snippet.Api.getSnippet id)
      ( path "/rss/" <|> path "/rss" <|> path "/pages/Rss" <|> path "/pages/Rss/" ) >>= Rss.getRss
      browseStaticFiles
      RequestErrors.NOT_FOUND "Found no handlers." ]


