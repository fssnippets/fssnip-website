module FsSnip.Data

open System
open System.IO
open FSharp.Azure.StorageTypeProvider
open Microsoft.WindowsAzure.Storage.Blob
open FsSnip.Utils
open FsSnip.Storage
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
    if Storage.Azure.isConfigured() then Storage.Azure.functions
    else Storage.Local.functions

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
    Versions = s.Versions; Tags = (s.DisplayTags |> Array.map(fun t -> t.ToLowerInvariant())) }

let private saveSnippet (s:Snippet) =
  Index.Snippet
    ( s.ID, s.Title, s.Comment, s.Author, s.Link, s.Date, s.Likes, s.Private,
      s.Passcode, Array.ofSeq s.References, s.Source, s.Versions, Array.ofSeq s.Tags, Array.ofSeq s.Tags )

let readSnippets () =
  let index = Index.Parse(Storage.readIndex ())
  let snippets = index.Snippets |> Seq.map readSnippet |> List.ofSeq
  snippets, snippets |> Seq.filter (fun s -> not s.Private)

let mutable snippets, publicSnippets = readSnippets ()

let loadSnippetInternal folder id revision = 
  let id = demangleId id
  snippets
  |> Seq.tryFind (fun s -> s.ID = id)
  |> Option.bind (fun snippetInfo ->
      let r = match revision with Latest -> snippetInfo.Versions - 1 | Revision r -> r
      Storage.readFile (sprintf "%s/%d/%d" folder id r) )

let loadSnippet id revision = 
  loadSnippetInternal "formatted" id revision 

let loadRawSnippet id revision =
  loadSnippetInternal "source" id revision 

let getAllPublicSnippets () =
  publicSnippets

let getNextId () = 
  let largest = snippets |> Seq.map (fun s -> s.ID) |> Seq.max
  largest + 1


let insertSnippet newSnippet source formatted =
  let index = Index.Parse(Storage.readIndex())
  let _, otherSnippets = index.Snippets |> Array.partition (fun snippet -> snippet.Id = newSnippet.ID)
  let json = Index.Root(Array.append otherSnippets [| saveSnippet newSnippet |]).JsonValue.ToString()

  let version = newSnippet.Versions - 1
  Storage.writeFile (sprintf "source/%d/%d" newSnippet.ID version) source
  Storage.writeFile (sprintf "formatted/%d/%d" newSnippet.ID version) formatted
  Storage.saveIndex json

  let newSnippets, newPublicSnippets = readSnippets ()
  snippets <- newSnippets
  publicSnippets <- newPublicSnippets


let likeSnippet id revision =
  let currentLikes = ref 0
  let index = Index.Parse(Storage.readIndex())
  let newSnippets = index.Snippets |> Array.map (fun snippet -> 
    if snippet.Id = id then 
      currentLikes := snippet.Likes + 1
      Index.Snippet
        ( snippet.Id, snippet.Title, snippet.Comment, snippet.Author, snippet.Link, snippet.Date, 
          !currentLikes, snippet.IsPrivate, snippet.Passcode, Array.ofSeq snippet.References, 
          snippet.Source, snippet.Versions, Array.ofSeq snippet.DisplayTags, Array.ofSeq snippet.EnteredTags )
    else snippet)
  let json = Index.Root(newSnippets).JsonValue.ToString()
  Storage.saveIndex json

  let newSnippets, newPublicSnippets = readSnippets ()
  snippets <- newSnippets
  publicSnippets <- newPublicSnippets
  !currentLikes
