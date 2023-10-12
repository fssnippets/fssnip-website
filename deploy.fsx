#r "nuget: Farmer"

open System
open Farmer
open Farmer.Arm
open Farmer.Builders

let appName = "fssnip"
    
let trimmedAppName = appName.Replace("-", "")
let storageAccountName = $"{trimmedAppName}-storage"
let artifactDir = "wwwroot"
let blobContainerName = "data"
let recaptchaSecret = Environment.GetEnvironmentVariable "RECAPTCHA_SECRET"

let logAnalytics =
    logAnalytics {
        name $"{appName}-workspace"
    }

let insights =
    appInsights {
        name $"{appName}-ai"
        log_analytics_workspace logAnalytics
    }

let storage = storageAccount {
    name storageAccountName
    sku Storage.Sku.Standard_LRS
    add_private_container blobContainerName
}

let app = webApp {
    name appName 
    runtime_stack Runtime.DotNetCore31
    link_to_app_insights insights
    sku WebApp.Sku.S1
    operating_system OS.Linux
    connection_string ("FSSNIP_STORAGE", storage.Key)
    settings [
        "FSSNIP_HOME_DIR", "."
        "RECAPTCHA_SECRET", recaptchaSecret
    ]
    zip_deploy artifactDir
    always_on
}

let deployment = arm {
    location Location.UKSouth
    add_resources [
        logAnalytics
        insights
        app
        storage
    ]
    output "storage-key" storage.Key
}

deployment
|> Deploy.execute appName Deploy.NoParameters
|> Map.find "storage-key"
|> fun key -> IO.File.WriteAllText("deployed_storage_key.txt", key)