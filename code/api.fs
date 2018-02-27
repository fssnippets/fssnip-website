module FsSnip.Api

open FsSnip
open FsSnip.Data
open FsSnip.Utils

open System
open System.Text
open FSharp.Data
open FSharp.CodeFormat
open FSharp.Literate

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

let [<Literal>] FormatSnippetExamples = 
    """[ {"snippets": [ "let x = 1", "x + 1" ], "packages":["FSharp.Data"] },
         {"snippets": [ "let x = 1", "x + 1" ], "packages":["FSharp.Data"], "prefix":"fs" },
         {"snippets": [ "let x = 1", "x + 1" ], "packages":["FSharp.Data"], "lineNumbers":false } ]"""

type GetSnippetJson = JsonProvider<GetSnippetExample>
type AllSnippetsJson = JsonProvider<AllSnippetsExample>
type PutSnippetJson = JsonProvider<PutSnippetExample>
type FormatSnippetJson = JsonProvider<FormatSnippetExamples, SampleIsList=true>
type FormatSnippetResult = JsonProvider<"""{ "snippets":["html", "more html"], "tips":"divs" }""">
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
                details.Date, details.Likes, Array.ofSeq details.References, 
                (if details.Source.IndexOf("using System;") > -1 then "// Non-usable." else details.Source), details.Versions,
                formatted, Array.ofSeq details.Tags)            
        Writers.setMimeType "application/json" >=> Successful.OK (json.JsonValue.ToString())
    | None -> RequestErrors.NOT_FOUND (sprintf "Snippet %s not found" id)

let getPublicSnippets = request (fun request ->
    let all = request.queryParam "all" = Choice1Of2 "true"
    let snippets = getAllPublicSnippets()
    let snippets = if all then snippets else snippets |> Seq.take 20
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
            if json.Code.IndexOf("using System;") > -1 then
                raise (NullReferenceException "Cannot insert anti-pattern.")
            let id = Data.getNextId()
            let session = (System.Guid.NewGuid().ToString())
            let doc = Parser.parseScript session json.Code json.Nugetpkgs
            let html = Literate.WriteHtml(doc, "fs", true, true)
            Data.insertSnippet 
              { ID = id; Title = json.Title; Comment = json.Description; Author = json.Author; 
                Link = json.Link; Date = System.DateTime.UtcNow; Likes = 0; Private = false; 
                Passcode = ""; References = json.Nugetpkgs; Source = json.Source; Versions = 1; Tags = json.Tags }
                json.Code html
            let mangledId = Utils.mangleId id
            let response = PutSnippetResponseJson.Root("created", mangledId, "http://fssnip.net/" + mangledId)
            Parser.completeSession session
            Successful.CREATED <| response.JsonValue.ToString()
        with ex -> 
            RequestErrors.BAD_REQUEST <| (JsonValue.Record [| ("error", JsonValue.String ex.Message) |]).ToString()
)

let contentType (str:string) = request (fun r ctx -> 
    if r.headers |> Seq.exists (fun (h, v) -> h.ToLower() = "content-type" && v = str) then async.Return(Some ctx)
    else async.Return(None) )

let formatSnippets = request (fun r ->    
    let request = 
      try 
          let r = FormatSnippetJson.Parse(Encoding.UTF8.GetString r.rawForm)
          Some(r.Snippets, r.Packages, defaultArg r.LineNumbers true, defaultArg r.Prefix "fs")
      with _ -> None 
    match request with 
    | None -> RequestErrors.BAD_REQUEST "Incorrectly formatted JSON"
    | Some(snippets, packages, lineNumbers, prefix) ->
        let session = Guid.NewGuid().ToString()
        let snippets = snippets |> String.concat ("\n(** " + session + " *)\n")
        let doc = Parser.parseScript session snippets packages
        let html = Literate.WriteHtml(doc, prefix, lineNumbers)
        let indexOfTips = html.IndexOf("<div class=\"tip\"")
        let html, tips = 
            if indexOfTips = -1 then html, ""
            else html.Substring(0, indexOfTips), html.Substring(indexOfTips)
        let formatted = html.Split([| "<p>" + session + "</p>" |], StringSplitOptions.None)
        let res = FormatSnippetResult.Root(formatted, tips).ToString()
        Successful.OK res )

// Composed web part to be included in the top-level route
let apis = 
  choose [ 
    GET >=> path "/1/snippet" >=> getPublicSnippets
    GET >=> pathWithId "/1/snippet/%s" getSnippet 
    PUT >=> path "/1/snippet" >=> putSnippet ]

let webPart = 
  choose 
    [ clientHost "api.fssnip.net" >=> apis
      apis ] 

let acceptWebPart = 
  contentType "application/vnd.fssnip-v1+json" >=> 
    POST >=> path "/api/format" >=> formatSnippets 