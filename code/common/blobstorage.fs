module FsSnip.BlobStorage

// -------------------------------------------------------------------------------------------------
// Azure Blob Storage Functions
// -------------------------------------------------------------------------------------------------

open FSharp.Azure.StorageTypeProvider
open Microsoft.WindowsAzure.Storage.Blob
open System.IO

//============================================
// Initialize Azure Storage from example data
//=============================================
type Azure = AzureTypeProvider<"UseDevelopmentStorage=true">

let initializeStorage () =
    let container = Azure.Containers.CloudBlobClient.GetContainerReference("data")
    container.CreateIfNotExists() |> ignore
    let permissions = new BlobContainerPermissions()
    permissions.PublicAccess <- BlobContainerPublicAccessType.Blob
    let formatted = container.GetDirectoryReference("formatted")
    let source = container.GetDirectoryReference("source")
    ()

let files (directory: Microsoft.WindowsAzure.Storage.Blob.CloudBlobDirectory) path =
    Directory.EnumerateFiles(path)
    |> Seq.map (fun filepath -> async {
        let file = filepath.Substring(path.Length + 1)
        let blob = directory.GetBlockBlobReference(file)
        let task = blob.UploadFromFileAsync(Path.Combine(path, file), FileMode.Open)
        do! task |> Async.AwaitIAsyncResult |> Async.Ignore
        })
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore


let folders path dirName (containerName: string) =
    let container = Azure.Containers.CloudBlobClient.GetContainerReference(containerName)
    Directory.EnumerateDirectories(path)
    |> Seq.take 5
    |> Seq.map (fun dirPath ->
        let dir = dirPath.Substring(path.Length + 1)
        (Path.Combine(path, dir), container.GetDirectoryReference(dirName + "/" + dir))
        )
    |> Seq.map (fun (p, c) -> files c p)
    |> Seq.toList

let uploadLocalFoldersToAzure path =
    folders (Path.Combine(path, "source")) "source" "data" |> ignore
    folders (Path.Combine(path, "formatted")) "formatted" "data" |> ignore

//============================================
// Data functions
//=============================================

let ReadBlobText containerName blobPath =
    let container = Azure.Containers.CloudBlobClient.GetContainerReference(containerName)
    if container.Exists() then
        let blob = container.GetBlockBlobReference(blobPath)
        if blob.Exists() then
            Some(blob.DownloadText(System.Text.Encoding.UTF8))
        else None
    else None

let ReadBlobStream containerName blobPath =
    let stream = new MemoryStream();
    let container = Azure.Containers.CloudBlobClient.GetContainerReference(containerName)
    let blob = container.GetBlockBlobReference(blobPath)
    if blob.Exists() then
        blob.DownloadToStream(stream)
    stream

let WriteBlobText containerName blobPath text = 
    let container = Azure.Containers.CloudBlobClient.GetContainerReference(containerName)
    if container.Exists() then
        let blob = container.GetBlockBlobReference(blobPath)
        blob.UploadText(text, System.Text.Encoding.UTF8)
