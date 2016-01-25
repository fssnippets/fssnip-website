module FsSnip.Pages.Update

open System
open Suave
open FsSnip
open FSharp.Literate
open FsSnip.Data
open FsSnip.Utils
open FsSnip.Snippet
open FSharp.CodeFormat

// -------------------------------------------------------------------------------------------------
// Updates an existing snippet with a new data (provided the passcode matches)
// -------------------------------------------------------------------------------------------------

type RawSnippet =
  { Raw : string
    Details : Data.Snippet
    Revision : int
    Session: string 
    Error : string }

type UpdateForm =
  { Title : string
    Passcode : string option
    Description : string option
    Tags : string[]
    Author : string option
    Link : string
    Code : string
    NugetPkgs : string option
    Session : string }

/// Displays the update form when the user comes to the page for the first time
let showForm snippetInfo source mangledId error =
  match source, Data.loadRawSnippet mangledId Latest with
  | Some snippet, _
  | None, Some snippet ->
      let rev = snippetInfo.Versions - 1
      { Raw = snippet
        Details = snippetInfo
        Revision = rev
        Session = Guid.NewGuid().ToString() 
        Error = error }
      |> DotLiquid.page<RawSnippet> "update.html"
  | None, None -> 
      showInvalidSnippet "Snippet not found" 
        (sprintf "The snippet '%s' that you were looking for was not found." mangledId)


/// Handle a post request with updated snippet data - update the DB or show error
let handlePost (snippetInfo:Data.Snippet) requestForm mangledId id =

  // Parse the snippet & format it
  let form = Utils.readForm<UpdateForm> requestForm
  let nugetReferences = Utils.parseNugetPackages form.NugetPkgs
  let doc = Parser.parseScript form.Session form.Code nugetReferences
  let html = Literate.WriteHtml(doc, "fs", true, true)
  Parser.completeSession form.Session
  let newSnippetInfo = 
    { ID = id; Title = form.Title; Comment = defaultArg form.Description "";
      Author = defaultArg form.Author ""; Link = form.Link; Date = System.DateTime.UtcNow;
      Likes = 0; Private = snippetInfo.Private; Passcode = snippetInfo.Passcode; 
      References = nugetReferences; Source = ""; Versions = snippetInfo.Versions + 1;
      Tags = form.Tags }

  // Check the password if there is one
  let existingPass = snippetInfo.Passcode
  let enteredPass = form.Passcode |> Option.bind tryGetHashedPasscode 
  match existingPass, enteredPass with
  | p1, Some p2 when p1 <> p2 -> showForm newSnippetInfo (Some form.Code) mangledId "The entered password did not match!"
  | p1, None when not (String.IsNullOrEmpty p1) -> showForm newSnippetInfo (Some form.Code) mangledId "This snippet is password-protected. Please enter a password!"
  | p1, Some _ when String.IsNullOrEmpty p1 -> showForm newSnippetInfo (Some form.Code) mangledId "This snippet is not password-protected. Password is not needed!"
  | _ ->

  // Check that snippet is private or has all required data
  match snippetInfo.Private, form, Array.isEmpty form.Tags with
  | false, { Description = Some _; Author = Some _ }, false  
  | true, _, _ ->
      Data.insertSnippet newSnippetInfo form.Code html
      Redirection.FOUND ("/" + mangledId)
  | _ ->
      showForm snippetInfo (Some form.Code) mangledId "Some of the inputs were not valid."


/// Generate the form (on the first visit) or insert snippet (on a subsequent visit)
let updateSnippet mangledId = request (fun req ->
  let id = demangleId mangledId
  match Seq.tryFind (fun s -> s.ID = id) snippets with
  | Some snippetInfo ->
    if req.form |> Seq.exists (function "submit", _ -> true | _ -> false) then
      handlePost snippetInfo req.form mangledId id
    else
      showForm snippetInfo None mangledId ""
  | None -> showInvalidSnippet "Snippet not found" (sprintf "The snippet '%s' that you were looking for was not found." mangledId) )

let webPart = pathWithId "/%s/update" updateSnippet
