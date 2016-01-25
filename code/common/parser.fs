module FsSnip.Parser

open System
open System.IO
open Paket
open FsSnip
open FSharp.Literate
open FSharp.CodeFormat
open Microsoft.FSharp.Compiler.SourceCodeServices

// -------------------------------------------------------------------------------------------------
// Parse & format documents - restores packages using Paket and invokes
// the F# Compiler checker with the appropriate references
// -------------------------------------------------------------------------------------------------

let private framework = DotNetFramework(FrameworkVersion.V4_5)
let private formatAgent = lazy CodeFormat.CreateAgent()
let private checker = lazy FSharpChecker.Create()

let private restorePackages packages folder =
  if Array.isEmpty packages
  then [| |]
  else
    Dependencies.Init folder
    let dependencies = Dependencies.Locate folder

    // Because F# Data is already loaded from another location, we cannot put it in
    // another place and load it from there - so we just use the currently loaded one
    // hoping that it will be compatible with other dependencies...
    //
    // Also, silently ignore all packages that cannot be added (e.g. because they don't exist)
    let addedPackages = 
      packages |> Array.choose (fun pkg ->
        try
          if not(String.Equals(pkg, "FSharp.Data", StringComparison.InvariantCultureIgnoreCase)) then 
            dependencies.Add(pkg)
          Some(pkg)
        with _ -> None)

    // Silently ignore all packages that could not be installed
    addedPackages
    |> Seq.collect(fun package -> 
        try
          if String.Equals(package, "FSharp.Data", StringComparison.InvariantCultureIgnoreCase) then 
            seq [ __SOURCE_DIRECTORY__ + "/../../packages/FSharp.Data/lib/net40/FSharp.Data.dll" ]
          else dependencies.GetLibraries((None, package), framework)
        with _ -> seq [] )
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
    checker.Value.GetProjectOptionsFromScript(scriptFile, content, DateTime.Now)
    |> Async.RunSynchronously

  let compilerOptions =
    defaultOptions.OtherOptions
    |> Seq.append nugetReferences
    |> Seq.map (encloseInQuotes "-r:")
    |> Seq.map (encloseInQuotes "--reference:")
    |> String.concat " "

  Literate.ParseScriptString(content, scriptFile, formatAgent.Value, compilerOptions)

/// Marks parsing session as complete - basically deletes working forlder for the given session
let completeSession session =
  let folder = workingFolderFor session
  if Directory.Exists folder then
    try
      Directory.Delete(folder, true)
    with
      | e -> printfn "Failed to delete folder \"%s\": %O" folder e
