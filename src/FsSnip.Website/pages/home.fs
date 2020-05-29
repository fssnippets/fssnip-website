module FsSnip.Pages.Home

open Suave
open System
open System.Web
open FsSnip.Utils
open FsSnip.Data
open Suave.Filters
open Suave.Operators

// -------------------------------------------------------------------------------------------------
// Home page - domain model
// -------------------------------------------------------------------------------------------------

type HomeLink = 
  { Text : string
    Link : string
    Size : int 
    Count : int }

type Home =
  { Recent : seq<Snippet> 
    Popular : seq<Snippet>
    Tags : seq<HomeLink>
    Authors : seq<HomeLink>
    TotalCount : int
    PublicCount : int }

// -------------------------------------------------------------------------------------------------
// Loading home page information (recent, popular, authors, tags, etc.)
// -------------------------------------------------------------------------------------------------

let getRecent () =
  publicSnippets
  |> Seq.sortBy (fun s -> DateTime.Now - s.Date)
  |> Seq.take 6

let getPopular () =
  let rnd = Random()
  publicSnippets
  |> Seq.sortBy (fun s -> -s.Likes)
  |> Seq.takeShuffled 40 6

let getShuffledLinksByCount take top (names:seq<string>) =
  names
  |> Seq.countBy id
  |> Seq.sortBy (fun (_, c) -> -c)
  |> Seq.takeShuffled take top
  |> Seq.withSizeBy snd
  |> Seq.map (fun ((n,c),s) -> 
      { Text = n; Size = 80 + s; Count = c;
        Link = System.Net.WebUtility.UrlEncode(n) })

let getAuthors () = 
  publicSnippets
  |> Seq.map (fun s -> s.Author)
  |> getShuffledLinksByCount 30 20

let getTags () = 
  publicSnippets
  |> Seq.collect (fun s -> s.Tags)
  |> getShuffledLinksByCount 40 20

let getHome () = 
  { Recent = getRecent(); Popular = getPopular() 
    TotalCount = Seq.length snippets
    PublicCount = Seq.length publicSnippets
    Tags = getTags(); Authors = getAuthors() }

let showHome = delay (fun () -> 
  DotLiquid.page "home.html" (getHome()))
  
let webPart = path "/" >=> showHome