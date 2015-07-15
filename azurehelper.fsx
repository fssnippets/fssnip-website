#load "packages/FSharp.Azure.StorageTypeProvider/StorageTypeProvider.fsx"

// -------------------------------------------------------------------------------------------------
// Azure Blob Storage Functions
// -------------------------------------------------------------------------------------------------

open FSharp.Azure.StorageTypeProvider
open Microsoft.WindowsAzure.Storage.Blob
open System.IO

//============================================
// Initialize Azure Storage from example data
//=============================================
[<Literal>]
let azureConnectionString = "UseDevelopmentStorage=true"

type Azure = AzureTypeProvider<azureConnectionString>

//===========================================
// Initialization functions - can be used to set up containers and upload example data.
//===========================================
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
        if blob.Exists() then printfn "blob exists: %s/%s" path file
        else 
            let task = blob.UploadFromFileAsync(Path.Combine(path, file), FileMode.Open)
            printfn "starting updload.."
            let! result = task |> Async.AwaitIAsyncResult
            printfn "upload result: %A ==> file: %s/%s" result path file
        })
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore


let folders take path dirName (containerName: string) =
    let container = Azure.Containers.CloudBlobClient.GetContainerReference(containerName)
    Directory.EnumerateDirectories(path)
    |> Seq.take take
    |> Seq.map (fun dirPath ->
        let dir = dirPath.Substring(path.Length + 1)
        (Path.Combine(path, dir), container.GetDirectoryReference(dirName + "/" + dir))
        )
    |> Seq.map (fun (p, c) -> files c p)
    |> Seq.toList
    |> ignore

let indexPath = __SOURCE_DIRECTORY__ + "/data/"
let indexFile = __SOURCE_DIRECTORY__ + "/data/index.json"

//Run this to create the containers
initializeStorage ()

// Upload the local index.json file
Azure.Containers.data.Upload(indexFile)
let index = Azure.Containers.data.``index.json``.Read()

// THESE CAN TAKE A LONG TIME TO RUN!
folders 150 (Path.Combine(indexPath, "source")) "source" "data"
folders 150 (Path.Combine(indexPath, "formatted")) "formatted" "data"

// List the source containers
Azure.Containers.data.AsCloudBlobContainer().GetDirectoryReference("source").ListBlobs(true, BlobListingDetails.All)
|> Seq.iter (fun b -> printfn "%s" (b.StorageUri.PrimaryUri.ToString()))
// List the formated containers
Azure.Containers.data.AsCloudBlobContainer().GetDirectoryReference("formatted").ListBlobs(true, BlobListingDetails.All)
|> Seq.iter (fun b -> printfn "%s" (b.StorageUri.PrimaryUri.ToString()))