# Tickbox — Backend Runbook

Operational reference for `Tickbox.Api`. For the single-command bring-up, see `local.md`.

## Stack

- .NET 9 (`net9.0`).
- Clean Architecture: `Tickbox.Domain`, `Tickbox.Application`, `Tickbox.Infrastructure`, `Tickbox.Api`.
- MediatR for CQS, FluentValidation per command (assembly-scanned via `ValidationBehavior` pipeline).
- EF Core SQL Server (LocalDB in dev). No repository / unit-of-work — `IAppDbContext` directly.
- Auth: bcrypt password hashing (work factor 12) + JWT bearer (issuer/audience/lifetime/signature, 30 s clock skew).

## Run

```powershell
cd backend/src/Tickbox.Api
dotnet run
```

Port: **5217** (HTTP). The HTTPS profile (`dotnet run --launch-profile https`) adds `7166`. Both ports are pinned in `Properties/launchSettings.json` so the frontend's `app.config.ts` default API base URL (`http://localhost:5217`) just works.

## Configuration

`appsettings.json` defaults to:

```json
{
  "ConnectionStrings": { "Default": "Server=(localdb)\\MSSQLLocalDB;..." },
  "Jwt": { "Issuer": "...", "Audience": "...", "SigningKey": "<dev-only>", "AccessTokenLifetimeMinutes": 60 }
}
```

In production these are overridden by App Service settings (`ConnectionStrings__Default`, `Jwt__Issuer`, etc.) — see `docs/runbooks/deploy.md`.

## Migrations

EF Core migrations live in `Tickbox.Infrastructure/Persistence/Migrations` (001–009). In Development, `Program.cs` runs `db.Database.Migrate()` on startup so a fresh dev DB is seeded automatically. To run migrations against a specific connection string:

```powershell
dotnet ef database update `
  --project backend/src/Tickbox.Infrastructure `
  --startup-project backend/src/Tickbox.Api `
  --connection "<your conn string>"
```

## Tests

```powershell
dotnet test backend/Tickbox.sln
```

41 acceptance tests, all run via `WebApplicationFactory<Program>` against EF Core **InMemory** (not LocalDB), so the test suite is independent of the dev DB. The factory:

- Skips the SqlServer `DbContext` registration in the `Testing` environment.
- Registers EF Core InMemory in its place.
- Calls `EnsureSeeded()` to insert the `User` role row (InMemory doesn't run `HasData` seeds).
- Creates an HTTP client with `HandleCookies = false; AllowAutoRedirect = false` so refresh-cookie rotation is observable in the test.

## Known issues

- **`SQLUserInstance.dll` blocked by Windows AppControl** on some hardened machines. Symptom: `dotnet run` fails immediately with a CLR-load error referencing the DLL. Workaround: run the test suite (which uses InMemory) instead, or relax the AppControl policy temporarily. The integration tests cover the same code paths the live API exposes.
- **`Roles` static-class shadowing.** `Tickbox.Domain.KnownRoles` (renamed from `Roles` in B-005) — there must be no `Roles` static class re-introduced or it shadows `DbSet<Role> Roles` on the context.

## Health surface

The API does not yet expose a dedicated `/healthz`. The cheapest health probe is `GET /api/auth/oidc/authorize` (always 200 with a JSON body) — used by the smoke check in `local.md` and by App Service's default health-pinging.
