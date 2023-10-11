@echo off

set PAKET_SKIP_RESTORE_TARGETS true
set RECAPTCHA_SECRET=insert_secret_here

dotnet tool restore
dotnet paket restore
REM in case another script left the global.json renamed
ren notglobal.json global.json
dotnet fake run build.fsx %*
dotnet wwwroot/fssnip.dll