module FsSnip.Pages.Author

open Suave
open System
open System.Web
open FsSnip.Utils
open FsSnip.Data

// -------------------------------------------------------------------------------------------------
// Author page - domain model
// -------------------------------------------------------------------------------------------------

type AuthorModel =
  { Author : string
    Snippets : seq<Snippet> }

// -------------------------------------------------------------------------------------------------
// Loading author page information (snippets by the given author)
// -------------------------------------------------------------------------------------------------

let showSnippets (author) =
  let a = System.Web.HttpUtility.UrlDecode author
  publicSnippets
  |> Seq.filter (fun s -> s.Author.Equals(a, StringComparison.InvariantCultureIgnoreCase))
  |> (fun s ->
      { Author = a; Snippets = s})
  |> DotLiquid.page "author.html"
