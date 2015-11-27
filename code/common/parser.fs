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

let parseScript id content packages =
    let tempFolder = Path.Combine(Environment.CurrentDirectory, "temp", id.ToString())

    if (not <| Directory.Exists tempFolder)
    then Directory.CreateDirectory tempFolder |> ignore

    let references = 
        restorePackages packages tempFolder
        |> Seq.map (sprintf "-r \"%s\"")
        |> String.concat " "

    let scriptFile = Path.Combine(tempFolder, "Script.fsx")
    let doc = Literate.ParseScriptString(content, scriptFile, Utils.formatAgent, references)
    Literate.WriteHtml(doc, "fs", true, true)
