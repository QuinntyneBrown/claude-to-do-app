# Tickbox backend — implementation plan (BP1)

Author: `claude@M5` (BP1)
Status: draft, awaiting BP2 evaluation
Inputs: `docs/requirements.md` (approved), `backend/` MVP (accepted), `dotnet-angular-authenticated-full-stack-workflow.html` Implementation Guidance.

This plan is the authoritative source for what backend tasks BT1 will slice. Every plan item below maps to one or more guidance bullets and to one or more `REQ-*` requirements; every backend-relevant requirement appears here.

---

## 1. Project layout

Already established by MB1; **no new projects** are needed. Each new piece of work lands in the layer it belongs to:

```
backend/
  Tickbox.sln                                       (old-style .sln)
  global.json                                       (.NET 9 SDK pin)
  Directory.Build.props                             (net9.0, nullable, warnings as errors)
  src/
    Tickbox.Domain/        ← entities, enums, value objects
    Tickbox.Application/   ← commands/queries/handlers/validators (CQS via MediatR)
    Tickbox.Infrastructure/← DbContext + EF migrations, BCrypt, JWT, IdP client,
                             email service (logging no-op)
    Tickbox.Api/           ← controllers, JWT bearer, exception middleware,
                             CurrentUserService, DI composition root
  tests/
    Tickbox.Api.Tests/     ← WebApplicationFactory + InMemory DB acceptance tests
                             (one test fixture per feature slice)
```

Maps to: Backend §"Clean Architecture (Api / Application / Domain / Infrastructure)", §"Old-style .sln", §"backend/src" / §"backend/tests".

---

## 2. Domain entities and `AppDbContext` shape

All entities live in `Tickbox.Domain`; one type per file. The `AppDbContext` (in `Tickbox.Infrastructure.Persistence`) inherits `DbContext` and implements `IAppDbContext`. Handlers in `Tickbox.Application` consume `IAppDbContext` directly (no repository / unit-of-work).

| Entity                  | Purpose                                                                                                  | Requirements                                                                                  |
|-------------------------|----------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------|
| `User`                  | Account holder. `Email`, `DisplayName`, `PasswordHash`, `CreatedAt`. Unique index on `Email`.            | REQ-AUTH-1, REQ-AUTH-2, REQ-AUTH-7, REQ-ACCT-1                                                |
| `User` (extended)       | Add `PendingEmail`, `PendingEmailToken`, `PendingEmailExpiresAt` for email re-verification.              | REQ-ACCT-2 AC2/AC3                                                                            |
| `Todo`                  | Owned to-do. `Id`, `UserId`, `Title`, `Status`, `Notes`, `DueDate`, `CreatedAt`, `CompletedAt`.          | REQ-TODO-1, REQ-TODO-2, REQ-TODO-3, REQ-TODO-4, REQ-TODO-5, REQ-TODO-6, REQ-TODO-7            |
| `TodoStatus` enum       | `Incomplete`, `Complete`. No third value. Persisted as `int` via `HasConversion<int>()`.                 | REQ-TODO-1                                                                                    |
| `TodoActivityEntry`     | One row per lifecycle event (`Created`, `MarkedComplete`). `TodoId`, `Kind`, `OccurredAt`.               | REQ-TODO-8                                                                                    |
| `TodoActivityKind` enum | `Created`, `MarkedComplete`. Persisted as `int`.                                                         | REQ-TODO-8                                                                                    |
| `Role`                  | RBAC role. `Id`, `Name`. Seeded with one row: `User`.                                                    | Auth §"RBAC implementation from database to frontend"                                         |
| `UserRole`              | Join: `UserId`, `RoleId`. Composite key.                                                                 | Auth §"RBAC..."                                                                               |
| `PasswordResetToken`    | `Id`, `UserId`, `TokenHash`, `ExpiresAt`, `ConsumedAt`.                                                  | REQ-AUTH-4                                                                                    |
| `SecurityAuditEvent`    | `Id`, `UserId?`, `Kind`, `OccurredAt`, `IpAddress`, `Detail`. One row per audit-relevant event.          | REQ-NFR-4                                                                                     |
| `SecurityAuditKind` enum| `SignInFailed`, `SignInLocked`, `PasswordChanged`, `PasswordResetRequested`, `PasswordResetUsed`, `AccountDeleted`. | REQ-NFR-4                                                                              |
| `SignInAttempt`         | `Id`, `Email`, `OccurredAt`, `Succeeded`. Used to compute lockout window.                                | REQ-AUTH-2 AC3                                                                                |
| `RefreshToken`          | `Id`, `UserId`, `TokenHash`, `IssuedAt`, `ExpiresAt`, `RevokedAt?`. Server-side revocation list.         | REQ-AUTH-5, REQ-NFR-6                                                                         |

