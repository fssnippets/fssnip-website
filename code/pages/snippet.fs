module FsSnip.Pages.Snippet

open System
open System.Web
open FsSnip
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

// TODO: Handle the case when `id` does not exist (#5)

let showSnippet id =
  { Html = Data.loadSnippet id
    Details = Data.snippets |> Seq.find (fun s -> s.ID = demangleId id) }
  |> DotLiquid.page<FormattedSnippet> "snippet.html"

let showRawSnippet id =
  Writers.setMimeType "text/plain" >>= OK (Data.loadRawSnippet id)