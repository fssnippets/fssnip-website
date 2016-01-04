module FsSnip.Pages.Update

open System
open Suave
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

let formatAgent = CodeFormat.CreateAgent()

let showForm snippetInfo mangledId id' =
  match Data.loadRawSnippet mangledId Latest with
  | Some snippet ->
    let rev = snippetInfo.Versions - 1
    { Raw = snippet
      Details = Data.snippets |> Seq.find (fun s -> s.ID = id')
      Revision = rev }
    |> DotLiquid.page<RawSnippet> "update.html"
  | None -> invalidSnippetId mangledId

// Assuming all input is valid (TODO issue #12)
let handlePost snippetInfo requestForm mangledId id' =
  let form = Utils.readForm<UpdateForm> requestForm

  let nugetReferences = 
    match form.NugetPkgs with
    | Some s when not (String.IsNullOrWhiteSpace(s)) -> s.Split(',')
    | _ -> [| |]

  // TODO: Download NuGet packages and pass "-r:..." args to the formatter! (issue #13)
  let doc = Literate.ParseScriptString(form.Code, "/temp/Snippet.fsx", formatAgent)
  let html = Literate.WriteHtml(doc, "fs", true, true)

  // TODO: check password if there is one
  // TODO: if old snippet was hidden, new one should be too
  // TODO: handle concurrent updates gracefully
  match form with
  | { Description = Some descr; Author = Some author; Link = Some link; 
      Tags = Some tags } when not (String.IsNullOrWhiteSpace(tags)) ->
      let tags = tags.Split(',')
      Data.insertSnippet 
        { ID = id'; Title = form.Title; Comment = descr; 
          Author = author; Link = link; Date = System.DateTime.UtcNow;
          Likes = 0; Private = false; Passcode = ""; 
          References = nugetReferences; Source = ""; Versions = snippetInfo.Versions + 1; 
          Tags = tags }
        form.Code html
  | _ -> 
      failwith "Invalid input!"
  Redirection.FOUND ("/" + mangledId)

let updateSnippet id ctx = async {
  let id' = demangleId id
  match Seq.tryFind (fun s -> s.ID = id') publicSnippets with
  | Some snippetInfo ->
    if ctx.request.form |> Seq.exists (function "submit", _ -> true | _ -> false) then
      return! handlePost snippetInfo ctx.request.form id id' ctx
    else
      return! showForm snippetInfo id id' ctx
  | None -> return! invalidSnippetId id ctx }

let webPart = pathWithId "/%s/update" updateSnippet