`IAppDbContext` exposes `DbSet<>` for each entity above. The `AppDbContext` `OnModelCreating` configures keys, unique indexes, max-lengths, value-conversions, and the `User → UserRole` and `User → Todo`/`Todo → TodoActivityEntry` cascade behaviour.

Maps to: Backend §"Microsoft SQL Server", §"`IAppDbContext`", §"Do **not** add repository or unit-of-work abstractions", General §"One type per C# file".

---

## 3. Command + Query inventory

Every command and query lives under `Tickbox.Application/<Feature>/<UseCase>/`, one type per file. Every command has a colocated `<Command>Validator.cs`. Handlers depend on `IAppDbContext`, `ICurrentUserService`, `TimeProvider`, and feature-specific service interfaces.

### 3.1 Auth feature

| Use case                         | Command / Query                  | Returns                              | Requirements                                          | Notes                                                                                                  |
|----------------------------------|----------------------------------|--------------------------------------|-------------------------------------------------------|--------------------------------------------------------------------------------------------------------|
| Register a new account           | `RegisterUserCommand` *(extend)* | `RegisterUserResult`                 | REQ-AUTH-1, REQ-AUTH-7                                | Already in MVP. Extend: assign `User` role; record `SignInAttempt` for the auto-issued token.          |
| Local sign-in                    | `SignInUserCommand` *(extend)*   | `SignInUserResult`                   | REQ-AUTH-2, REQ-AUTH-7, REQ-NFR-4                     | Extend: query `SignInAttempt` for lockout (5 fails / 15 min); on failure write `SignInFailed`; on lock write `SignInLocked`. |
| Begin OIDC                       | `BeginOidcSignInQuery`           | `BeginOidcSignInResult` (auth URL + state) | REQ-AUTH-3                                       | Generates code verifier/challenge; persists `state` server-side keyed by anti-forgery cookie.         |
| Complete OIDC                    | `CompleteOidcSignInCommand`      | `SignInUserResult`                   | REQ-AUTH-3                                            | Exchanges code with IdP via `IOidcClient`; provisions `User` on first sign-in; issues app JWT.         |
| Sign out                         | `SignOutCommand`                 | (no body)                            | REQ-AUTH-5                                            | Revokes the caller's `RefreshToken`.                                                                   |
| Request password reset           | `RequestPasswordResetCommand`    | (no body — always 202)               | REQ-AUTH-4 AC1, REQ-NFR-4                             | Always 202 (no enumeration). Writes `PasswordResetToken` if account exists; emails via `IEmailService` (no-op). |
| Complete password reset          | `CompletePasswordResetCommand`   | `SignInUserResult`                   | REQ-AUTH-4 AC2/AC3, REQ-AUTH-7, REQ-NFR-4             | Validates unexpired/unconsumed token; updates `PasswordHash`; revokes all `RefreshToken`s; logs audit. |

### 3.2 Account feature

