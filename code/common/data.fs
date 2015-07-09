module FsSnip.Data

open System
open System.IO
open FsSnip.Utils
open FsSnip.BlobStorage
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

let indexFile = __SOURCE_DIRECTORY__ + "/../../data/index.json"

let readSnippets () =
  let index = Index.Load(indexFile)
  let snippets = index.Snippets |> Seq.map readSnippet |> List.ofSeq
  snippets, snippets |> Seq.filter (fun s -> not s.Private)

let mutable snippets, publicSnippets = readSnippets ()

let private loadSnippetInternal basePath id revision =
  let id' = demangleId id
  match Seq.tryFind (fun s -> s.ID = id') publicSnippets with
  | Some snippetInfo ->
      match revision with
      | Latest -> 
        let r = match revision with Latest -> snippetInfo.Versions - 1 | Revision r -> r
        Some (File.ReadAllText (sprintf "%s/%d" basePath r))
      | Revision r -> Some (File.ReadAllText(sprintf "%s/%d" basePath r))
  | None -> None

let loadSnippet id = 
  loadSnippetInternal (sprintf "%s/../../data/formatted/%d" __SOURCE_DIRECTORY__ (demangleId id)) id

let loadRawSnippet id =
  loadSnippetInternal (sprintf "%s/../../data/source/%d" __SOURCE_DIRECTORY__ (demangleId id)) id

let private loadSnippetAzureInternal container path id revision =
  let id' = demangleId id
  match Seq.tryFind (fun s -> s.ID = id') publicSnippets with
  | Some snippetInfo ->
      match revision with
      | Latest -> 
        let r = match revision with Latest -> snippetInfo.Versions - 1 | Revision r -> r
        ReadBlobText container (sprintf "%s/%d" path r)
      | Revision r -> ReadBlobText container (sprintf "%s/%d" path r)
  | None -> None

let loadSnippetAzure id = 
  loadSnippetAzureInternal "data" (sprintf "formatted/%d" (demangleId id)) id

let loadRawSnippetAzure id =
  loadSnippetAzureInternal "data" (sprintf "source/%d" (demangleId id)) id


let getNextId () = (snippets |> Seq.map (fun s -> s.ID) |> Seq.max) + 1

let insertSnippet newSnippet source formatted =
  let index = Index.Load(indexFile)
  let json = Index.Root(Array.append index.Snippets [| saveSnippet newSnippet |]).JsonValue.ToString()
  File.WriteAllText(indexFile, json)
  let id = newSnippet.ID
  let rawPath = sprintf "%s/../../data/source/%d" __SOURCE_DIRECTORY__ id
  let formattedPath = sprintf "%s/../../data/formatted/%d" __SOURCE_DIRECTORY__ id
  Directory.CreateDirectory(rawPath) |> ignore
  Directory.CreateDirectory(formattedPath) |> ignore

  File.WriteAllText(sprintf "%s/%d" rawPath (newSnippet.Versions - 1), source)
  File.WriteAllText(sprintf "%s/%d" formattedPath (newSnippet.Versions - 1), formatted)

  let newSnippets, newPublicSnippets  = readSnippets ()
  snippets <- newSnippets
  publicSnippets <- newPublicSnippets
