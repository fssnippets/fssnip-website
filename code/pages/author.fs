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
    let fromAuthor (s:Snippet) = 
        s.Author.Equals(a, StringComparison.InvariantCultureIgnoreCase)
    let ss = Seq.filter fromAuthor publicSnippets
    DotLiquid.page "author.html" { Author = a
                                   Snippets = ss }