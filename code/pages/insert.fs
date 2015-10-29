module FsSnip.Pages.Insert

open System
open System.IO
open Suave
open Suave.Types
open Suave.Http
open Suave.Http.Successful
open FsSnip
open FSharp.CodeFormat
open FSharp.Literate

// -------------------------------------------------------------------------------------------------
// Snippet details and raw view pages
// -------------------------------------------------------------------------------------------------

type InsertForm =
  { Hidden : bool 
    Title : string
    Passcode : string option
    Description : string option
    Tags : string option
    Author : string option
    Link : string option
    Code : string
    NugetPkgs : string option }

let formatAgent = CodeFormat.CreateAgent()

let insertSnippet ctx = async { 
  if ctx.request.form |> Seq.exists (function "submit", _ -> true | _ -> false) then
    let form = Utils.readForm<InsertForm> ctx.request.form
    
    // Assuming all input is valid (TODO issue #12)
    let nugetReferences = 
      match form.NugetPkgs with
      | Some s when not (String.IsNullOrWhiteSpace(s)) -> s.Split(',')
      | _ -> [| |]

    // TODO: Download NuGet packages and pass "-r:..." args to the formatter! (issue #13)
    let doc = Literate.ParseScriptString(form.Code, "/temp/Snippet.fsx", formatAgent)
    let html = Literate.WriteHtml(doc, "fs", true, true)
    let id = Data.getNextId()
    match form with
    | { Hidden = true } ->
        Data.insertSnippet 
          { ID = id; Title = form.Title; Comment = ""; Author = ""; 
            Link = ""; Date = System.DateTime.UtcNow; Likes = 0; Private = true; 
            Passcode = defaultArg form.Passcode ""; 
            References = nugetReferences; Source = ""; Versions = 1; Tags = [| |] }
          form.Code html
    | { Hidden = false; Description = Some descr; Author = Some author; Link = Some link; 
        Tags = Some tags } when not (String.IsNullOrWhiteSpace(tags)) ->
        let tags = tags.Split(',')
        Data.insertSnippet 
          { ID = id; Title = form.Title; Comment = descr; 
            Author = author; Link = link; Date = System.DateTime.UtcNow;
            Likes = 0; Private = form.Hidden; Passcode = defaultArg form.Passcode ""; 
            References = nugetReferences; Source = ""; Versions = 1; 
            Tags = tags }
          form.Code html
    | _ -> 
        failwith "Invalid input!"
    return! Redirection.FOUND ("/" + Utils.mangleId id) ctx
  else
    return! DotLiquid.page "insert.html" () ctx }


open FSharp.Data
type Errors = JsonProvider<"""[ {"location":[1,1,10,10], "error":true, "message":"sth"} ]""">

let checkSnippet ctx = async {
  use sr = new StreamReader(new MemoryStream(ctx.request.rawForm))
  let request = sr.ReadToEnd()
  let doc = Literate.ParseScriptString(request, "/temp/Snippet.fsx", formatAgent)
  let json = 
    JsonValue.Array
      [| for SourceError((l1,c1),(l2,c2),kind,msg) in doc.Errors ->
         Errors.Root([| l1; c1; l2; c2 |], (kind = ErrorKind.Error), msg).JsonValue |]

  return! ctx |>
    ( Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
      >>= Writers.setHeader "Pragma" "no-cache"
      >>= Writers.setHeader "Expires" "0"
      >>= Writers.setMimeType "application/json"
      >>= Successful.OK(json.ToString()) ) }

module Api =
    open System.Text

    let [<Literal>] PutSnippetExample =
        """{ "title": "Hello", "author": "Tomas Petricek", "description": "Hello world",
        "code": "Fun.cube", "tags": [ "test" ], "public": true, "link": "http://tomasp.net",
        "nugetpkgs": ["na"], "source": "fun3d" }"""

    type PutSnippetJson = JsonProvider<PutSnippetExample>

    type PutSnippetResponseJson = JsonProvider<"""{ "status": "created", "id": "sY", "url": "http://fssnip.net/sY" }""">

    let putSnippet =
        request (fun r ->
            try
                let json = PutSnippetJson.Parse(Encoding.UTF8.GetString r.rawForm)
                let id = Data.getNextId()
                let doc = Literate.ParseScriptString(json.Code, "/temp/Snippet.fsx", formatAgent)
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
