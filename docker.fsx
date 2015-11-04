// --------------------------------------------------------------------------------------
// Start the 'app' WebPart defined in 'app.fsx' on Heroku using %PORT%
// --------------------------------------------------------------------------------------

#load "app.fsx"
open App
open System
open Suave
open System
open System.Web
open System.Net
open Suave
open Suave.Web
open Suave.Http
open Suave.Types
open FSharp.Data
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Http.Writers

let app = App.app

let serverConfig =
  let ip = IPAddress.Parse "0.0.0.0"
  { Web.defaultConfig with
      homeFolder = Some __SOURCE_DIRECTORY__
      logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Warn
      bindings = [ HttpBinding.mk HTTP ip 8080us ] }

Web.startWebServer serverConfig app