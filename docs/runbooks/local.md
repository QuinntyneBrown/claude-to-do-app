# Tickbox — Local Runtime Runbook

This runbook covers the single command needed to bring up Tickbox locally on a fresh checkout. For the per-component detail (env vars, ports, migrations, cache busting), see:

- `docs/runbooks/backend.md` — `Tickbox.Api` operational notes.
- `docs/runbooks/frontend.md` — Angular workspace operational notes.

## Single command

From the repo root, in a PowerShell prompt:

```powershell
./scripts/start-local.ps1
```

That spawns two new PowerShell windows — one for the backend (`dotnet run`) and one for the frontend (`ng serve`) — and returns control to the caller. After roughly 20 s both windows print their respective "ready" lines and the app is reachable on:

- **Frontend:** http://localhost:4200
- **Backend:** http://localhost:5217

The script is idempotent: it skips `npm ci` and the initial `ng build api/components/domain` if `frontend/node_modules` and `frontend/dist/` already exist. Force a clean install with `./scripts/start-local.ps1 -InstallFrontendDeps`.

## Prerequisites (one-time)

- .NET 9 SDK on PATH (`dotnet --version`).
- Node 20+ on PATH (`node --version`).
- SQL Server LocalDB, `MSSQLLocalDB` instance running (`sqllocaldb info MSSQLLocalDB`). The connection string in `appsettings.json` targets it.

## Smoke check

Run these from a third PowerShell window after both windows have started:

```powershell
# Backend up?
$resp = curl http://localhost:5217/api/auth/oidc/authorize -SkipHttpErrorCheck
$resp.StatusCode  # 200 (or whatever the configured IdP returns) — anything but a connection error means the backend is listening

# Frontend up?
Start-Process http://localhost:4200
# Browser opens; the sign-in page renders.
```

A green smoke check is:

1. `http://localhost:5217/api/auth/oidc/authorize` returns a JSON body (no connection refused).
2. `http://localhost:4200` renders the sign-in screen.
3. Sign up an account at `/sign-up`, then land on `/todos` with the empty-state mock.

The empty-state landing is the load-bearing assertion: it proves backend + frontend + DB + JWT issuance are all wired correctly.

## Stopping

Each child window can be stopped with Ctrl-C in that window, or close the windows. The parent shell is unaffected.

## Troubleshooting

- **Port 4200 in use.** A previous `ng serve` is still bound. Kill it:
  ```powershell
  Get-NetTCPConnection -LocalPort 4200 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
  ```
- **Port 5217 in use.** Same idea, with port 5217.
- **`SQLUserInstance.dll` blocked by AppControl** (Windows policy). The backend won't start. Either temporarily disable the AppControl policy, or use the test suite (`dotnet test`) which uses EF Core InMemory and bypasses LocalDB. See `docs/runbooks/backend.md` for the workaround.
- **Frontend says `Cannot find module 'api'` / `'components'` / `'domain'`.** The library bundles aren't built yet. Re-run with `-InstallFrontendDeps` or build manually: `cd frontend; npx ng build api; npx ng build components; npx ng build domain`.

## Verification (TP2 acceptance gate)

Done when this runbook's "Single command" section, run on a fresh clone with the listed prerequisites, brings both processes to a "ready" state and the smoke check sequence succeeds. The fix-on-find flow for any prerequisite gap is to update **this** runbook, not bury the workaround in a slack message.
