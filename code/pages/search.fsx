module FsSnip.Pages.Search
#load "../packages.fsx" "../common/common.fsx"

open Suave
open System
open System.Web
open FsSnip.Utils
open FsSnip.Data
open Suave.Filters

// -------------------------------------------------------------------------------------------------
// Search results page - domain model
// -------------------------------------------------------------------------------------------------

type Results = 
  { Query : string
    Count : int
    Results : Snippet list }

// -------------------------------------------------------------------------------------------------
// Loading search results
// -------------------------------------------------------------------------------------------------

let getResults (query:string) =
  publicSnippets
  |> Seq.filter (fun s -> 
    [ s.Title ; s.Comment; s.Title.ToLowerInvariant() ; s.Comment.ToLowerInvariant() ]
    |> Seq.exists (fun x -> x.Contains(query) ))

let showResults (query) = delay (fun () -> 
  let decodedQuery = FsSnip.Filters.urlDecode query
  let results = getResults decodedQuery |> Seq.toList
  { Query = decodedQuery
    Results = results
    Count = (List.length results) } |> DotLiquid.page "search.html")

let webPart = pathScan "/search/%s" showResults                                 
