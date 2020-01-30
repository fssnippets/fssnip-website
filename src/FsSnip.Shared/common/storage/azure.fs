module FsSnip.Storage.Azure

open System
open System.IO
open Microsoft.WindowsAzure.Storage

// -------------------------------------------------------------------------------------------------
// Azure storage - the connection string is provided by environment variable, which is configured 
// in the Azure portal. The `functions` value should be compatible with one in `local.fs`
// -------------------------------------------------------------------------------------------------

let storageEnvVar = "CUSTOMCONNSTR_FSSNIP_STORAGE"

let isConfigured() = 
  Environment.GetEnvironmentVariable(storageEnvVar) <> null

let createCloudBlobClient() = 
  let account = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable(storageEnvVar))
  account.CreateCloudBlobClient()

let private readBlobText containerName blobPath =
    let container = createCloudBlobClient().GetContainerReference(containerName)
    if container.ExistsAsync().Result then
        let blob = container.GetBlockBlobReference(blobPath)
        if blob.ExistsAsync().Result then
            Some(blob.DownloadTextAsync().Result)
        else None
    else None

let private readBlobStream containerName blobPath =
    let stream = new MemoryStream();
    let container = createCloudBlobClient().GetContainerReference(containerName)
    let blob = container.GetBlockBlobReference(blobPath)
    if blob.ExistsAsync().Result then
        blob.DownloadToStreamAsync(stream).Wait()
    else failwith "blob not found"
    stream

let private writeBlobText containerName blobPath text = 
    let container = createCloudBlobClient().GetContainerReference(containerName)
    if container.ExistsAsync().Result then
        let blob = container.GetBlockBlobReference(blobPath)
        blob.UploadTextAsync(text).Wait()
    else failwith (sprintf "container not found %s" containerName)

let readIndex () =
  match readBlobText "data" "index.json" with
  | Some x -> x
  | None -> failwith "index file not found"
let saveIndex json = 
  writeBlobText "data" "index.json" json
let readFile file =
  readBlobText "data" file
let writeFile file data = 
  writeBlobText "data" file data

let functions = 
  readIndex, saveIndex,
  readFile, writeFile