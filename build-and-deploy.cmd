@echo off

set PAKET_SKIP_RESTORE_TARGETS true

dotnet tool restore
dotnet paket restore
REM The following 2 FSXs require different SDK/Lang version each, this workaround
REM enables the global.json for the first one but hides it (by renaming it) for the second one.
ren notglobal.json global.json
dotnet fake run build.fsx %*
ren global.json notglobal.json
dotnet fsi deploy.fsx
ren notglobal.json global.json