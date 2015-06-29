module FsSnip.Pages.Insert

open System
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