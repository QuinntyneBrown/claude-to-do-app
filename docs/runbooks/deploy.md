# Tickbox — Deployment Runbook

This runbook documents how to provision Azure resources and deploy Tickbox end-to-end. The setup targets the **cheapest viable Azure plan** (~$5/month, dominated by the SQL DB).

## Resources & SKU choices

| Resource              | SKU                | Why                                                                 |
|-----------------------|--------------------|---------------------------------------------------------------------|
| App Service Plan      | Linux **F1 Free**  | Free tier, .NET 9 supported, 60 min CPU/day, no Always On.          |
| App Service (backend) | .NET 9 on F1       | Hosts the `Tickbox.Api` published bundle.                           |
| Azure SQL Server      | (server is free)   | Logical server only — billing is on the database below.             |
| Azure SQL Database    | **Basic** (5 DTU)  | Cheapest standalone DB SKU, ~$5/month, 2 GB cap.                    |
| Static Web Apps       | **Free**           | Hosts the Angular bundle; 100 GB bandwidth/month, free SSL.         |
| Resource Group        | (free)             | Container only.                                                     |

Trade-offs we accepted:
- F1 cold-starts and cuts CPU after the daily quota; fine for demo / dogfood traffic, not production.
- SQL Basic has 5 DTU; under load the API will throttle on database time before the F1 plan does.
- Static Web Apps Free does not bind to custom domains beyond the `*.azurestaticapps.net` default — accept this for now or upgrade to Standard ($9/month) when a custom domain is needed.

## Required secrets

The deploy workflow reads these from repo / environment secrets:

| Secret                             | Used by                                | Notes                                                            |
|------------------------------------|----------------------------------------|------------------------------------------------------------------|
| `AZURE_CREDENTIALS`                | `azure/login@v2`                       | Service principal JSON with `Contributor` on the resource group. |
| `AZURE_STATIC_WEB_APPS_API_TOKEN`  | `Azure/static-web-apps-deploy@v1`      | Deployment token from the Static Web App resource.               |

App Service settings populated by the provisioning script (not GitHub secrets — these live on Azure):

- `ConnectionStrings__Default` — SQL connection string.
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SigningKey` — JWT validation.
- `ASPNETCORE_ENVIRONMENT=Production`.

## First-time provisioning (idempotent)

Run from a developer machine with the Azure CLI signed in (`az login`).

```powershell
# Read these values from a secrets manager — never check them in.
$sqlPwd = Read-Host "SQL admin password" -AsSecureString
$jwtKey = Read-Host "JWT signing key (≥32 random bytes, base64)" -AsSecureString

./scripts/provision-azure.ps1 `
    -SubscriptionId "<your-sub-id>" `
    -Location "westus2" `
    -SqlAdminUser "tickboxadmin" `
    -SqlAdminPassword $sqlPwd `
    -JwtIssuer "https://<your-app-service>.azurewebsites.net" `
    -JwtAudience "tickbox-api" `
    -JwtSigningKey $jwtKey
```

The script is idempotent: re-running updates each resource in place. Resource names default to `tickbox-rg / tickbox-plan / tickbox-api / tickbox-sql / tickbox / tickbox-web` and can be overridden with the matching parameters.

After provisioning:

1. **Service principal for CI.** Create one and store its JSON output as `AZURE_CREDENTIALS`:
   ```powershell
   az ad sp create-for-rbac `
       --name "tickbox-deployer" `
       --role contributor `
       --scopes "/subscriptions/<sub>/resourceGroups/tickbox-rg" `
       --sdk-auth
   ```
2. **Static Web Apps deploy token.** Pull from Azure and store as `AZURE_STATIC_WEB_APPS_API_TOKEN`:
   ```powershell
   az staticwebapp secrets list --name tickbox-web --query "properties.apiKey" --output tsv
   ```
3. **Frontend → backend wiring.** The frontend uses `window.__TICKBOX_API__` (see `app.config.ts`) for the base URL. Set it in `frontend/src/index.html` (production override) or via a build-time environment file before pushing to `deploy`.

## Per-deploy flow

Push to the `deploy` branch (or trigger `Deploy` manually from the Actions tab). The `.github/workflows/deploy.yml` workflow:

1. **build-backend** — `dotnet restore` → `dotnet build -warnaserror` → `dotnet test` → `dotnet publish` → uploads the artifact.
2. **build-frontend** — `npm ci` → `ng build api/components/domain` (in dependency order) → `ng build tickbox --configuration production` → uploads the artifact.
3. **deploy-backend** — downloads the artifact, logs into Azure with the SP, runs `azure/webapps-deploy@v3` against the App Service.
4. **deploy-frontend** — downloads the artifact and runs `Azure/static-web-apps-deploy@v1` with `skip_app_build: true` so the workflow uses the pre-built `dist/` folder.

The `build-*` jobs run in parallel; the `deploy-*` jobs only run if their build succeeded. There is no DB-migration step — EF Core auto-applies migrations on first request via `db.Database.Migrate()` in `Program.cs` (development path); for production, run the explicit migration once after provisioning:

```powershell
# From a machine with dotnet-ef and the prod connection string in $env:CONN
dotnet ef database update --project backend/src/Tickbox.Infrastructure --startup-project backend/src/Tickbox.Api `
    --connection $env:CONN
```

## Rollback

Two options, in order of preference:

1. **App Service deployment slot rollback.** App Service keeps the previous package; revert via:
   ```powershell
   az webapp deployment list-publishing-profiles --resource-group tickbox-rg --name tickbox-api
   # then redeploy a known-good artifact via webapps-deploy
   ```
   For Static Web Apps, redeploy the previous tag/SHA via `workflow_dispatch` against that ref.
2. **Re-deploy a known-good ref.** Push the previous good commit back onto the `deploy` branch (`git push --force-with-lease origin <good-sha>:deploy`). The CI runs again and replaces both bundles.

For a database rollback, restore from the automatic Azure SQL backup (Basic tier keeps 7 days):

```powershell
az sql db restore --dest-name tickbox-restore --resource-group tickbox-rg --server tickbox-sql `
    --name tickbox --time 2026-01-15T12:00:00Z
```

## Environment variables matrix

| Variable                        | Set on                | Source                          |
|---------------------------------|-----------------------|---------------------------------|
| `ConnectionStrings__Default`    | App Service            | `provision-azure.ps1`           |
| `Jwt__Issuer`                   | App Service            | `provision-azure.ps1` parameter |
| `Jwt__Audience`                 | App Service            | `provision-azure.ps1` parameter |
| `Jwt__SigningKey`               | App Service            | `provision-azure.ps1` parameter (secure string) |
| `ASPNETCORE_ENVIRONMENT`        | App Service            | `provision-azure.ps1` (`Production`) |
| `AZURE_CREDENTIALS`             | GitHub repo secret     | Output of `az ad sp create-for-rbac` |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | GitHub repo secret   | Static Web App `apiKey`         |
| `window.__TICKBOX_API__`        | `frontend/src/index.html` (prod) | App Service default hostname |

## Cost guardrails

- Set a budget alert on the resource group at $10/month (`az consumption budget create`).
- The F1 plan's daily CPU quota is your hard ceiling — if it hits limit, the API returns 503 until the next day.
- Review the SQL DB DTU usage weekly; a sustained >80% indicates the demo has outgrown Basic and should move to S0 ($15/month).
