module FsSnip.Pages.Snippet

open System
open System.Web
open FsSnip
open FsSnip.Data
open FsSnip.Utils
open Suave
open Suave.Http
open Suave.Http.Successful

// -------------------------------------------------------------------------------------------------
// Snippet details and raw view pages
// -------------------------------------------------------------------------------------------------

type FormattedSnippet =
  { Html : string
    Details : Data.Snippet }

let showSnippet id =
  match Data.loadSnippet id Latest with
  | Some s ->
            { Html = s
              Details = Data.snippets |> Seq.find (fun s -> s.ID = demangleId id) }
            |> DotLiquid.page<FormattedSnippet> "snippet.html"
  | None -> RequestErrors.NOT_FOUND "Invalid snippet ID"

let showRawSnippet id =
  match Data.loadRawSnippet id Latest with
  | Some s ->
    Writers.setMimeType "text/plain" >>= OK s
  | None -> RequestErrors.NOT_FOUND "Invalid snippet ID"