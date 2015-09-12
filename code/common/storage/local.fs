module FsSnip.Data.Local.Storage

open System
open System.IO

// -------------------------------------------------------------------------------------------------
// Local file system storag - the `functions` value should be compatibl with `azure.fs`
// -------------------------------------------------------------------------------------------------

let private indexFile = __SOURCE_DIRECTORY__ + "/../../../data/index.json"

let readIndex () = 
  File.ReadAllText(indexFile)
let readFile file = 
  File.ReadAllText(sprintf "%s/../../../data/%s" __SOURCE_DIRECTORY__ file)
let saveIndex json = 
  File.WriteAllText(indexFile, json)
let writeFile file data = 
  let path = sprintf "%s/../../../data/%s" __SOURCE_DIRECTORY__ file
  let dir = Path.GetDirectoryName(path)
  if not(Directory.Exists(dir)) then Directory.CreateDirectory(dir) |> ignore
  File.WriteAllText(path, data)

let functions = 
  readIndex, saveIndex,
  readFile, writeFile