module FsSnip.Api

open FsSnip
open FsSnip.Data
open FsSnip.Utils

open FSharp.Data
open FSharp.CodeFormat
open FSharp.Literate
open System.Text

open Suave
open Suave.Operators
open Suave.Filters

// -------------------------------------------------------------------------------------------------
// REST API - Using JSON type provider to get strongly-typed representations of returned data
// -------------------------------------------------------------------------------------------------

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

let [<Literal>] PutSnippetExample =
    """{ "title": "Hello", "author": "Tomas Petricek", "description": "Hello world",
    "code": "Fun.cube", "tags": [ "test" ], "public": true, "link": "http://tomasp.net",
    "nugetpkgs": ["na"], "source": "fun3d" }"""
    
type GetSnippetJson = JsonProvider<GetSnippetExample>
type AllSnippetsJson = JsonProvider<AllSnippetsExample>
type PutSnippetJson = JsonProvider<PutSnippetExample>
type PutSnippetResponseJson = JsonProvider<"""{ "status": "created", "id": "sY", "url": "http://fssnip.net/sY" }""">

// -------------------------------------------------------------------------------------------------
// REST API - Suave end-points that return snippets & available snippets
// -------------------------------------------------------------------------------------------------

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
        Writers.setMimeType "application/json" >=> Successful.OK (json.JsonValue.ToString())
    | None -> invalidSnippetId id

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
        
        Writers.setMimeType "application/json" >=> Successful.OK (json.ToString()))

let putSnippet =
    request (fun r ->
        try
            let json = PutSnippetJson.Parse(Encoding.UTF8.GetString r.rawForm)
            let id = Data.getNextId()
            let doc = Literate.ParseScriptString(json.Code, "/temp/Snippet.fsx", Utils.formatAgent)
            let html = Literate.WriteHtml(doc, "fs", true, true)
            Data.insertSnippet 
              { ID = id; Title = json.Title; Comment = ""; Author = json.Author; 
                Link = json.Link; Date = System.DateTime.UtcNow; Likes = 0; Private = false; 
                Passcode = ""; References = json.Nugetpkgs; Source = json.Source; Versions = 1; Tags = json.Tags }
                json.Code html
            let mangledId = Utils.mangleId id
            let response = PutSnippetResponseJson.Root("created", mangledId, "http://fssnip.net/" + mangledId)
            Successful.CREATED <| response.JsonValue.ToString()
        with
        | ex -> RequestErrors.BAD_REQUEST <| (JsonValue.Record [| ("error", JsonValue.String ex.Message) |]).ToString()
)

// Composed web part to be included in the top-level route
let webPart = 
  choose 
    [ GET >=> path "/api/1/snippet" >=>
        request (fun x -> cond (x.queryParam "all") (fun _ -> allPublicSnippets) never)
      GET >=> pathWithId "/api/1/snippet/%s" getSnippet 
      PUT >=> path "/api/1/snippet" >=> putSnippet ]