| Use case                    | Command / Query                     | Returns                  | Requirements                  | Notes                                                                                            |
|-----------------------------|-------------------------------------|--------------------------|-------------------------------|--------------------------------------------------------------------------------------------------|
| View profile                | `GetMyProfileQuery`                 | `MyProfile`              | REQ-ACCT-1                    | Returns `Email`, `DisplayName`, `PendingEmail` (if any).                                         |
| Update display name         | `UpdateDisplayNameCommand`          | `MyProfile`               | REQ-ACCT-2 AC1                | 1–100 chars; validator-enforced.                                                                 |
| Request email change        | `RequestEmailChangeCommand`         | `MyProfile` (with pending) | REQ-ACCT-2 AC2, REQ-NFR-4   | Sets `User.PendingEmail*`; emails verification link via `IEmailService` (no-op).                 |
| Confirm email change        | `ConfirmEmailChangeCommand`         | `MyProfile`              | REQ-ACCT-2 AC3                | Validates token, swaps `Email`, clears pending fields.                                           |
| Cancel pending email change | `CancelEmailChangeCommand`          | `MyProfile`              | REQ-ACCT-2 AC2                | Clears pending fields.                                                                           |
| Change password             | `ChangePasswordCommand`             | (no body)                | REQ-ACCT-3, REQ-AUTH-7, REQ-NFR-4 | Verifies current password; updates hash; revokes all OTHER `RefreshToken`s; writes `PasswordChanged` audit. |
| Delete account              | `DeleteMyAccountCommand`            | (no body)                | REQ-ACCT-4, REQ-NFR-4         | Cascades all `Todo`s, `RefreshToken`s, `UserRole`s; writes `AccountDeleted` audit.               |

### 3.3 Todos feature

| Use case             | Command / Query                  | Returns                | Requirements                                  | Notes                                                                                          |
|----------------------|----------------------------------|------------------------|-----------------------------------------------|------------------------------------------------------------------------------------------------|
| List all my todos    | `GetTodosQuery` *(extend)*       | `IReadOnlyList<TodoListItem>` | REQ-TODO-3, REQ-NFR-5                  | Server-side ordering: `DueDate` asc nulls-last, then `CreatedAt` desc. No date filtering — list shows all the user's todos. |
| Get todo by id       | `GetTodoByIdQuery`               | `TodoDetail` (with activity) | REQ-TODO-4, REQ-TODO-8, REQ-NFR-5      | 404 if not found OR not owned (no enumeration).                                                |
| Create todo          | `CreateTodoCommand` *(extend)*   | `CreateTodoResult`     | REQ-TODO-1, REQ-TODO-2                        | Already in MVP. Extend: accept `Notes`, `DueDate`. Writes `TodoActivityEntry { Created }`.     |
| Update todo          | `UpdateTodoCommand`              | `TodoDetail`           | REQ-TODO-4                                    | Updates `Title`, `Notes`, `DueDate` only. Status changes go through `ToggleTodoStatusCommand`. |
| Toggle todo status   | `ToggleTodoStatusCommand`        | `TodoDetail`           | REQ-TODO-1, REQ-TODO-5, REQ-TODO-8            | Body: `{ status: "Incomplete" | "Complete" }`. On `Complete`: set `CompletedAt`, write `MarkedComplete` activity. On `Incomplete`: clear `CompletedAt`, remove latest `MarkedComplete` activity. |
| Delete todo          | `DeleteTodoCommand`              | (no body)              | REQ-TODO-6                                    | Hard delete, no undo.                                                                          |

### 3.4 Result DTOs (one per file in `Tickbox.Application`)

`MyProfile`, `TodoListItem` *(extended with `DueDate`, `CompletedAt`, `Notes`)*, `TodoDetail`, `TodoActivityItem`, `BeginOidcSignInResult`, `RegisterUserResult`, `SignInUserResult`, `CreateTodoResult`.

Maps to: Backend §"Command/Query Separation (CQS) using MediatR", General §"One type per C# file".

---

## 4. Validator inventory

Every command above gets a colocated `<Command>Validator.cs` extending `AbstractValidator<T>`. Queries with input that needs server-side validation also get a validator. All validators are picked up by assembly scanning (`AddValidatorsFromAssembly`). Failures throw `ValidationException` and the existing `ExceptionToProblemDetailsMiddleware` returns HTTP 400 `application/problem+json`.

