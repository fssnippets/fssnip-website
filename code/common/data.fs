module FsSnip.Data

open System
open System.IO
open FSharp.Azure.StorageTypeProvider
open Microsoft.WindowsAzure.Storage.Blob
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
let [<Literal>] AzureConnectionString = "UseDevelopmentStorage=true"
let indexFile = __SOURCE_DIRECTORY__ + "/../../data/index.json"

type Index = JsonProvider<Index>
type Azure = AzureTypeProvider<AzureConnectionString>

type StorageProvider =
    | LocalFilesystem
    | AzureBlobStorage

type SnippetVersion =
    | Latest
    | Revision of int

type Snippet =
  { ID : int; Title : string; Comment : string; Author : string;
    Link : string; Date : DateTime; Likes : int; Private : bool;
    Passcode : string; References : seq<string>; Source : string;
    Versions: int; Tags : seq<string> }

// select the storage provider here...
let storageProvider = AzureBlobStorage // or LocalFileSystem

//=======
// Private Filesystem functions
//=======
let private readSnippet (s:Index.Snippet) =
  { ID = s.Id; Title = s.Title; Comment = s.Comment; Author = s.Author;
    Link = s.Link; Date = s.Date; Likes = s.Likes; Private = s.IsPrivate;
    Passcode = s.Passcode; References = s.References; Source = s.Source;
    Versions = s.Versions; Tags = s.Tags }

let private saveSnippet (s:Snippet) =
  Index.Snippet
    ( s.ID, s.Title, s.Comment, s.Author, s.Link, s.Date, s.Likes, s.Private,
      s.Passcode, Array.ofSeq s.References, s.Source, s.Versions, Array.ofSeq s.Tags )

let private readBlobText containerName blobPath =
    let container = Azure.Containers.CloudBlobClient.GetContainerReference(containerName)
    if container.Exists() then
        let blob = container.GetBlockBlobReference(blobPath)
        if blob.Exists() then
            blob.DownloadText(System.Text.Encoding.UTF8)
        else failwith "blob not found"
    else failwith "container not found"

let private readBlobStream containerName blobPath =
    let stream = new MemoryStream();
    let container = Azure.Containers.CloudBlobClient.GetContainerReference(containerName)
    let blob = container.GetBlockBlobReference(blobPath)
    if blob.Exists() then
        blob.DownloadToStream(stream)
    else failwith "blob not found"
    stream

let private writeBlobText containerName blobPath text = 
    let container = Azure.Containers.CloudBlobClient.GetContainerReference(containerName)
    if container.Exists() then
        let blob = container.GetBlockBlobReference(blobPath)
        blob.UploadText(text, System.Text.Encoding.UTF8)
    else failwith (sprintf "container not found %s" containerName)

let readSnippets () =
  let index =
    match storageProvider with
    | LocalFilesystem -> 
      Index.Load(indexFile)    
    | AzureBlobStorage ->
      let text = readBlobText "data" "index.json"
      Index.Parse(text)
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

let private loadSnippetAzureInternal container path id revision =
  let id' = demangleId id
  match Seq.tryFind (fun s -> s.ID = id') publicSnippets with
  | Some snippetInfo ->
      match revision with
      | Latest -> 
        let r = match revision with Latest -> snippetInfo.Versions - 1 | Revision r -> r
        Some (readBlobText container (sprintf "%s/%d" path r))
      | Revision r -> 
        Some (readBlobText container (sprintf "%s/%d" path r))
  | None -> None

let loadSnippet id = 
  match storageProvider with
  | LocalFilesystem ->
    loadSnippetInternal (sprintf "%s/../../data/formatted/%d" __SOURCE_DIRECTORY__ (demangleId id)) id
  | AzureBlobStorage ->
    loadSnippetAzureInternal "data" (sprintf "formatted/%d" (demangleId id)) id

let loadRawSnippet id =
  match storageProvider with
  | LocalFilesystem ->
    loadSnippetInternal (sprintf "%s/../../data/source/%d" __SOURCE_DIRECTORY__ (demangleId id)) id
  | AzureBlobStorage ->
    loadSnippetAzureInternal "data" (sprintf "source/%d" (demangleId id)) id

let getNextId () = (snippets |> Seq.map (fun s -> s.ID) |> Seq.max) + 1

let private insertSnippetLocal newSnippet source formatted =
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

let private insertSnippetAzure newSnippet source formatted =
  let text = readBlobText "data" "index.json"
  let index = Index.Parse(text)
  let json = Index.Root(Array.append index.Snippets [| saveSnippet newSnippet |]).JsonValue.ToString()
  writeBlobText "data" "index.json" json
  let id = newSnippet.ID
  let rawPath = sprintf "source/%d" id
  let formattedPath = sprintf "formatted/%d" id
  writeBlobText "data" (sprintf "%s/%d" rawPath (newSnippet.Versions - 1)) source
  writeBlobText "data" (sprintf "%s/%d" formattedPath (newSnippet.Versions - 1)) formatted
  let newSnippets, newPublicSnippets  = readSnippets ()

  snippets <- newSnippets
  publicSnippets <- newPublicSnippets

let insertSnippet newSnippet source formatted =
  match storageProvider with
  | LocalFilesystem ->
    insertSnippetLocal newSnippet source formatted
  | AzureBlobStorage ->
    insertSnippetAzure newSnippet source formatted

