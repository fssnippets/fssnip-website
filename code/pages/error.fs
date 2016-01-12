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

let reportError code title details ctx = 
  { Title = title; Details = details }
  |> DotLiquid.page "error. html" 
  >>= Writers.setStatus code