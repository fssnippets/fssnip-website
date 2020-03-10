module FsSnip.Filters

open FsSnip.Utils
open System

// -------------------------------------------------------------------------------------------------
// Filters that can be used in DotLiquid templates
// -------------------------------------------------------------------------------------------------

let formatId (id:int) =
  mangleId id

let cleanTitle (title:string) = 
  generateCleanTitle title

let urlEncode (url:string) =
  System.Web.HttpUtility.UrlEncode(url)

let urlDecode (input:string) =
  System.Web.HttpUtility.UrlDecode(input)

let niceDate (dt:DateTime) =
  let print (elapsed:float) timeUnit =
    // truncate to int
    let count = int elapsed
    // pluralize (conveniently, all of our units are pluralized with "s")
    let suffix = if count = 1 then "" else "s"
    sprintf "%d %s%s ago" count timeUnit suffix
  let ts = DateTime.UtcNow - dt
  if ts.TotalSeconds < 60.0 then print ts.TotalSeconds "second"
  elif ts.TotalMinutes < 60.0 then print ts.TotalMinutes "minute"
  elif ts.TotalHours < 24.0 then print ts.TotalHours "hour"
  elif ts.TotalHours < 48.0 then "yesterday"
  elif ts.TotalDays < 30.0 then print ts.TotalDays "day"
  elif ts.TotalDays < 365.0 then print (ts.TotalDays / 30.0) "month"
  else print (ts.TotalDays / 365.0) "year"
