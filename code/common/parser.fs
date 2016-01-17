module FsSnip.Parser

open System
open System.IO
open Paket
open FSharp.Literate
open FsSnip
open Microsoft.FSharp.Compiler.SourceCodeServices

let framework = DotNetFramework(FrameworkVersion.V4_5)

let private checker = FSharpChecker.Create()

let private restorePackages packages folder =
  if Array.isEmpty packages
  then [| |]
  else
    Dependencies.Init folder
    let dependencies = Dependencies.Locate folder

    // Because F# Data is already loaded from another location, we cannot put it in
    // another place and load it from there - so we just use the currently loaded one
    // hoping that it will be compatible with other dependencies...
    for pkg in packages do 
      if not(String.Equals(pkg, "FSharp.Data", StringComparison.InvariantCultureIgnoreCase)) then 
        dependencies.Add(pkg)

    packages
    |> Seq.collect(fun package -> 
        if String.Equals(package, "FSharp.Data", StringComparison.InvariantCultureIgnoreCase) then 
          seq [ __SOURCE_DIRECTORY__ + "/../../packages/FSharp.Data/lib/net40/FSharp.Data.dll" ]
        else dependencies.GetLibraries((None, package), framework))
    |> Array.ofSeq

let private workingFolderFor session = Path.Combine(Environment.CurrentDirectory, "temp", session)

/// encloses string content in quotes
let private encloseInQuotes prefix (line: string) =
  if (line.StartsWith prefix && line.Contains " ")
  then sprintf "%s\"%s\"" prefix (line.Substring prefix.Length)
  else line

/// Parses F# script file and download NuGet packages if required.
let parseScript session content packages =
  let workingFolder = workingFolderFor session

  if (not <| Directory.Exists workingFolder)
  then Directory.CreateDirectory workingFolder |> ignore

  let nugetReferences =
    restorePackages packages workingFolder
    |> Seq.map (sprintf "-r:%s")

  let scriptFile = Path.Combine(workingFolder, "Script.fsx")
  let defaultOptions =
    checker.GetProjectOptionsFromScript(scriptFile, content, DateTime.Now)
    |> Async.RunSynchronously

  let compilerOptions =
    defaultOptions.OtherOptions
    |> Seq.append nugetReferences
    |> Seq.map (encloseInQuotes "-r:")
    |> Seq.map (encloseInQuotes "--reference:")
    |> String.concat " "

  Literate.ParseScriptString(content, scriptFile, Utils.formatAgent, compilerOptions)

/// Marks parsing session as complete - basically deletes working forlder for the given session
let completeSession session =
  let folder = workingFolderFor session
  if Directory.Exists folder then
    try
      Directory.Delete(folder, true)
    with
      | e -> printfn "Failed to delete folder \"%s\": %O" folder e
