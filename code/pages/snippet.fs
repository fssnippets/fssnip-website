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
    Details : Data.Snippet
    Revision : int }

let invalidSnippetId id =
  RequestErrors.NOT_FOUND (sprintf "Snippet with id %s not found" id)

let showSnippet id r =
  let id' = demangleId id
  match Seq.tryFind (fun s -> s.ID = id') publicSnippets with
  | Some snippetInfo -> 
      match Data.loadSnippet id r with
      | Some snippet ->
          let rev = match r with Latest -> snippetInfo.Versions - 1 | Revision r -> r
          { Html = snippet
            Details = Data.snippets |> Seq.find (fun s -> s.ID = id')
            Revision = rev }
          |> DotLiquid.page<FormattedSnippet> "snippet.html"
      | None -> invalidSnippetId id
  | None -> invalidSnippetId id

let showRawSnippet id r =
  match Data.loadRawSnippet id r with
  | Some s -> Writers.setMimeType "text/plain" >>= OK s
  | None -> invalidSnippetId id