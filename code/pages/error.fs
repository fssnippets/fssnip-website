module FsSnip.Error

open FsSnip
open FsSnip.Data
open FsSnip.Utils
open Suave
open Suave.Operators
open Suave.Filters

// -------------------------------------------------------------------------------------------------
// Basic error page for showing error with title & details
// -------------------------------------------------------------------------------------------------

type Error =
  { Title : string
    Details : string }

let reportError code title details ctx = async {
  let! result = 
    DotLiquid.page "error.html"
      { Title = title; Details = details } ctx
  return result |> Option.map (fun res ->
    { res with response = { res.response with status = code }}) }