| Validator                                | Rules                                                                                       | Requirements                                       |
|------------------------------------------|---------------------------------------------------------------------------------------------|----------------------------------------------------|
| `RegisterUserCommandValidator`           | Email required + `EmailAddress()` + ≤256; DisplayName required + ≤100; Password 12–256.    | REQ-AUTH-1, REQ-AUTH-7                             |
| `SignInUserCommandValidator`             | Email required + `EmailAddress()`; Password required.                                       | REQ-AUTH-2                                         |
| `CompleteOidcSignInCommandValidator`     | Code required; State required.                                                              | REQ-AUTH-3                                         |
| `RequestPasswordResetCommandValidator`   | Email required + `EmailAddress()`.                                                          | REQ-AUTH-4                                         |
| `CompletePasswordResetCommandValidator`  | Token required; NewPassword 12–256.                                                         | REQ-AUTH-4, REQ-AUTH-7                             |
| `UpdateDisplayNameCommandValidator`      | DisplayName required + ≤100.                                                                | REQ-ACCT-2 AC1                                     |
| `RequestEmailChangeCommandValidator`     | NewEmail required + `EmailAddress()` + ≤256 + not equal to current.                         | REQ-ACCT-2 AC2                                     |
| `ConfirmEmailChangeCommandValidator`     | Token required.                                                                             | REQ-ACCT-2 AC3                                     |
| `ChangePasswordCommandValidator`         | CurrentPassword required; NewPassword 12–256 + ≠ CurrentPassword.                           | REQ-ACCT-3, REQ-AUTH-7                             |
| `CreateTodoCommandValidator` *(extend)*  | Title 1–200; Notes ≤2000; DueDate ≥ today (allowed nullable).                               | REQ-TODO-2 AC4                                     |
| `UpdateTodoCommandValidator`             | Same shape as `CreateTodoCommandValidator`.                                                 | REQ-TODO-4                                         |
| `ToggleTodoStatusCommandValidator`       | Status ∈ {`Incomplete`, `Complete`}.                                                        | REQ-TODO-1 AC2, REQ-TODO-5                         |

Maps to: Validation §"Use FluentValidation. Do **not** use System.ComponentModel.DataAnnotations", §"One AbstractValidator per command", §"register validators by assembly scanning", §"HTTP request DTOs are pure transport shapes — no validation attributes".

---

## 5. Controller surface

Three controllers in `Tickbox.Api.Controllers`. Each controller method delegates to MediatR; no business logic in controllers. HTTP request DTOs (one per file, no annotations) live alongside the controller. Authorization is `[Authorize]` by default; explicit `[AllowAnonymous]` on auth endpoints.

### `AuthController` (`/api/auth`)

| Method | Route                          | Anonymous? | Sends                              | Returns               | Requirement       |
|--------|--------------------------------|------------|------------------------------------|------------------------|-------------------|
| POST   | `/register`                    | ✓          | `RegisterUserCommand`              | `RegisterUserResult`   | REQ-AUTH-1        |
| POST   | `/sign-in`                     | ✓          | `SignInUserCommand`                | `SignInUserResult`     | REQ-AUTH-2        |
| GET    | `/oidc/authorize`              | ✓          | `BeginOidcSignInQuery`             | redirect URL + state   | REQ-AUTH-3        |
| POST   | `/oidc/callback`               | ✓          | `CompleteOidcSignInCommand`        | `SignInUserResult`     | REQ-AUTH-3        |
| POST   | `/sign-out`                    |            | `SignOutCommand`                   | 204                    | REQ-AUTH-5        |
| POST   | `/password-reset/request`      | ✓          | `RequestPasswordResetCommand`      | 202                    | REQ-AUTH-4 AC1    |
| POST   | `/password-reset/complete`     | ✓          | `CompletePasswordResetCommand`     | `SignInUserResult`     | REQ-AUTH-4 AC2/3  |

### `AccountController` (`/api/account`)

