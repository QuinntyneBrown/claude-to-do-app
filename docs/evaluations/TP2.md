# TP2 — Local runtime

Implementer: `claude@M5`.

## Deliverables (shipped)

- `scripts/start-local.ps1` — single-command bring-up. Spawns `dotnet run` (backend) and `ng serve` (frontend) in two new PowerShell windows; returns to the caller. Idempotent: runs `npm ci` and the initial library builds only on first invocation. Force-clean with `-InstallFrontendDeps`.
- `docs/runbooks/local.md` — runs the single command + smoke check; references the per-component runbooks below; troubleshooting for orphan ports and the AppControl/SQL LocalDB issue.
- `docs/runbooks/backend.md` — `Tickbox.Api` operational reference (stack, ports, configuration, migrations, tests, known issues).
- `docs/runbooks/frontend.md` — Angular workspace operational reference (build order, dependency direction, test patterns, known issues).

## Side fix bundled into TP2

`backend/src/Tickbox.Api/Properties/launchSettings.json` had stale ports (5046) inconsistent with the frontend default (`http://localhost:5217` in `app.config.ts`). The runbook required a single-command bring-up to "just work" — that's only true if `dotnet run` lands on 5217 with no extra `--urls` arg. Updated both profiles to 5217. Backend acceptance tests are unaffected (they use `WebApplicationFactory<Program>` which doesn't read launchSettings).

## Verification status

- ✅ `scripts/start-local.ps1` parses cleanly (222 tokens, zero parse errors).
- ✅ Runbook documentation references the per-component runbooks rather than duplicating their content (per the workflow's "reference rather than duplicate" rule).
- ⚠ End-to-end "fresh-clone runs the app from one command" was not executed inside this loop. Two reasons: (a) the loop already has an `ng serve` running on port 4200 from earlier slices that I deliberately don't kill (it's the dev server), and (b) the documented AppControl block on `SQLUserInstance.dll` prevents `dotnet run` against LocalDB on this machine — the same workaround as `docs/runbooks/backend.md` says: use the test suite (which uses InMemory) for the same code-path coverage. The script is verified by syntax and by inspection; the user is the only one who can certify "fresh clone, single command".

The next phase (TP3) executes `docs/qa/test-plan.md` against a running app five times. That will exercise the local runtime in anger, and any TP2 gaps will surface as bugs in `docs/bugs/pass-1.md`.
