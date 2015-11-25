module FsSnip.Pages.Search

open Suave
open System
open System.Web
open FsSnip.Utils
open FsSnip.Data

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

let getResults (query) =
  publicSnippets
  |> Seq.filter (fun s -> [ s.Title ; s.Comment ] |> Seq.exists (fun x -> x.Contains(query)))

let showResults (query) = delay (fun () -> 
  let decodedQuery = FsSnip.Filters.urlDecode query
  let results = getResults decodedQuery |> Seq.toList
  DotLiquid.page "search.html" { Query = decodedQuery
                                 Results = results
                                 Count = (List.length results) })