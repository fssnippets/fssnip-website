module FsSnip.Pages.Author

open Suave
open Suave.Http
open Suave.Http.Applicatives
open System
open System.Web
open FsSnip.Utils
open FsSnip.Data

// -------------------------------------------------------------------------------------------------
// Author page - domain model
// -------------------------------------------------------------------------------------------------

type AuthorLink = 
  { Text : string
    Link : string
    Size : int 
    Count : int }

type AuthorLinks = seq<AuthorLink>

type AuthorModel =
  { Author : string
    Snippets : seq<Snippet> }

type AllAuthorsModel =
  { Authors: AuthorLinks}


let getAllAuthors () = 
    let links = 
      publicSnippets
      |> Seq.map (fun s -> s.Author)
      |> Seq.countBy id
      |> Seq.sortBy (fun (_, c) -> -c)
      |> Seq.withSizeBy snd
      |> Seq.map (fun ((n,c),s) -> 
          { Text = n; Size = 80 + s; Count = c;
            Link = HttpUtility.UrlEncode(n) })
    { Authors = links }

// -------------------------------------------------------------------------------------------------
// Suave web parts
// -------------------------------------------------------------------------------------------------

// Loading author page information (snippets by the given author)
let showSnippets (author) = 
    let a = System.Web.HttpUtility.UrlDecode author
    let fromAuthor (s:Snippet) = 
        s.Author.Equals(a, StringComparison.InvariantCultureIgnoreCase)
    let ss = Seq.filter fromAuthor publicSnippets
    DotLiquid.page "author.html" { Author = a
                                   Snippets = ss }

// Loading author page information (all authors)
let showAll = delay (fun () -> 
  DotLiquid.page "authors.html" (getAllAuthors()))

// Composed web part to be included in the top-level route
let webPart =   
  choose 
    [ path "/authors/" >>= showAll
      pathScan "/authors/%s" showSnippets ]
