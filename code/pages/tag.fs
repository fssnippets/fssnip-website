module FsSnip.Pages.Tag

open Suave
open System
open System.Web
open FsSnip.Utils
open FsSnip.Data

// -------------------------------------------------------------------------------------------------
// Tag page - domain model
// -------------------------------------------------------------------------------------------------

type TagModel =
  { Tag : string
    Snippets : seq<Snippet> }

// -------------------------------------------------------------------------------------------------
// Loading tag page information (snippets by the given tag)
// -------------------------------------------------------------------------------------------------

let showSnippets (tag) =
  let t = System.Web.HttpUtility.UrlDecode tag
  publicSnippets
  |> Seq.filter (fun s -> Seq.exists (fun t' -> t.Equals(t', StringComparison.InvariantCultureIgnoreCase)) s.Tags)
  |> (fun s ->
      { Tag = t; Snippets = s})
  |> DotLiquid.page "tag.html"
