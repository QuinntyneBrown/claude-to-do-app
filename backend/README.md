# Tickbox backend

ASP.NET Core 9 backend for Tickbox. Reference implementation for the patterns every backend slice will follow downstream of MB1.

## Architecture

Clean Architecture with one type per file across the entire backend.

```
backend/
  Tickbox.sln
  global.json                  # pins .NET 9 SDK
  Directory.Build.props        # net9.0, nullable, warnings as errors
  src/
    Tickbox.Domain/            # entities, enums (Todo, User, TodoStatus)
    Tickbox.Application/       # CQS via MediatR, FluentValidation, IAppDbContext
    Tickbox.Infrastructure/    # AppDbContext (EF Core / SQL Server),
                               # BcryptPasswordHasher, JwtTokenService
    Tickbox.Api/               # ASP.NET Core controllers, JWT bearer auth,
                               # ProblemDetails exception middleware
  tests/
    Tickbox.Api.Tests/         # WebApplicationFactory + InMemory DB
                               # acceptance tests (ATDD)
```

Layer dependencies:

```
Api ──▶ Application ──▶ Domain
       └▶ Infrastructure ──▶ Application, Domain
```

Handlers depend on **`IAppDbContext`** (an EF Core abstraction over the concrete `AppDbContext`). There are no repository or unit-of-work classes — handlers use the `DbContext` directly via the interface.

## Patterns proved by the MVP slice

The MVP ships **two real, end-to-end slices** that the rest of the implementation follows.

1. **Auth** — `POST /api/auth/register`, `POST /api/auth/sign-in`. Passwords are stored as bcrypt hashes (work factor 12). Sign-in issues a signed JWT (HS256). The bearer token is validated on every protected request: issuer, audience, signature, expiration.
2. **Todos** — `GET /api/todos`, `POST /api/todos`. Both require a valid JWT. Handlers scope every query to the authenticated user via `ICurrentUserService`, so cross-user reads are impossible.

Each slice goes through:

```
HTTP request
   ▼
ASP.NET Core controller
   ▼
MediatR (with ValidationBehavior pipeline → FluentValidation → ValidationException → 400)
   ▼
Command/Query handler
   ▼
IAppDbContext (EF Core → SQL Server)
```

`ExceptionToProblemDetailsMiddleware` translates application exceptions into `application/problem+json`:

| Exception                       | HTTP |
| ------------------------------- | ---- |
| `ValidationException`           | 400  |
| `AuthenticationFailedException` | 401  |
| `NotFoundException`             | 404  |
| `ConflictException`             | 409  |
| (any other)                     | 500  |

## Conventions

- **One type per file.** Every class, interface, enum, record, and delegate gets its own file. The file name matches the type name.
- **Validation via FluentValidation.** One `AbstractValidator<TCommand>` per command, colocated with the command in `Tickbox.Application`. The MediatR pipeline runs validators before the handler.
- **No data annotations** on commands, queries, or request DTOs. HTTP request DTOs are pure transport shapes; mapping happens in the controller.
- **No repository or unit-of-work classes.** Handlers consume `IAppDbContext` directly.
- **All warnings are errors.** `dotnet build` produces zero warnings.

## Run locally

Prerequisites:

- .NET 9 SDK (pinned via `global.json`).
- SQL Server LocalDB (`MSSQLLocalDB`). The default connection string in `src/Tickbox.Api/appsettings.json` targets it.

```powershell
cd backend
dotnet run --project src/Tickbox.Api
```

On startup in Development the API runs `Database.Migrate()` against LocalDB, creating the `Tickbox` database with the `Users` and `Todos` tables. Hit:

```
POST   http://localhost:5000/api/auth/register   { "email", "displayName", "password" }
POST   http://localhost:5000/api/auth/sign-in    { "email", "password" }
GET    http://localhost:5000/api/todos           (Bearer <accessToken>)
POST   http://localhost:5000/api/todos           (Bearer <accessToken>)  { "title" }
```

The JWT signing key in `appsettings.json` is for development only — replace with a real secret (≥ 32 bytes of entropy) for any non-dev environment, supplied via user secrets, environment variables, or a key vault.

## Test

```powershell
cd backend
dotnet test
```

Tests use `WebApplicationFactory<Program>` against an EF Core InMemory database, so they run without LocalDB. The `Testing` environment skips the SQL Server registration so the factory can wire InMemory cleanly.

## Migrations

```powershell
dotnet ef migrations add <Name> --project src/Tickbox.Infrastructure --startup-project src/Tickbox.Api --output-dir Persistence/Migrations --context AppDbContext
dotnet ef database update      --project src/Tickbox.Infrastructure --startup-project src/Tickbox.Api --context AppDbContext
```

## Deferred (no-op) integrations

None yet. When transactional email is added in a later slice (REQ-AUTH-4 password reset, REQ-ACCT-2 email change), it will use a logging no-op service named accordingly (`LoggingEmailService`) until a real provider is wired in. That swap is the only acceptable form of deferred work in this backend.
