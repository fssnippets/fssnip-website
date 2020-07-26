module FsSnip.Pages.Update

open System
open Suave
open FsSnip
open FsSnip.Data
open FsSnip.Utils
open FsSnip.Snippet

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
      |> DotLiquid.page "update.html"
  | None, None -> 
      showInvalidSnippet "Snippet not found" 
        (sprintf "The snippet '%s' that you were looking for was not found." mangledId)


/// Handle a post request with updated snippet data - update the DB or show error
let handlePost (snippetInfo:Data.Snippet) ctx mangledId id = async {

  // Parse the snippet & format it
  let form = Utils.readForm<UpdateForm> ctx.request.form
  let! valid = Recaptcha.validateRecaptcha ctx.request.form
  if not valid then 
      return! Recaptcha.recaptchaError ctx
  else

  let nugetReferences = Utils.parseNugetPackages form.NugetPkgs
  let doc = Parser.parseScript form.Session form.Code nugetReferences
  let html = Literate.writeHtmlToString "fs" true doc
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
  | p1, Some p2 when p1 <> p2 -> return! showForm newSnippetInfo (Some form.Code) mangledId "The entered password did not match!" ctx
  | p1, None when not (String.IsNullOrEmpty p1) -> return! showForm newSnippetInfo (Some form.Code) mangledId "This snippet is password-protected. Please enter a password!" ctx
  | p1, Some _ when String.IsNullOrEmpty p1 -> return! showForm newSnippetInfo (Some form.Code) mangledId "This snippet is not password-protected. Password is not needed!" ctx
  | _ ->

  // Check that snippet is private or has all required data
  match snippetInfo.Private, form, Array.isEmpty form.Tags with
  | false, { Description = Some _; Author = Some _ }, false  
  | true, _, _ ->
      Data.insertSnippet newSnippetInfo form.Code html
      return! Redirection.FOUND ("/" + mangledId) ctx
  | _ ->
      return! showForm snippetInfo (Some form.Code) mangledId "Some of the inputs were not valid." ctx }


/// Generate the form (on the first visit) or insert snippet (on a subsequent visit)
let updateSnippet mangledId ctx = async {
  let id = demangleId mangledId
  match Seq.tryFind (fun s -> s.ID = id) snippets with
  | Some snippetInfo ->
    if ctx.request.form |> Seq.exists (function "submit", _ -> true | _ -> false) then
      return! handlePost snippetInfo ctx mangledId id
    else
      return! showForm snippetInfo None mangledId "" ctx 
  | None -> 
      let details = (sprintf "The snippet '%s' that you were looking for was not found." mangledId)
      return! showInvalidSnippet "Snippet not found" details ctx }

let webPart = pathWithId "/%s/update" updateSnippet