| Method | Route                          | Sends                              | Returns                | Requirement                    |
|--------|--------------------------------|------------------------------------|------------------------|--------------------------------|
| GET    | `/me`                          | `GetMyProfileQuery`                | `MyProfile`            | REQ-ACCT-1                     |
| PUT    | `/display-name`                | `UpdateDisplayNameCommand`         | `MyProfile`            | REQ-ACCT-2 AC1                 |
| POST   | `/email-change/request`        | `RequestEmailChangeCommand`        | `MyProfile`            | REQ-ACCT-2 AC2                 |
| POST   | `/email-change/confirm`        | `ConfirmEmailChangeCommand`        | `MyProfile`            | REQ-ACCT-2 AC3                 |
| DELETE | `/email-change`                | `CancelEmailChangeCommand`         | `MyProfile`            | REQ-ACCT-2 AC2                 |
| PUT    | `/password`                    | `ChangePasswordCommand`            | 204                    | REQ-ACCT-3                     |
| DELETE | (root)                         | `DeleteMyAccountCommand`           | 204                    | REQ-ACCT-4                     |

### `TodosController` (`/api/todos`)

| Method | Route                          | Sends                              | Returns                          | Requirement                         |
|--------|--------------------------------|------------------------------------|----------------------------------|-------------------------------------|
| GET    | (root)                         | `GetTodosQuery`                    | `TodoListItem[]`                 | REQ-TODO-3                          |
| GET    | `/{id}`                        | `GetTodoByIdQuery`                 | `TodoDetail`                     | REQ-TODO-4, REQ-TODO-8              |
| POST   | (root)                         | `CreateTodoCommand`                | `CreateTodoResult`               | REQ-TODO-1, REQ-TODO-2              |
| PUT    | `/{id}`                        | `UpdateTodoCommand`                | `TodoDetail`                     | REQ-TODO-4                          |
| PATCH  | `/{id}/status`                 | `ToggleTodoStatusCommand`          | `TodoDetail`                     | REQ-TODO-5                          |
| DELETE | `/{id}`                        | `DeleteTodoCommand`                | 204                              | REQ-TODO-6                          |

Empty list (REQ-TODO-7) is a UI state — the API returns `200 []`. No special endpoint needed.

Maps to: Backend §"ASP.NET Core Controllers for HTTP endpoints", Validation §"map [validation failures] to HTTP 400 ValidationProblemDetails", REQ-AUTH-6.

---

## 6. Auth flows

### 6.1 Local username/password

1. **Register.** `POST /api/auth/register` → `RegisterUserCommand` → handler hashes password (bcrypt, work factor 12), inserts `User`, attaches the `User` role via `UserRole`, issues access JWT and refresh token. Audit `SignInFailed` is NOT written on register.
2. **Sign-in.** `POST /api/auth/sign-in` → handler:
   - Reads `SignInAttempt` rows for this email in the last 15 minutes. If 5+ failed, return 401 + write `SignInLocked` audit. Generic message ("Incorrect email or password.").
   - Else verify password. On success: write `SignInAttempt { Succeeded = true }`, issue access + refresh tokens.
   - On failure: write `SignInAttempt { Succeeded = false }`, write `SignInFailed` audit, return 401 generic.
3. **Token shape.** Access JWT (HS256, 15-minute lifetime, claims: `sub`, `email`, `display_name`, `role`, `jti`). Refresh token (opaque, 14-day lifetime, hashed in `RefreshToken` table).
4. **Sign-out.** `POST /api/auth/sign-out` revokes the caller's `RefreshToken`. Access JWT remains valid until expiry (typical short-lifetime JWT model).

### 6.2 PKCE OIDC

1. **Begin.** `GET /api/auth/oidc/authorize`:
   - Generate `code_verifier` (random 43–128 char) and `code_challenge` (S256).
   - Generate `state` (CSRF token).
   - Persist `(state → { code_verifier, expires_at })` in a short-TTL store (5 minutes). Implementation: use `ProtectedSessionStorage`-style server cache (simplest: a dedicated `OidcAuthorizationRequest` table, deleted on completion; or `IDistributedCache` if scaled).
   - Return the IdP authorization URL with `client_id`, `redirect_uri`, `response_type=code`, `scope=openid email profile`, `state`, `code_challenge`, `code_challenge_method=S256`.
