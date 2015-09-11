module FsSnip.Data.Azure.Storage

open System.IO
open FSharp.Azure.StorageTypeProvider
open Microsoft.WindowsAzure.Storage.Blob

// -------------------------------------------------------------------------------------------------
//
// -------------------------------------------------------------------------------------------------

let [<Literal>] AzureConnectionString = "UseDevelopmentStorage=true"
type Azure = AzureTypeProvider<AzureConnectionString>

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

let readIndex () =
  readBlobText "data" "index.json"
let saveIndex json = 
  writeBlobText "data" "index.json" json
let readFile file =
  readBlobText "data" file
let writeFile file data = 
  writeBlobText "data" file data
