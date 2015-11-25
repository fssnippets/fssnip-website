module FsSnip.Utils

open System
open Suave.Http.Applicatives
open Microsoft.FSharp.Reflection
open Suave.Http
open FSharp.CodeFormat

// -------------------------------------------------------------------------------------------------
// Helpers for working with fssnip IDs, formatting F# code and for various Suave things
// -------------------------------------------------------------------------------------------------

// Global instance of code formatting agent from F# Formatting
let formatAgent = CodeFormat.CreateAgent()

// This is the alphabet used in the IDs
let alphabet = [ '0' .. '9' ] @ [ 'a' .. 'z' ] @ [ 'A' .. 'Z' ] |> Array.ofList
let alphabetMap = Seq.zip alphabet [ 0 .. alphabet.Length - 1 ] |> dict

/// Generate mangled name to be used as part of the URL
let mangleId i =
  let rec mangle acc = function
    | 0 -> new String(acc |> Array.ofList)
    | n -> let d, r = Math.DivRem(n, alphabet.Length)
           mangle (alphabet.[r]::acc) d
  mangle [] i

/// Translate mangled URL name to a numeric snippet ID
let demangleId (str:string) =
  let rec demangle acc = function
    | [] -> acc
    | x::xs ->
      let v = alphabetMap.[x]
      demangle (acc * alphabet.Length + v) xs
  demangle 0 (str |> List.ofSeq)

/// Web part that succeeds when the specified string is a valid FsSnip ID
let pathWithId pf f =
  pathScan pf (fun id ctx -> async {
    if Seq.forall (alphabetMap.ContainsKey) id then
      return! f id ctx
    else return None } )

/// Creates a web part from a function (to enable lazy computation)
let delay (f:unit -> Suave.Types.WebPart) ctx = 
  async { return! f () ctx }

module Seq = 
  /// Take the number of elements specified by `take`, then shuffle the
  /// rest of the items and then take just the `top` number of elements
  let takeShuffled take top snips = 
    let rnd = Random()
    snips
    |> Seq.take take
    |> Seq.sortBy (fun _ -> rnd.NextDouble())
    |> Seq.take top

  /// Given a sequence of items, return item together with its relative
  /// size (as percentage). The specified function `f` returns the "size".
  /// We assume that the smallest size is zero.
  let withSizeBy f snips =
    let snips = snips |> List.ofSeq
    let max = snips |> Seq.map f |> Seq.max
    snips |> Seq.map (fun s -> s, (f s) * 100 / max)


let private convert (ty:System.Type) str = 
  if ty = typeof<string option> then 
    box (if System.String.IsNullOrWhiteSpace(str) then None else Some str)
  elif ty = typeof<bool> then 
    box (if str = "on" then true else false)
  elif ty = typeof<string> then 
    box str
  else failwithf "Could not covert '%s' to '%s'" str ty.Name

let private getDefaultValue (ty:System.Type) =
  if ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<option<_>> then null
  elif ty = typeof<bool> then box false
  else failwithf "Could not get value of type '%s'" ty.Name

/// Read data from a form into an F# record
let readForm<'T> (form:list<string*string option>) = 
  let lookup = dict [ for k, v in form -> k.ToLower(), v ]
  let values =
    [| for pi in FSharpType.GetRecordFields(typeof<'T>) ->
         match lookup.TryGetValue(pi.Name.ToLower()) with
         | true, Some v -> convert pi.PropertyType v
         | _ -> getDefaultValue pi.PropertyType |]
  FSharpValue.MakeRecord(typeof<'T>, values) :?> 'T
  
let invalidSnippetId id =
  RequestErrors.NOT_FOUND (sprintf "Snippet with id %s not found" id)
