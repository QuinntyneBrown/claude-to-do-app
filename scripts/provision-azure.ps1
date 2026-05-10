# Tickbox Azure provisioning — cheapest viable plan
#
# Provisions:
#   - Resource group
#   - App Service Plan (Linux F1 Free)        — backend host
#   - App Service (.NET 9)                    — backend
#   - Azure SQL Server + Database (Basic)     — persistence
#   - Static Web App (Free)                   — frontend
#
# Total run-cost target: ~$5/month (Azure SQL Basic). All other resources sit on free tiers.
#
# Idempotent: safe to re-run; every `az` call uses --only-show-errors and creates-or-updates.

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)] [string] $SubscriptionId,
    [Parameter(Mandatory = $true)] [string] $Location,
    [string] $ResourceGroup    = "tickbox-rg",
    [string] $AppServicePlan   = "tickbox-plan",
    [string] $WebApp           = "tickbox-api",
    [string] $SqlServer        = "tickbox-sql",
    [string] $SqlDatabase      = "tickbox",
    [string] $StaticWebApp     = "tickbox-web",
    [Parameter(Mandatory = $true)] [string] $SqlAdminUser,
    [Parameter(Mandatory = $true)] [securestring] $SqlAdminPassword,
    [Parameter(Mandatory = $true)] [string] $JwtIssuer,
    [Parameter(Mandatory = $true)] [string] $JwtAudience,
    [Parameter(Mandatory = $true)] [securestring] $JwtSigningKey
)

$ErrorActionPreference = "Stop"

function Convert-FromSecureString-Plain {
    param([securestring]$s)
    [System.Net.NetworkCredential]::new("", $s).Password
}

$plainSqlPwd = Convert-FromSecureString-Plain $SqlAdminPassword
$plainJwtKey = Convert-FromSecureString-Plain $JwtSigningKey

Write-Host "==> Setting subscription"
az account set --subscription $SubscriptionId --only-show-errors | Out-Null

Write-Host "==> Creating resource group $ResourceGroup in $Location"
az group create --name $ResourceGroup --location $Location --only-show-errors | Out-Null

Write-Host "==> Creating App Service Plan ($AppServicePlan, Linux F1 Free)"
az appservice plan create `
    --name $AppServicePlan `
    --resource-group $ResourceGroup `
    --location $Location `
    --sku F1 `
    --is-linux `
    --only-show-errors | Out-Null

Write-Host "==> Creating App Service ($WebApp, .NET 9)"
az webapp create `
    --name $WebApp `
    --resource-group $ResourceGroup `
    --plan $AppServicePlan `
    --runtime "DOTNETCORE:9.0" `
    --only-show-errors | Out-Null

Write-Host "==> Creating Azure SQL Server ($SqlServer)"
az sql server create `
    --name $SqlServer `
    --resource-group $ResourceGroup `
    --location $Location `
    --admin-user $SqlAdminUser `
    --admin-password $plainSqlPwd `
    --only-show-errors | Out-Null

Write-Host "==> Allowing Azure services through SQL firewall"
az sql server firewall-rule create `
    --resource-group $ResourceGroup `
    --server $SqlServer `
    --name AllowAzureServices `
    --start-ip-address 0.0.0.0 `
    --end-ip-address 0.0.0.0 `
    --only-show-errors | Out-Null

Write-Host "==> Creating SQL Database ($SqlDatabase, Basic)"
az sql db create `
    --name $SqlDatabase `
    --resource-group $ResourceGroup `
    --server $SqlServer `
    --service-objective Basic `
    --backup-storage-redundancy Local `
    --only-show-errors | Out-Null

$connectionString = "Server=tcp:$SqlServer.database.windows.net,1433;Database=$SqlDatabase;User ID=$SqlAdminUser;Password=$plainSqlPwd;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

Write-Host "==> Configuring App Service settings"
az webapp config connection-string set `
    --resource-group $ResourceGroup `
    --name $WebApp `
    --connection-string-type SQLAzure `
    --settings "Default=$connectionString" `
    --only-show-errors | Out-Null

az webapp config appsettings set `
    --resource-group $ResourceGroup `
    --name $WebApp `
    --settings "Jwt__Issuer=$JwtIssuer" "Jwt__Audience=$JwtAudience" "Jwt__SigningKey=$plainJwtKey" "ASPNETCORE_ENVIRONMENT=Production" `
    --only-show-errors | Out-Null

Write-Host "==> Creating Static Web App ($StaticWebApp, Free)"
az staticwebapp create `
    --name $StaticWebApp `
    --resource-group $ResourceGroup `
    --location $Location `
    --sku Free `
    --only-show-errors | Out-Null

$webAppHost = az webapp show --name $WebApp --resource-group $ResourceGroup --query defaultHostName --output tsv --only-show-errors

Write-Host ""
Write-Host "==> Provisioning complete."
Write-Host "    Backend:  https://$webAppHost"
Write-Host "    Frontend: discover via 'az staticwebapp show --name $StaticWebApp --resource-group $ResourceGroup --query defaultHostname --output tsv'"
Write-Host "    SQL DB:   $SqlServer.database.windows.net / $SqlDatabase"
