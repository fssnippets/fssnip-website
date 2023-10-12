@echo off

set PAKET_SKIP_RESTORE_TARGETS true
set RECAPTCHA_SECRET=insert_secret_here

dotnet tool restore
dotnet paket restore
REM The following 2 FSXs require different SDK/Lang version each, this workaround
REM enables the global.json for the first one but hides it (by renaming it) for the second one.
ren global.notJson global.json
REM dotnet fake run build.fsx %*
ren global.json global.notJson
dotnet fsi deploy.fsx
ren global.notJson global.json