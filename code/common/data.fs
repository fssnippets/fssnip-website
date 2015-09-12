module FsSnip.Data

open System
open System.IO
open FSharp.Azure.StorageTypeProvider
open Microsoft.WindowsAzure.Storage.Blob
open FsSnip.Utils
open FsSnip.Data
open FSharp.Data

// -------------------------------------------------------------------------------------------------
// Data access. We keep an in-memory index (saved as JSON) with all the meta-data about snippets.
// When something changes (someone likes a snippet or a new snippet is added), we update this and
// save it back. Aside from that, we keep the source, parsed and formatted snippets as blobs.
// -------------------------------------------------------------------------------------------------

/// The storage can be either file system or Azure blob storage. This chooses Azure
/// (when it has the connection string) or file system (when running locally)
module Storage = 
  let readIndex, saveIndex, readFile, writeFile =
    if Azure.Storage.isConfigured() then Azure.Storage.functions
    else Local.Storage.functions


let [<Literal>] Index = __SOURCE_DIRECTORY__ + "/../samples/index.json"
type Index = JsonProvider<Index>

type SnippetVersion =
    | Latest
    | Revision of int

type Snippet =
  { ID : int; Title : string; Comment : string; Author : string;
    Link : string; Date : DateTime; Likes : int; Private : bool;
    Passcode : string; References : seq<string>; Source : string;
    Versions: int; Tags : seq<string> }

let private readSnippet (s:Index.Snippet) =
  { ID = s.Id; Title = s.Title; Comment = s.Comment; Author = s.Author;
    Link = s.Link; Date = s.Date; Likes = s.Likes; Private = s.IsPrivate;
    Passcode = s.Passcode; References = s.References; Source = s.Source;
    Versions = s.Versions; Tags = s.Tags }

let private saveSnippet (s:Snippet) =
  Index.Snippet
    ( s.ID, s.Title, s.Comment, s.Author, s.Link, s.Date, s.Likes, s.Private,
      s.Passcode, Array.ofSeq s.References, s.Source, s.Versions, Array.ofSeq s.Tags )

let readSnippets () =
  let index = Index.Parse(Storage.readIndex ())
  let snippets = index.Snippets |> Seq.map readSnippet |> List.ofSeq
  snippets, snippets |> Seq.filter (fun s -> not s.Private)

let mutable snippets, publicSnippets = readSnippets ()

let loadSnippetInternal folder id revision = 
  let id = demangleId id
  publicSnippets
  |> Seq.tryFind (fun s -> s.ID = id) 
  |> Option.map (fun snippetInfo ->
      let r = match revision with Latest -> snippetInfo.Versions - 1 | Revision r -> r
      Storage.readFile (sprintf "%s/%d/%d" folder id r) )

let loadSnippet id revision = 
  loadSnippetInternal "formatted" id revision 

let loadRawSnippet id revision =
  loadSnippetInternal "source" id revision 

let getNextId () = 
  let largest = snippets |> Seq.map (fun s -> s.ID) |> Seq.max
  largest + 1

let insertSnippet newSnippet source formatted =
  if newSnippet.Versions <> 1 then invalidOp "insertSnippet can only insert first version"
  let index = Index.Parse(Storage.readIndex())
  let json = Index.Root(Array.append index.Snippets [| saveSnippet newSnippet |]).JsonValue.ToString()
  Storage.writeFile (sprintf "source/%d/0" newSnippet.ID) source
  Storage.writeFile (sprintf "formatted/%d/0" newSnippet.ID) formatted
  Storage.saveIndex json

  let newSnippets, newPublicSnippets  = readSnippets ()
  snippets <- newSnippets
  publicSnippets <- newPublicSnippets
