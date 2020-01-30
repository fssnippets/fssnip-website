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
    NugetPkgs : string option
    Session : string }

type InsertSnippetModel =
    { Session: string }
    static member Create() = { Session = Guid.NewGuid().ToString() }

let insertSnippet ctx = async {
  if ctx.request.form |> Seq.exists (function "submit", _ -> true | _ -> false) then
    //// Give up early if the reCAPTCHA was not correct
    //let! valid = Recaptcha.validateRecaptcha ctx.request.form
    //if not valid then
    //    return! Recaptcha.recaptchaError ctx
    //else

    // Parse the inputs and the F# source file
    let form = Utils.readForm<InsertForm> ctx.request.form
    let nugetReferences = Utils.parseNugetPackages form.NugetPkgs

    let id = Data.getNextId()
    let doc = Parser.parseScript form.Session form.Code nugetReferences
    let html = Literate.WriteHtml(doc, "fs", true, true)
    Parser.completeSession form.Session

    // Insert as private or public, depending on the check box
    match form with    
    | { Hidden = true } ->
        Data.insertSnippet
          { ID = id; Title = form.Title; Comment = ""; Author = "";
            Link = ""; Date = System.DateTime.UtcNow; Likes = 0; Private = true;
            Passcode = Utils.sha1Hash (defaultArg form.Passcode "");
            References = nugetReferences; Source = ""; Versions = 1; Tags = [| |] }
          form.Code html
        return! Redirection.FOUND ("/" + Utils.mangleId id) ctx

    | { Hidden = false; Description = Some descr; Author = Some author; Link = link; 
        Tags = tags } when tags.Length > 0 ->
        Data.insertSnippet 
          { ID = id; Title = form.Title; Comment = descr; 
            Author = author; Link = link; Date = System.DateTime.UtcNow;
            Likes = 0; Private = form.Hidden; Passcode = Utils.sha1Hash (defaultArg form.Passcode "");
            References = nugetReferences; Source = ""; Versions = 1;
            Tags = tags |> Array.map(fun t -> t.ToLowerInvariant()) }
          form.Code html
        return! Redirection.FOUND ("/" + Utils.mangleId id) ctx

    | _ ->
        let details = 
          "Some of the inputs for the snippet were not valid, but the client-side checking"+
          " did not catch that. Please consider opening a bug issue!"
        return! Error.reportError HTTP_400 "Inserting snippet failed!" details ctx

  else
    return! DotLiquid.page "insert.html" (InsertSnippetModel.Create()) ctx } 

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
  let errors, tags = 
    try
      // Check the snippet and report errors
      let form = Utils.readForm<InsertForm> request.form
      let nugetReferences = Utils.parseNugetPackages form.NugetPkgs
      let doc = Parser.parseScript form.Session form.Code nugetReferences
      let errors = 
        [| for SourceError((l1,c1),(l2,c2),kind,msg) in doc.Errors ->
            CheckResponse.Error([| l1; c1; l2; c2 |], (kind = ErrorKind.Error), msg) |]

      // Recommend tags based on the snippet contents
      let tags = [| |]
      errors, tags
    with e ->
      [| CheckResponse.Error([| 0; 0; 0; 0 |], true, "Parsing the snippet failed.") |], [| |]
  ( disableCache
    >=> Successful.OK(CheckResponse.Root(errors, tags).ToString()) ))

let listTags = request (fun _ -> 
    let tags = 
      Data.getAllPublicSnippets() 
      |> Seq.collect (fun snip -> snip.Tags) 
      |> Seq.map (fun tag -> tag.Trim().ToLowerInvariant())
      |> Seq.distinct
      |> Seq.sort
    let json = JsonValue.Array [| for s in tags -> JsonValue.String s |]
    disableCache >=> Successful.OK(json.ToString()) )
     
let webPart = 
  choose 
   [ path "/pages/insert" >=> insertSnippet
     path "/pages/insert/taglist" >=> listTags
     path "/pages/insert/check" >=> checkSnippet ]