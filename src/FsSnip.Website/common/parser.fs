module FsSnip.Parser

open System
open System.IO
open Paket
open FSharp.Formatting.CodeFormat
open FSharp.Formatting.Literate
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Text

// -------------------------------------------------------------------------------------------------
// Parse & format documents - restores packages using Paket and invokes
// the F# Compiler checker with the appropriate references
// -------------------------------------------------------------------------------------------------

let private framework = TargetProfile.SinglePlatform (FrameworkIdentifier.DotNetCoreApp DotNetCoreAppVersion.V3_1)
let private checker = lazy FSharpChecker.Create()

let private defaultOptions = lazy(
  checker.Value.GetProjectOptionsFromScript("foo.fsx", SourceText.ofString "module Foo", assumeDotNetFramework = false)
  |> Async.RunSynchronously
  |> fst)

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
          dependencies.Add(pkg)
          Some(pkg)
        with _ -> None)

    // Silently ignore all packages that could not be installed
    addedPackages
    |> Seq.collect(fun package -> 
        try dependencies.GetLibraries(None, package, framework) |> Seq.map (fun l -> l.Path)
        with _ -> seq [] )
    |> Array.ofSeq

let private workingFolderFor session = Path.Combine(Environment.CurrentDirectory, "temp", session)

/// encloses string content in quotes
let private encloseInQuotes (prefix : string) (line: string) =
  if (line.StartsWith prefix && line.Contains " ")
  then sprintf "%s\"%s\"" prefix (line.Substring prefix.Length)
  else line

/// Parses F# script file and download NuGet packages if required.
let parseScript session (content : string) packages =
  let workingFolder = workingFolderFor session

  if (not <| Directory.Exists workingFolder)
  then Directory.CreateDirectory workingFolder |> ignore

  let nugetReferences =
    restorePackages packages workingFolder
    |> Seq.map (sprintf "-r:%s")

  let scriptFile = Path.Combine(workingFolder, "Script.fsx")

  let compilerOptions =
    defaultOptions.Value.OtherOptions
    |> Seq.append nugetReferences
    |> Seq.map (encloseInQuotes "-r:")
    |> Seq.map (encloseInQuotes "--reference:")
    |> String.concat " "

  Literate.ParseScriptString(content, scriptFile, fscOptions = compilerOptions)

/// Marks parsing session as complete - basically deletes working folder for the given session
let completeSession session =
  let folder = workingFolderFor session
  if Directory.Exists folder then
    try
      Directory.Delete(folder, true)
    with
      | e -> printfn "Failed to delete folder \"%s\": %O" folder e