# Tickbox local-runtime entry point.
#
# Brings up backend (Tickbox.Api) and frontend (ng serve) in two new PowerShell
# windows. Returns immediately so the calling shell stays free.
#
# Prereqs (verified once on a fresh checkout, not on every run):
#   - .NET 9 SDK on PATH (`dotnet --version`)
#   - Node 20+ and npm on PATH (`node --version`)
#   - SQL Server LocalDB instance MSSQLLocalDB (`sqllocaldb info MSSQLLocalDB`)
#
# After this script returns, you'll have:
#   - Backend on http://localhost:5217
#   - Frontend on http://localhost:4200

[CmdletBinding()]
param(
    [switch] $InstallFrontendDeps
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$backendApi = Join-Path $repoRoot "backend\src\Tickbox.Api"
$frontend   = Join-Path $repoRoot "frontend"

if (-not (Test-Path $backendApi)) {
    throw "Backend project not found at $backendApi"
}
if (-not (Test-Path $frontend)) {
    throw "Frontend workspace not found at $frontend"
}

if ($InstallFrontendDeps -or -not (Test-Path (Join-Path $frontend "node_modules"))) {
    Write-Host "==> Installing frontend dependencies (npm ci)"
    Push-Location $frontend
    try { npm ci } finally { Pop-Location }
}

if (-not (Test-Path (Join-Path $frontend "dist\api"))) {
    Write-Host "==> First-time library build (api / components / domain)"
    Push-Location $frontend
    try {
        npx ng build api
        npx ng build components
        npx ng build domain
    } finally { Pop-Location }
}

Write-Host "==> Starting backend on http://localhost:5217"
Start-Process -FilePath "pwsh" -ArgumentList "-NoExit", "-Command", "Set-Location '$backendApi'; dotnet run"

Write-Host "==> Starting frontend on http://localhost:4200"
Start-Process -FilePath "pwsh" -ArgumentList "-NoExit", "-Command", "Set-Location '$frontend'; npx ng serve"

Write-Host ""
Write-Host "Tickbox is starting. Smoke check after both windows show 'Listening' / 'Local: http://localhost:4200/':"
Write-Host "  curl http://localhost:5217/api/auth/oidc/authorize  # backend smoke"
Write-Host "  Start-Process http://localhost:4200                  # frontend smoke"
