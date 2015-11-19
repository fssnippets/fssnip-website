module FsSnip.Pages.Like

open Suave.Http
open FsSnip
open FsSnip.Data
open FsSnip.Utils
open FsSnip.Pages.Snippet

let likeSnippet id r =
    let id' = demangleId id
    match Seq.tryFind (fun s -> s.ID = id') publicSnippets with
    | Some snippetInfo -> 
        let newLikes = Data.likeSnippet id' r
        Successful.OK (newLikes.ToString())
    | None -> invalidSnippetId (id'.ToString())