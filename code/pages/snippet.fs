module FsSnip.Snippet

open FsSnip
open FsSnip.Data
open FsSnip.Utils
open Suave
open Suave.Operators
open Suave.Filters

// -------------------------------------------------------------------------------------------------
// Snippet details and raw view pages
// -------------------------------------------------------------------------------------------------

type FormattedSnippet =
  { Html : string
    Details : Data.Snippet
    Revision : int }

let invalidSnippetId id =
  Error.reportError HttpCode.HTTP_404 "Snippet not found" 
    (sprintf "The snippet '%s' that you are looking for was not found." id)

let showSnippet id r =
  let id' = demangleId id
  match Seq.tryFind (fun s -> s.ID = id') snippets with
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
  | Some s -> Writers.setMimeType "text/plain" >=> Successful.OK s
  | None -> invalidSnippetId id
  
// Web part to be included in the top-level route specification  
let webPart = 
  choose 
    [ pathScan "/%s/%d" (fun (id, r) -> showSnippet id (Revision r))
      pathWithId "/%s" (fun id -> showSnippet id Latest)
      pathScan "/raw/%s/%d" (fun (id, r) -> showRawSnippet id (Revision r))
      pathWithId "/raw/%s" (fun id -> showRawSnippet id Latest) ]
  