module FsSnip.Pages.Like

open Suave
open FsSnip
open FsSnip.Data
open FsSnip.Utils
open FsSnip.Snippet

// -------------------------------------------------------------------------------------------------
// Incrementing the number of likes when called from the client-side
// -------------------------------------------------------------------------------------------------

let likeSnippet id r =
  let id' = demangleId id
  match Seq.tryFind (fun s -> s.ID = id') snippets with
  | Some snippetInfo -> 
      let newLikes = Data.likeSnippet id' r
      Successful.OK (newLikes.ToString())
  | None -> showInvalidSnippet "Snippet not found" (sprintf "The snippet '%s' that you were looking for was not found." id)
    
let webPart = 
  pathWithId "/like/%s" (fun id -> likeSnippet id Latest)    