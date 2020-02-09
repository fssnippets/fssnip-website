module FsSnip.Utils

open System
open Microsoft.FSharp.Reflection
open FSharp.CodeFormat
open Suave
open Suave.Http
open Suave.Filters
open System.Text
open System.Security.Cryptography

// -------------------------------------------------------------------------------------------------
// Helpers for working with fssnip IDs, formatting F# code and for various Suave things
// -------------------------------------------------------------------------------------------------

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

let private sha = new SHA1Managed()

/// Returns SHA1 hash of a given string formatted as Base64 string
let sha1Hash (password:string) = 
  if String.IsNullOrEmpty password then ""
  else Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(password)))

/// Returns None if the passcode is empty or Some with the hash
let tryGetHashedPasscode (p:string) =
  if String.IsNullOrWhiteSpace(p) then None 
  else Some(sha1Hash p)

/// Creates a web part from a function (to enable lazy computation)
let delay (f:unit -> WebPart) ctx = 
  async { return! f () ctx }

/// Cleanup url for title: "concurrent memoization" -> concurrent-memoization
let generateCleanTitle title = 
    System.Net.WebUtility.UrlEncode(
        System.Text.RegularExpressions.Regex.Replace(
            System.Text.RegularExpressions.Regex.Replace(title, "[^a-zA-Z0-9 ]", ""), 
            " +", "-"))

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

/// Converts an array of string values to the specified type (for use when parsing form data)
/// (supports `string option`, `bool`, `string` and `string[]` at the moment) 
let private convert (ty:System.Type) (strs:string[]) = 
  let single() = 
    if strs.Length = 1 then strs.[0]
    else failwith "Got multiple values!"
  if ty = typeof<string option> then 
    box (if System.String.IsNullOrWhiteSpace(single()) then None else Some(single()))
  elif ty = typeof<bool> then 
    box (if single() = "on" then true else false)
  elif ty = typeof<string> then 
    box (single())
  elif ty = typeof<string[]> then 
    box (strs)
  else failwithf "Could not covert '%A' to '%s'" strs ty.Name

/// Returns a default value when the type has a sensible default or throws
let private getDefaultValue (ty:System.Type) =
  if ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<option<_>> then null
  elif ty = typeof<bool> then box false
  elif ty.IsArray then box (Array.CreateInstance(ty.GetElementType(), [| 0 |]))
  else failwithf "Could not get value of type '%s'" ty.Name

/// Read data from a form into an F# record
let readForm<'T> (form:list<string*string option>) = 
  let lookup = 
    [ for k, v in form do
        match v with Some v -> yield k.ToLower(), v | _ -> () ] 
    |> Seq.groupBy fst 
    |> Seq.map (fun (k, vs) -> k, Seq.map snd vs)
    |> dict
  let values =
    [| for pi in FSharpType.GetRecordFields(typeof<'T>) ->
         match lookup.TryGetValue(pi.Name.ToLower()) with
         | true, vs -> convert pi.PropertyType (Array.ofSeq vs)
         | _ -> getDefaultValue pi.PropertyType |]
  FSharpValue.MakeRecord(typeof<'T>, values) :?> 'T

/// Converts comma-separated string with NuGet package names to list of strings
let parseNugetPackages = function
  | Some s when not (String.IsNullOrWhiteSpace(s)) ->
    s.Split([|","|], StringSplitOptions.RemoveEmptyEntries)
  |> Array.map (fun s -> s.Trim())
  | _ -> [| |]


type Environment with
  static member GetEnvironmentVariable(variable : string, defaultValue : string) =
    match Environment.GetEnvironmentVariable variable with
    | null -> defaultValue
    | value -> value