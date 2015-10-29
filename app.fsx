#r "System.Xml.Linq.dll"
#r "packages/Suave/lib/net40/Suave.dll"
#r "packages/FSharp.Data/lib/net40/FSharp.Data.dll"
#r "packages/DotLiquid/lib/NET45/DotLiquid.dll"
#r "packages/Suave.DotLiquid/lib/net40/Suave.DotLiquid.dll"
#load "packages/FSharp.Azure.StorageTypeProvider/StorageTypeProvider.fsx"
#load "packages/FSharp.Formatting/FSharp.Formatting.fsx"

// -------------------------------------------------------------------------------------------------
// Loading the FsSnip.WebSite project files
// -------------------------------------------------------------------------------------------------

#load "code/common/storage/azure.fs"
#load "code/common/storage/local.fs"
#load "code/common/utils.fs"
#load "code/common/filters.fs"
#load "code/common/data.fs"
#load "code/common/rssfeed.fs"
#load "code/pages/home.fs"
#load "code/pages/insert.fs"
#load "code/pages/snippet.fs"
#load "code/pages/author.fs"
#load "code/pages/tag.fs"
#load "code/pages/rss.fs"
#load "code/pages/update.fs"
#load "app.fs"

