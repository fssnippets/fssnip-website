module FsSnip.Pages.Rss

open Suave
open Suave.Http
open System
open FsSnip.Utils
open FsSnip.Data
open FsSnip.Rssfeed
open FsSnip.Filters

let getRecent () =
  publicSnippets
  |> Seq.sortBy (fun s -> DateTime.Now - s.Date)
  |> Seq.take 10

let getRss = fun (ctx: Types.HttpContext) -> async {
    let rssOutput = 
        getRecent()
        |> Seq.map (fun s -> {Title=s.Title; Link="http://fssnip.net/" + (formatId (s.ID)); PubDate=s.Date.ToString("R"); Author=s.Author; Description=s.Comment})
        |> RssOutput "Recent F# snippets" "http://fssnip.net" "Provides links to all recently added public F# snippets." "en-US"
    return! Successful.OK rssOutput ctx
    }