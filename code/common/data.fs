module FsSnip.Data

open System
open System.IO
open FsSnip.Utils
open FSharp.Data

// -------------------------------------------------------------------------------------------------
// Data access. We keep an in-memory index (saved as JSON) with all the meta-data about snippets.
// When something changes (someone likes a snippet or a new snippet is added), we update this and
// save it back. Aside from that, we keep the source, parsed and formatted snippets as blobs.
// -------------------------------------------------------------------------------------------------

// This is currently using a local file system, but it needs to be changed
// so that the files can be loaded from Azure blob storage (see issue #6)

let [<Literal>] Index = __SOURCE_DIRECTORY__ + "/../samples/index.json"
type Index = JsonProvider<Index>

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

let indexFile = __SOURCE_DIRECTORY__ + "/../../data/index.json"

let readSnippets () =
  let index = Index.Load(indexFile)
  let snippets = index.Snippets |> Seq.map readSnippet |> List.ofSeq
  snippets, snippets |> Seq.filter (fun s -> not s.Private)

let mutable snippets, publicSnippets = readSnippets ()

let loadSnippet id =
  File.ReadAllText(sprintf "%s/../../data/formatted/%d/0" __SOURCE_DIRECTORY__ (demangleId id))

let loadRawSnippet id =
  File.ReadAllText(sprintf "%s/../../data/source/%d/0" __SOURCE_DIRECTORY__ (demangleId id))

let getNextId () = (snippets |> Seq.map (fun s -> s.ID) |> Seq.max) + 1

let insertSnippet newSnippet source formatted =
  let index = Index.Load(indexFile)
  let json = Index.Root(Array.append index.Snippets [| saveSnippet newSnippet |]).JsonValue.ToString()
  File.WriteAllText(indexFile, json)
  let id = newSnippet.ID
  Directory.CreateDirectory(sprintf "%s/../../data/source/%d" __SOURCE_DIRECTORY__ id) |> ignore
  Directory.CreateDirectory(sprintf "%s/../../data/formatted/%d" __SOURCE_DIRECTORY__ id) |> ignore
  File.WriteAllText(sprintf "%s/../../data/source/%d/0" __SOURCE_DIRECTORY__ id, source)
  File.WriteAllText(sprintf "%s/../../data/formatted/%d/0" __SOURCE_DIRECTORY__ id, formatted)

  let newSnippets, newPublicSnippets  = readSnippets ()
  snippets <- newSnippets
  publicSnippets <- newPublicSnippets
