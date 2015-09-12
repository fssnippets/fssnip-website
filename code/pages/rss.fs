module FsSnip.Pages.Rss

open Suave
open Suave.Http
open System
open FsSnip.Utils
open FsSnip.Data
open FsSnip.Rssfeed
open FsSnip.Filters

let getRss = delay (fun () ->
  let rssOutput = 
    publicSnippets
    |> Seq.sortBy (fun s -> DateTime.Now - s.Date)
    |> Seq.take 10
    |> Seq.map (fun s -> 
        { Title = s.Title; 
          Link = "http://fssnip.net/" + (formatId (s.ID)); 
          PubDate = s.Date.ToString("R"); 
          Author = s.Author; 
          Description = s.Comment })
    |> RssOutput "Recent F# snippets" "http://fssnip.net" "Provides links to all recently added public F# snippets." "en-US"
  Writers.setHeader "Content-Type" "application/rss+xml; charset=utf-8" >>= Successful.OK rssOutput)