module FsSnip.Pages.Test

open System
open Suave
open FsSnip
open FSharp.Literate
open FsSnip.Data
open FsSnip.Utils
open FsSnip.Snippet
open FSharp.CodeFormat

// -------------------------------------------------------------------------------------------------
// Displays a test page for running the snippet
// -------------------------------------------------------------------------------------------------

type TestSnippet =
  { Encoded : string
    Details : Data.Snippet }

/// Displays the update form when the user comes to the page for the first time
let showForm snippetInfo source mangledId error =
  match source, Data.loadRawSnippet mangledId Latest with
  | Some snippet, _
  | None, Some snippet ->
      { Encoded = Web.HttpUtility.HtmlAttributeEncode(snippet)
        Details = snippetInfo }
      |> DotLiquid.page "test.html"
  | None, None -> 
      showInvalidSnippet "Snippet not found" 
        (sprintf "The snippet '%s' that you were looking for was not found." mangledId)

/// Generate the form (on the first visit) or insert snippet (on a subsequent visit)
let testSnippet mangledId ctx = async {
  let id = demangleId mangledId
  match Seq.tryFind (fun s -> s.ID = id) snippets with
  | Some snippetInfo ->
      return! showForm snippetInfo None mangledId "" ctx 
  | None -> 
      let details = (sprintf "The snippet '%s' that you were looking for was not found." mangledId)
      return! showInvalidSnippet "Snippet not found" details ctx }

let webPart = pathWithId "/%s/test" testSnippet
