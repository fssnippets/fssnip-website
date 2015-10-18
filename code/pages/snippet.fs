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

module Api = 
    open FSharp.Data

    let [<Literal>] GetSnippetExample =
        """{
            "id": 22,
            "title": "Filtering lists",
            "comment": "Two functions showing how to filter functional lists using the specified predicate.",
            "author": "Tomas Petricek",
            "link": "http://tomasp.net",
            "date": "2010-12-03T23:56:49.3730000",
            "likes": 56,
            "references": ["Nada"],
            "source": "http",
            "versions": 1,
            "formatted": "let x = 4",
            "tags": [ "list", "filter", "recursion" ]
        }"""

    type GetSnippetJson = JsonProvider<GetSnippetExample>

    let getSnippet id =
        let id' = demangleId id
        match Data.loadSnippet id Latest with
        | Some formatted ->
            let details = Data.snippets |> Seq.find (fun s -> s.ID = id')
            let json =
                GetSnippetJson.Root(
                    details.ID, details.Title, details.Comment, details.Author, details.Link,
                    details.Date, details.Likes, Array.ofSeq details.References, details.Source, details.Versions,
                    formatted, Array.ofSeq details.Tags)            
            Writers.setMimeType "application/json" >>= OK (json.JsonValue.ToString())
        | None -> invalidSnippetId id

    let [<Literal>] AllSnippetsExample =
        """{
          "id": 22,
          "title": "Filtering lists",
          "comment": "Two functions showing how to filter functional lists using the specified predicate.",
          "author": "Tomas Petricek",
          "link": "http://tomasp.net",
          "date": "2010-12-03T23:56:49.3730000",
          "likes": 56,
          "references": ["Nada"],
          "source": "http",
          "versions": 1,
            "tags": [ "list", "filter", "recursion" ]
        }"""

    type AllSnippetsJson = JsonProvider<AllSnippetsExample>

    let allPublicSnippets =
        delay (fun () ->
            let snippets = getAllPublicSnippets()
            let json =
                snippets
                |> Seq.map (fun x ->
                    AllSnippetsJson.Root(
                        x.ID, x.Title, x.Comment, x.Author, x.Link, x.Date, x.Likes, Array.ofSeq x.References,
                        x.Source, x.Versions, Array.ofSeq x.Tags))
                |> Seq.map (fun x -> x.JsonValue)
                |> Array.ofSeq
                |> JsonValue.Array
            
            Writers.setMimeType "application/json" >>= OK (json.ToString()))