2. **Callback.** `POST /api/auth/oidc/callback` with `{ code, state }`:
   - Look up the stored verifier; reject if missing/expired.
   - Exchange code at the IdP token endpoint via `IOidcClient` using the verifier.
   - Validate the ID token (issuer, audience, signature, expiration, nonce — same parameters family as REQ-AUTH-6).
   - First-time sign-in: provision a `User` with `Email`, `DisplayName` from claims, no password hash (disabled-local-flow flag), and the `User` role.
   - Issue Tickbox app access + refresh tokens (same shape as local).

### 6.3 RBAC

- One role seeded: `User`.
- The MVP's `[Authorize]` attribute keeps controllers signed-in only. After RBAC lands, `[Authorize(Roles = "User")]` is applied at the controller level on `AccountController` and `TodosController`. `AuthController` remains mixed (anonymous register/sign-in, `[Authorize]` for sign-out).
- The seeded `User` role exists in DB; the `RegisterUserCommand` and OIDC provisioning attach that role on account creation. Future roles (e.g., `Admin`) require only seeding + `[Authorize(Roles = "...")]` on the relevant endpoint — no infrastructure change.
- Role claims are emitted in the access JWT so the frontend can also gate UI on role membership (REQ-NFR-7 / REQ-AUTH-* end-to-end RBAC).

### 6.4 JWT validation (REQ-AUTH-6)

Already wired in `Program.cs` MVP: `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey`, 30-second `ClockSkew`. No change needed — the implementation slices add audit logging and `[Authorize(Roles="User")]` annotations only.

Maps to: Auth §"PKCE-based OAuth 2.0 / OIDC", §"Local username + password", §"Salted hashes from a modern password-hashing function", §"JWTs validated on every request", §"Repeated failed sign-ins are rate-limited", §"Full user management", §"RBAC".

---

## 7. Migration sequencing

Each migration is its own slice; they ship in the order below. Slice naming: `<NNN>_<MigrationName>`. Migrations live under `src/Tickbox.Infrastructure/Persistence/Migrations/`.

| #   | Migration                          | Adds                                                                                          | Slice that introduces it             |
|-----|------------------------------------|-----------------------------------------------------------------------------------------------|--------------------------------------|
| 001 | `InitialCreate`                    | `Users`, `Todos`. **Already shipped in MB1.**                                                 | (MB1)                                |
| 002 | `AddRolesAndUserRoles`             | `Roles`, `UserRoles`; seed `User` role; assign role to existing users.                        | RBAC slice                           |
| 003 | `AddSignInAttemptsAndAuditEvents`  | `SignInAttempts`, `SecurityAuditEvents` + `SecurityAuditKind` int enum.                       | Sign-in lockout + audit slice        |
| 004 | `AddRefreshTokens`                 | `RefreshTokens` table; `RevokedAt` nullable.                                                  | Token refresh / sign-out slice       |
| 005 | `AddPasswordResetTokens`           | `PasswordResetTokens` table.                                                                  | Password reset slice                 |
| 006 | `AddPendingEmailFields`            | `Users.PendingEmail`, `PendingEmailToken`, `PendingEmailExpiresAt` (all nullable).            | Email-change slice                   |
| 007 | `AddTodoNotesAndDueDate`           | `Todos.Notes` (≤2000), `Todos.DueDate` (date, nullable). Already has `CompletedAt` from MB1.  | Todo full-shape slice                |
| 008 | `AddTodoActivityEntries`           | `TodoActivityEntries` table; `TodoActivityKind` int enum; FK to `Todos` cascade-delete.       | Todo activity strip slice            |

Order rationale: RBAC and audit infra come first because every later slice's audit-write and `[Authorize(Roles=...)]` depend on them. Refresh tokens before password reset because reset revokes refresh tokens. Email-change after refresh tokens for the same reason. Todo schema extensions come after the auth/account stack stabilises.

