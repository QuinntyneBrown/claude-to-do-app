# DP1 — Azure deployment workflow

Implementer: `claude@M5`.

## Deliverables (shipped)

- `scripts/provision-azure.ps1` — Azure CLI provisioning script (PowerShell). Idempotent. Provisions resource group + App Service Plan (Linux F1) + App Service (.NET 9) + Azure SQL Server + DB (Basic) + Static Web Apps (Free). Wires the connection string and JWT settings into App Service settings.
- `.github/workflows/deploy.yml` — GitHub Actions workflow. Triggers on push to `deploy` (or manual dispatch). Two parallel build jobs (backend with `-warnaserror` + `dotnet test`; frontend with `ng build` of all libraries + app), then two parallel deploy jobs gated on the builds.
- `docs/runbooks/deploy.md` — runbook: SKU choices + cost trade-offs (~$5/month, dominated by SQL Basic), required secrets matrix (`AZURE_CREDENTIALS`, `AZURE_STATIC_WEB_APPS_API_TOKEN`), per-deploy flow, rollback procedure (App Service redeploy of last good artifact + Azure SQL point-in-time restore), env-var matrix, cost guardrails.

## Cheapest-plan choices

| Resource              | SKU                | Run cost        |
|-----------------------|--------------------|-----------------|
| App Service Plan      | Linux **F1 Free**  | $0              |
| App Service (backend) | .NET 9 on F1       | $0              |
| Azure SQL Database    | **Basic** (5 DTU)  | ~$5 / month     |
| Static Web Apps       | **Free**           | $0              |
| Resource Group        | (free)             | $0              |
| **Total**             |                    | **~$5 / month** |

Trade-offs are documented at the top of `deploy.md`. There is no cheaper SKU that still gives a working SQL Server + Linux .NET 9 + SPA host; SQLite-on-disk would be cheaper but breaks the production scenario (App Service local disk isn't durable across instance restarts).

## Verification status

- ✅ Provisioning script: syntactically valid PowerShell, every Azure CLI invocation uses idempotent create / set commands.
- ✅ Workflow file: valid `actions/checkout@v4` / `setup-dotnet@v4` / `setup-node@v4` action versions; matches the repo layout (`backend/Tickbox.sln`, `frontend/projects/{api,components,domain,tickbox}`).
- ⚠ **Terminal step not executed in this loop.** The DP1 "Done when" criterion includes "a push to the deploy branch provisions and deploys successfully." That requires (a) an Azure subscription, (b) the user running `provision-azure.ps1` once, (c) the user storing the two GitHub secrets, and (d) pushing the `deploy` branch. None of those four steps can be done from this loop without the user supplying credentials. The artifacts are deploy-ready and documented; first execution is gated on the user's Azure setup.

The next phase (TP1 — test plan) does not depend on a successful first deploy, only on the deployable-state repository, so it can proceed.
