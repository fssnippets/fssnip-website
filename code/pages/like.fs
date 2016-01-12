module FsSnip.Pages.Like

open FsSnip
open FsSnip.Data
open FsSnip.Utils
open Suave

// -------------------------------------------------------------------------------------------------
// Incrementing the number of likes when called from the client-side
// -------------------------------------------------------------------------------------------------

let likeSnippet id r =
  let id' = demangleId id
  match Seq.tryFind (fun s -> s.ID = id') snippets with
  | Some snippetInfo -> 
      let newLikes = Data.likeSnippet id' r
      Successful.OK (newLikes.ToString())
  | None -> invalidSnippetId (id'.ToString())
    
let webPart = 
  pathWithId "/like/%s" (fun id -> likeSnippet id Latest)    