Maps to: Backend §"Entity Framework Core DbContext for data access", §"Microsoft SQL Server".

---

## 8. Deferred (no-op) integrations

Per General §"optional integrations explicitly deferred ... may be replaced by a clearly-named no-op service that logs the intended action".

| Integration                | Interface (`Tickbox.Application`) | Real impl (deferred)                 | No-op impl (v1)                                     | Used by                                                       |
|----------------------------|-----------------------------------|--------------------------------------|-----------------------------------------------------|---------------------------------------------------------------|
| Transactional email        | `IEmailService`                   | SMTP / SES / SendGrid wrapper        | `LoggingEmailService` in `Tickbox.Infrastructure`   | `RequestPasswordResetCommand`, `RequestEmailChangeCommand`    |
|                            |                                   |                                      | Logs: `"[email] would send {Template} to {Address}: {Tokens}"` and writes a structured log entry. |                                                               |

`IEmailService` defines: `SendPasswordResetAsync(string email, string token, CancellationToken)` and `SendEmailChangeVerificationAsync(string email, string token, CancellationToken)`. The no-op only logs; the token is generated and persisted in the database regardless, so the link can still be assembled from the database in dev/test.

OIDC is **not** a no-op: when OIDC is enabled in an environment (per config `Oidc:Enabled = true`), `IOidcClient` is wired to the real implementation. When disabled, `IOidcClient` is not registered and the OIDC endpoints return 404 (or hide entirely in the UI per REQ-AUTH-3 AC2). No no-op stand-in.

Maps to: General §"optional integrations explicitly deferred ... may be replaced by a clearly-named no-op service".

---

## 9. Requirements coverage matrix

Every backend-relevant requirement from `docs/requirements.md` maps to at least one plan section above.

