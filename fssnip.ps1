param($command='run')

Write-Output "Executing command: $command"

$env:PAKET_SKIP_RESTORE_TARGETS="true"
$env:IP_ADDRESS="0.0.0.0"
$env:FSSNIP_HOME_DIR="."
$env:RECAPTCHA_SECRET=<insert_value_here>
$env:fssnip_storage_key=<insert_value_here>
$env:fssnip_data_url="https://github.com/fssnippets/fssnip-data/archive/master.zip"

function Restore-GlobalJson { if (Test-Path -Path './notglobal.json') { Rename-Item './notglobal.json' -NewName './global.json' } }
function Hide-GlobalJson { if (Test-Path -Path './global.json') { Rename-Item './global.json' -NewName './notglobal.json' } }

function Build-App {
    # in case another script left the global.json renamed
    Restore-GlobalJson
    dotnet tool restore
    dotnet paket restore
    dotnet fsi build.fsx
}

function Start-App {
    # in case another script left the global.json renamed
    Restore-GlobalJson
    dotnet wwwroot/fssnip.dll
}

function Deploy-App {
    Hide-GlobalJson
    dotnet fsi deploy.fsx
    Restore-GlobalJson
}

function Deploy-Data {
    Hide-GlobalJson
    dotnet fsi upload-blobs.fsx
    Restore-GlobalJson
}

if ($command -eq 'run') { Start-App }
if ($command -eq 'build') { Build-App }
if ($command -eq 'deploy') { Deploy-App }
if ($command -eq 'upload') { Deploy-Data }