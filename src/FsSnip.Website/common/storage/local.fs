module FsSnip.Storage.Local

open System
open System.IO

// -------------------------------------------------------------------------------------------------
// Local file system storage - the `functions` value should be compatible with `azure.fs`
// -------------------------------------------------------------------------------------------------

let private dataFolder = Path.Combine(__SOURCE_DIRECTORY__, "../../../../data") |> Path.GetFullPath
let private indexFile = Path.Combine(dataFolder, "index.json")

let readIndex () = 
  File.ReadAllText(indexFile)
let readFile file = 
  try
    Some(File.ReadAllText(Path.Combine(dataFolder, file)))
  with
  | :? System.IO.FileNotFoundException as ex -> None
let saveIndex json = 
  File.WriteAllText(indexFile, json)
let writeFile file data = 
  let path = Path.Combine(dataFolder, file)
  let dir = Path.GetDirectoryName(path)
  if not(Directory.Exists(dir)) then Directory.CreateDirectory(dir) |> ignore
  File.WriteAllText(path, data)

let functions = 
  readIndex, saveIndex,
  readFile, writeFile