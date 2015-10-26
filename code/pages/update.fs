module FsSnip.Pages.Update

open System
open System.IO
open Suave
open Suave.Types
open Suave.Http
open Suave.Http.Successful
open FsSnip
open FSharp.Literate
open FsSnip.Data
open FsSnip.Utils
open FSharp.CodeFormat

type RawSnippet =
  { Raw : string
    Details : Data.Snippet
    Revision : int }

type UpdateForm =
  { Title : string
    Description : string option
    Tags : string option
    Author : string option
    Link : string option
    Code : string
    NugetPkgs : string option }

let showForm id =
    let id' = demangleId id
    match Seq.tryFind (fun s -> s.ID = id') publicSnippets with
    | Some snippetInfo -> 
        match Data.loadRawSnippet id Latest with
        | Some snippet ->
            let rev = snippetInfo.Versions - 1
            { Raw = snippet
              Details = Data.snippets |> Seq.find (fun s -> s.ID = id')
              Revision = rev }
            |> DotLiquid.page<RawSnippet> "update.html"
        | None -> invalidSnippetId id
    | None -> invalidSnippetId id

let formatAgent = CodeFormat.CreateAgent()

let updateSnippet id ctx = async {
  if ctx.request.form |> Seq.exists (function "submit", _ -> true | _ -> false) then
    let form = Utils.readForm<UpdateForm> ctx.request.form
    
    // Assuming all input is valid (TODO issue #12)
    let nugetReferences = 
      match form.NugetPkgs with
      | Some s when not (String.IsNullOrWhiteSpace(s)) -> s.Split(',')
      | _ -> [| |]

    // TODO: Download NuGet packages and pass "-r:..." args to the formatter! (issue #13)
    let doc = Literate.ParseScriptString(form.Code, "/temp/Snippet.fsx", formatAgent)
    let html = Literate.WriteHtml(doc, "fs", true, true)

    // TODO: versions should not be 4000, and data layer needs to support inserting new versions
    // TODO: check password if there is one
    // TODO: if old snippet was hidden, new one should be too
    match form with
    | { Description = Some descr; Author = Some author; Link = Some link; 
        Tags = Some tags } when not (String.IsNullOrWhiteSpace(tags)) ->
        let tags = tags.Split(',')
        Data.insertSnippet 
          { ID = id; Title = form.Title; Comment = descr; 
            Author = author; Link = link; Date = System.DateTime.UtcNow;
            Likes = 0; Private = false; Passcode = ""; 
            References = nugetReferences; Source = ""; Versions = 4000; 
            Tags = tags }
          form.Code html
    | _ -> 
        failwith "Invalid input!"
    return! Redirection.FOUND ("/" + Utils.mangleId id) ctx
  else
    return! showForm id ctx }