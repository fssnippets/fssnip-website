module FsSnip.Pages.Insert

open System
open System.IO
open Suave
open Suave.Operators
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
    Tags : string[]
    Author : string option
    Link : string
    Code : string
    NugetPkgs : string option }

let insertSnippet ctx = async { 
  if ctx.request.form |> Seq.exists (function "submit", _ -> true | _ -> false) then
    let form = Utils.readForm<InsertForm> ctx.request.form
    
    // Assuming all input is valid (TODO issue #12)
    let nugetReferences = 
      match form.NugetPkgs with
      | Some s when not (String.IsNullOrWhiteSpace(s)) -> s.Split(',')
      | _ -> [| |]

    // TODO: Download NuGet packages and pass "-r:..." args to the formatter! (issue #13)
    let doc = Literate.ParseScriptString(form.Code, "/temp/Snippet.fsx", Utils.formatAgent)
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

    | { Hidden = false; Description = Some descr; Author = Some author; Link = link; 
        Tags = tags } when tags.Length > 0 ->
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

// -------------------------------------------------------------------------------------------------
// REST API for checking snippet and listing tags
// -------------------------------------------------------------------------------------------------

open FSharp.Data
open Suave.Filters

let disableCache = 
  Writers.setHeader "Cache-Control" "no-cache, no-store, must-revalidate"
  >=> Writers.setHeader "Pragma" "no-cache"
  >=> Writers.setHeader "Expires" "0"
  >=> Writers.setMimeType "application/json"

type CheckResponse = JsonProvider<"""
  { "errors": [ {"location":[1,1,10,10], "error":true, "message":"sth"} ],
    "tags": [ "test", "demo" ] }""">

let checkSnippet = request (fun request -> 
  use sr = new StreamReader(new MemoryStream(request.rawForm))
  let request = sr.ReadToEnd()
  let errors, tags = 
    try
      // Check the snippet and report errors
      let doc = Literate.ParseScriptString(request, "/temp/Snippet.fsx", Utils.formatAgent)
      let errors = 
        [| for SourceError((l1,c1),(l2,c2),kind,msg) in doc.Errors ->
            CheckResponse.Error([| l1; c1; l2; c2 |], (kind = ErrorKind.Error), msg) |]

      // Recommend tags based on the snippet contents
      let tags = [| "pattern matching"; "test" |]
      errors, tags
    with e ->
      [| CheckResponse.Error([| 0; 0; 0; 0 |], true, "Parsing the snippet failed.") |], [| |]

  ( disableCache
    >=> Successful.OK(CheckResponse.Root(errors, tags).ToString()) ))

let listTags = request (fun _ -> 
    let tags = Data.getAllPublicSnippets() |> Seq.collect (fun snip -> snip.Tags) |> Seq.distinct
    let json = JsonValue.Array [| for s in tags -> JsonValue.String s |]
    disableCache >=> Successful.OK(json.ToString()) )
     
let webPart = 
  choose 
   [ path "/pages/insert" >=> insertSnippet
     path "/pages/insert/taglist" >=> listTags
     path "/pages/insert/check" >=> checkSnippet ]
