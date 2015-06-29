// --------------------------------------------------------------------------------------
// Start the 'app' WebPart defined in 'app.fsx' on Azure using %HTTP_PLATFORM_PORT%
// --------------------------------------------------------------------------------------

#r "packages/FAKE/tools/FakeLib.dll"
#load "app.fsx"
open App
open Fake
open System
open Suave

let serverConfig =
  let port = int (getBuildParam "port")
  { Web.defaultConfig with
      homeFolder = Some __SOURCE_DIRECTORY__
      logger = Logging.Loggers.saneDefaultsFor Logging.LogLevel.Warn
      bindings = [ Types.HttpBinding.mk' Types.HTTP "127.0.0.1" port ] }

Web.startWebServer serverConfig app