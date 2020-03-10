module FsSnip.Storage.Azure

open System
open System.IO
open FSharp.Azure.StorageTypeProvider
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
    if container.Exists() then
        let blob = container.GetBlockBlobReference(blobPath)
        if blob.Exists() then
            Some(blob.DownloadText(System.Text.Encoding.UTF8))
        else None
    else None

let private readBlobStream containerName blobPath =
    let stream = new MemoryStream();
    let container = createCloudBlobClient().GetContainerReference(containerName)
    let blob = container.GetBlockBlobReference(blobPath)
    if blob.Exists() then
        blob.DownloadToStream(stream)
    else failwith "blob not found"
    stream

let private writeBlobText containerName blobPath text = 
    let container = createCloudBlobClient().GetContainerReference(containerName)
    if container.Exists() then
        let blob = container.GetBlockBlobReference(blobPath)
        blob.UploadText(text, System.Text.Encoding.UTF8)
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