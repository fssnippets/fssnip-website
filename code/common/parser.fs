module FsSnip.Parser

open System
open System.IO
open Paket
open FSharp.Literate
open FsSnip

let framework = DotNetFramework(FrameworkVersion.V4_5)

let private restorePackages packages folder =
  if Array.isEmpty packages
  then [| |]
  else
    Dependencies.Init folder
    let dependencies = Dependencies.Locate folder
    packages |> Seq.iter dependencies.Add

    packages
    |> Seq.collect(fun package -> dependencies.GetLibraries((None, package), framework))
    |> Array.ofSeq

let private workingFolderFor session = Path.Combine(Environment.CurrentDirectory, "temp", session)

/// Parses F# script file and download NuGet packages if required.
let parseScript session content packages =
  let workingFolder = workingFolderFor session

  if (not <| Directory.Exists workingFolder)
  then Directory.CreateDirectory workingFolder |> ignore

  let references = 
    restorePackages packages workingFolder
    |> Seq.map (sprintf "--reference:\"%s\"")
    |> String.concat " "

  let scriptFile = Path.Combine(workingFolder, "Script.fsx")
  Literate.ParseScriptString(content, scriptFile, Utils.formatAgent, references)

/// Marks parsing session as complete - basically deletes working forlder for the given session
let completeSession session =
  let folder = workingFolderFor session
  if Directory.Exists folder then
    try
      Directory.Delete(folder, true)
    with 
      | e -> printfn "Failed to delete folder \"%s\": %O" folder e
