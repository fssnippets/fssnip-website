(* This script performs 3 steps:
1. Download the snippets data from the backup repo in GitHub,
2. Extract the zip file to a local folder and rename the folder to 'data',
3. Upload the data folder to blob storage (it compares the list of files present in the blob container
   with the list of files in the downloaded data folder and only uploads the difference.

It should be OS-agnostic as it is meant to be run from a devops pipeline, but can be run locally as well.
For that you will need 2 things that the script looks for in environment variables:
1) the backup data repo url, 2) the connection string for the target blob storage.
There is a scipt (upload-blobs.cmd) that will set these env variables before running this script,
but you'll have to edit the script to include those values first. *)

#r "nuget: Azure.Storage.Blobs"

open Azure.Storage.Blobs
open System
open System.IO

let dataDumpSource = Environment.GetEnvironmentVariable("fssnip_data_url")
let storageConnString = Environment.GetEnvironmentVariable("fssnip_storage_key")
let dataFolderName = "data"
let tempFile = $"{dataFolderName}.zip"
let unzippedFolder = "fssnip-data-master"
let dirSeparator = Path.DirectorySeparatorChar
let altDirSeparator = Path.AltDirectorySeparatorChar
let serviceClient = BlobServiceClient(storageConnString)
let containerClient = serviceClient.GetBlobContainerClient(dataFolderName)

let downloadDataAsync tempFile dataDumpSource =
    async {
        printfn "downloading data..."
        let client = new Net.Http.HttpClient()
        use! s = client.GetStreamAsync(dataDumpSource |> Uri) |> Async.AwaitTask
        use fs = new FileStream(tempFile, FileMode.CreateNew)
        do! s.CopyToAsync(fs) |> Async.AwaitTask
        fs.Close()
    }
let extractDataAsync tempFile =
    async {
        printfn "extracting data..."
        Compression.ZipFile.ExtractToDirectory(tempFile, ".", true)
        Directory.Move(unzippedFolder, dataFolderName)
    }

let uploadDataAsync (folderSeparator:char) (dataFolderName:string) (containerClient:BlobContainerClient) =
    async {
        let getMissingFileAsyncs () =
            let blobNameSet =
                containerClient.GetBlobs().AsPages()
                |> Seq.collect (fun page -> page.Values)
                |> Seq.filter (fun blob -> blob.Deleted |> not)
                // Throw index.json out of this set to make sure it is uploaded even if it exists already.
                |> Seq.filter (fun blob -> blob.Name <> "index.json")
                |> Seq.map (fun blob -> blob.Name)
                |> Set.ofSeq
            
            let fileNameSet =
                let fileNames =
                    DirectoryInfo(dataFolderName).EnumerateFiles("*", SearchOption.AllDirectories)
                    |> Seq.map (fun file -> file.FullName.Split($"{folderSeparator}{dataFolderName}{folderSeparator}").[1])
                if OperatingSystem.IsWindows()
                    then fileNames |> Seq.map (fun file -> file.Replace(dirSeparator, altDirSeparator))
                    else fileNames
                |> Set.ofSeq
            
            let difference = Set.difference fileNameSet blobNameSet

            difference
            |> fun fns -> printfn $"uploading {fns.Count} file(s)..."; fns
            |> Seq.map (fun fn ->
                let blobClient = BlobClient(storageConnString, dataFolderName, fn)
                let stream = File.OpenRead(Path.Combine [| dataFolderName; fn |]) :> Stream
                blobClient.UploadAsync(stream) |> Async.AwaitTask |> Async.Ignore
                )

        let! _ = containerClient.CreateIfNotExistsAsync() |> Async.AwaitTask
        //let! containerExists = containerClient.ExistsAsync() |> Async.AwaitTask
        //let containerExists = containerExists.Value
        getMissingFileAsyncs()
        |> Async.Parallel
        |> Async.RunSynchronously
        |> ignore
    }

// Preemptive dir deletes for local use, devops pipeline agent won't need them.    
if File.Exists($"{dataFolderName}.zip") then File.Delete($"{dataFolderName}.zip")
if Directory.Exists(dataFolderName) then Directory.Delete(dataFolderName, true)
if Directory.Exists(unzippedFolder) then Directory.Delete(unzippedFolder, true)

[
    downloadDataAsync tempFile dataDumpSource
    extractDataAsync tempFile
    uploadDataAsync dirSeparator dataFolderName containerClient 
]
|> Async.Sequential
|> Async.RunSynchronously
|> ignore
