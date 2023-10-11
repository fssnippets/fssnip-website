set fssnip_data_url=https://github.com/fssnippets/fssnip-data/archive/master.zip
set fssnip_storage_key=insert_azure_storage_account_connection_string_here
ren global.json nonglobal.json
dotnet fsi upload-blobs.fsx
ren nonglobal.json global.json