| Requirement   | Plan section(s)                                                                                                |
|---------------|---------------------------------------------------------------------------------------------------------------|
| REQ-AUTH-1    | §3.1 `RegisterUserCommand` · §4 `RegisterUserCommandValidator` · §5 `POST /api/auth/register` · §6.1 · §7 #002 |
| REQ-AUTH-2    | §3.1 `SignInUserCommand` · §4 · §5 · §6.1 (lockout + audit) · §7 #003                                          |
| REQ-AUTH-3    | §3.1 `BeginOidcSignInQuery`/`CompleteOidcSignInCommand` · §5 · §6.2 · §8 (no no-op; env-toggled real)          |
| REQ-AUTH-4    | §3.1 `RequestPasswordResetCommand`/`CompletePasswordResetCommand` · §4 · §5 · §6.1 · §7 #005 · §8 (email)      |
| REQ-AUTH-5    | §3.1 `SignOutCommand` · §5 · §6.1 · §7 #004                                                                    |
| REQ-AUTH-6    | §6.4 (already wired in MB1; no change)                                                                         |
| REQ-AUTH-7    | §4 (every password validator) · §6.1                                                                           |
| REQ-TODO-1    | §2 `TodoStatus` enum · §4 `ToggleTodoStatusCommandValidator` · §3.3                                            |
| REQ-TODO-2    | §3.3 `CreateTodoCommand` · §4 · §5 · §7 #007                                                                   |
| REQ-TODO-3    | §3.3 `GetTodosQuery` · §5 · server-side ordering rule                                                          |
| REQ-TODO-3a   | (Frontend; not backend.)                                                                                       |
| REQ-TODO-4    | §3.3 `GetTodoByIdQuery`/`UpdateTodoCommand` · §4 · §5                                                          |
| REQ-TODO-5    | §3.3 `ToggleTodoStatusCommand` · §4 · §5                                                                       |
| REQ-TODO-6    | §3.3 `DeleteTodoCommand` · §5                                                                                  |
| REQ-TODO-7    | (Frontend; backend returns `200 []`.)                                                                          |
| REQ-TODO-8    | §2 `TodoActivityEntry` · §3.3 `GetTodoByIdQuery`, `CreateTodoCommand`, `ToggleTodoStatusCommand` · §7 #008     |
| REQ-ACCT-1    | §3.2 `GetMyProfileQuery` · §5                                                                                  |
| REQ-ACCT-2    | §3.2 `UpdateDisplayNameCommand`, `RequestEmailChangeCommand`, `ConfirmEmailChangeCommand`, `CancelEmailChangeCommand` · §4 · §5 · §7 #006 · §8 (email) |
| REQ-ACCT-3    | §3.2 `ChangePasswordCommand` · §4 · §5 · §6.1 (token revocation)                                               |
| REQ-ACCT-4    | §3.2 `DeleteMyAccountCommand` · §5                                                                             |
| REQ-ERR-1     | (Frontend; backend already returns `application/problem+json` from MB1.)                                       |
| REQ-ERR-2     | (MB1 wiring covers this; every new command's `Validator` produces the 400 surface automatically.)              |
| REQ-NFR-3     | §6.1 (bcrypt work factor 12; already in MB1).                                                                  |
| REQ-NFR-4     | §2 `SecurityAuditEvent` · §6.1 (lockout, password change, password reset, account deletion all write events). |
| REQ-NFR-5     | §3.3 every Todo handler scopes by `_currentUser.UserId`; §3.2 every Account handler reads/writes the caller only. |
| REQ-NFR-6     | §6.1 refresh tokens are HttpOnly Secure cookie (frontend), hashed in DB. Access JWT is in-memory at the client (frontend concern; backend just sets `Set-Cookie` for refresh tokens with `HttpOnly; Secure; SameSite=Strict`). |
| REQ-NFR-8     | Build cleanliness inherited from MB1; new slices keep `dotnet build -warnaserror` 0/0.                        |

---

## 10. Plan-item → guidance-rule map

Sanity check: every plan item references at least one rule from the Backend / Validation / Authentication / General sections of the Implementation Guidance.

- §1 layout → Backend §"Clean Architecture (Api / Application / Domain / Infrastructure)", §"Old-style .sln", §"backend/src" / §"backend/tests".
- §2 entities → Backend §"Microsoft SQL Server", §"Entity Framework Core DbContext", General §"One type per C# file".
- §3 commands/queries → Backend §"Command/Query Separation (CQS) using MediatR", §"Application layer". General §"radically simple".
- §3 handlers depend on `IAppDbContext` → Backend §"Command and query handlers depend on an IAppDbContext interface", §"Do not add repository or unit-of-work abstractions".
- §4 validators → Validation §"FluentValidation", §"One AbstractValidator per command", §"register validators by assembly scanning", §"HTTP request DTOs ... no validation attributes".
- §5 controllers → Backend §"ASP.NET Core Controllers for HTTP endpoints (no minimal APIs)".
- §6 auth → Auth §"PKCE-based OAuth 2.0 / OIDC", §"Local username + password", §"Salted hashes", §"JWTs validated on every request", §"Repeated failed sign-ins are rate-limited", §"Full user management", §"RBAC".
- §7 migrations → Backend §"Entity Framework Core".
- §8 deferred → General §"optional integrations explicitly deferred ... no-op service that logs the intended action".

No plan item conflicts with the guidance:
- No repository / unit-of-work in §3.
- No data annotations on commands or DTOs in §3 / §5.
- No multiple-types-per-file in §1 (every entity, command, validator, handler, DTO, enum, interface, exception is its own file).
- No use of `System.ComponentModel.DataAnnotations` anywhere.
- No backend speculation: every requirement maps in §9; no plan section adds anything that isn't in `docs/requirements.md` or the guidance.

---

## 11. Acceptance gates

BP1 is done when this document is committed. BP2 evaluates §9 (every requirement appears) and §10 (every plan item maps and nothing conflicts). BT1 then takes this plan and decomposes each plan item into a vertically-sliced task. BI1 implements each slice via ATDD against the Backend / Validation / Authentication / General sections of the guidance.
