# Tickbox backend — vertically sliced tasks (BT1)

Author: `claude@M5` (BT1, BT2)
Status: approved (BT2 pass 2 clean)
Inputs: `docs/plans/backend.md` (approved), `docs/requirements.md` (approved), `backend/` MVP (accepted).

This list takes every plan item from `docs/plans/backend.md` and decomposes it into vertical slices. **Every task ships end-to-end:** controller endpoint(s) → command/query + validator + handler (Application) → entity / DbContext changes (Domain / Infrastructure) → migration (if needed) → acceptance test (Tickbox.Api.Tests). No "scaffolding only" tasks; no horizontal layer-only work.

For each task, BI1 will:

1. Write the `Tickbox.Api.Tests` acceptance test first (true ATDD).
2. Implement until green; commit + push.
3. Run the Implementation Evaluation Rubric scoped to backend; fix-on-find; commit + push.
4. Mark the task `done` here.

Order is dependency-driven. Each task is sized to land in 1–3 loop iterations.

## Common guidance rules (apply to every task)

Every task below MUST satisfy these guidance rules from the workflow's Implementation Guidance — they are not repeated per task to avoid noise:

- **General.** Radically simple, no stubs / `TODO` / `NotImplementedException` / empty bodies, every requirement implemented in entirety, one type per `.cs` file.
- **Backend.** Clean Architecture (handlers in Application; entities in Domain; SQL Server / EF / JWT / bcrypt in Infrastructure; controllers in Api). MediatR CQS. ASP.NET Core controllers. EF Core via `IAppDbContext`. **No** repository / unit-of-work classes. `Microsoft.Extensions.*` for logging / configuration / DI. Old-style `.sln`.
- **Validation.** FluentValidation, one `AbstractValidator<TCommand>` per command, colocated. No data annotations on commands / queries / DTOs. Pipeline behaviour throws `ValidationException` → 400 `application/problem+json` (already wired by MB1).
- **Authentication.** JWT validation on every protected endpoint (already wired). Bcrypt for password hashing. Generic 401 on auth failures (no enumeration).
- **Testing.** `Tickbox.Api.Tests` integration test first (`WebApplicationFactory<Program>` + EF Core InMemory), then implementation, then evaluation, then mark done.

## Tasks

### B-001 — RBAC scaffolding + role on register — **done** (see `docs/evaluations/BI1-B001.md`)

- **Implements:** Auth §"RBAC implementation from database to frontend"; supporting REQ-AUTH-1, REQ-AUTH-3.
- **Slice contents:**
  - **Domain:** `Role`, `UserRole` entities; one type per file.
  - **Infrastructure:** `AppDbContext` `OnModelCreating` for `Roles` and `UserRoles` (composite PK on `UserRoles`).
  - **Migration #002 `AddRolesAndUserRoles`:** create `Roles`, `UserRoles`, seed `Roles` with `("00000000-0000-0000-0000-000000000001", "User")`, attach role to every existing user, **make `Users.PasswordHash` nullable** (needed for B-005's OIDC-only accounts).
  - **Application:** extend `RegisterUserCommandHandler` to insert a `UserRole` row for the seeded `User` role; emit a `role` claim from `JwtTokenService` (Infrastructure) for downstream slices to use. No new commands.
  - **Api:** add `[Authorize(Roles = "User")]` on `TodosController` (already authorized; this is a tightening, not a new endpoint).
  - **Acceptance test:** `Register_creates_user_with_User_role_and_token_carries_role_claim` — registers, decodes the JWT, asserts the `role` claim is `"User"`, asserts `[Authorize(Roles="User")]` on `GET /api/todos` still succeeds.
- **Specific guidance rules:** Auth §"RBAC...", General §"One type per C# file".

### B-002 — Audit log + sign-in lockout + `IRequestContext` — **done** (see `docs/evaluations/BI1-B002.md`)

- **Implements:** REQ-AUTH-2 AC3 (5/15-min lockout), REQ-NFR-4.
- **Depends on:** B-001 (audit-event handlers run inside `[Authorize(Roles="User")]` controllers later).
- **Slice contents:**
  - **Domain:** `SignInAttempt`, `SecurityAuditEvent`, `SecurityAuditKind` enum.
  - **Application:** `IRequestContext` interface (`RemoteIp`, `UserAgent`); extend `SignInUserCommandHandler` to (1) read recent `SignInAttempt`s, (2) lock out at 5 fails/15 min with `SignInLocked` audit, (3) write `SignInFailed` on bad credentials, (4) handle `null` `PasswordHash` (OIDC-only) as 401-generic.
  - **Infrastructure:** `AppDbContext` config for the two new tables.
  - **Migration #003 `AddSignInAttemptsAndAuditEvents`.**
  - **Api:** `RequestContext : IRequestContext` reading `IHttpContextAccessor`; registered scoped.
  - **Acceptance tests:** `Sign_in_locks_out_after_five_failures_in_window`, `Sign_in_with_null_password_hash_returns_401_generic`.
- **Specific guidance rules:** Auth §"Repeated failed sign-ins are rate-limited, locked out, or otherwise throttled, with a security audit log entry on each event".

### B-003 — Refresh tokens + sign-out

- **Implements:** REQ-AUTH-5, REQ-NFR-6, partly REQ-AUTH-2 (token rotation on success).
- **Slice contents:**
  - **Domain:** `RefreshToken` (`Id`, `UserId`, `TokenHash`, `IssuedAt`, `ExpiresAt`, `RevokedAt?`).
  - **Migration #004 `AddRefreshTokens`.**
  - **Application:** `RefreshAccessTokenCommand` + handler + `RefreshAccessTokenCommandValidator`; `SignOutCommand` + handler. Both manipulate `RefreshToken` rows.
  - **Application:** extend `RegisterUserCommandHandler`, `SignInUserCommandHandler` to insert a `RefreshToken` row and return its plaintext value (for the cookie); access-JWT lifetime drops to 15 min.
  - **Api:** `AuthController.Refresh` (`POST /api/auth/refresh`, `[AllowAnonymous]`, reads cookie); `AuthController.SignOut` (`POST /api/auth/sign-out`, `[Authorize]`); a small `RefreshTokenCookieWriter` service in Api that sets `Set-Cookie: tickbox.refresh=<value>; HttpOnly; Secure; SameSite=Strict; Path=/api/auth; Max-Age=...`.
  - **Acceptance tests:** `Refresh_rotates_token_and_returns_new_access_jwt`, `Refresh_with_revoked_token_returns_401`, `Sign_out_revokes_caller_refresh_token_and_clears_cookie`.
- **Specific guidance rules:** Auth §"JWTs validated on every request"; REQ-NFR-6.

### B-004 — Password reset (request + complete) + `IEmailService` no-op

- **Implements:** REQ-AUTH-4 AC1/AC2/AC3, REQ-AUTH-7, REQ-NFR-4.
- **Depends on:** B-002 (audit), B-003 (revoke-all on reset).
- **Slice contents:**
  - **Domain:** `PasswordResetToken` (`Id`, `UserId`, `TokenHash`, `ExpiresAt`, `ConsumedAt?`).
  - **Migration #005 `AddPasswordResetTokens`.**
  - **Application:** `IEmailService` interface (`SendPasswordResetAsync`, `SendEmailChangeVerificationAsync`).
  - **Infrastructure:** `LoggingEmailService : IEmailService` (no-op; logs the address + template + token via `ILogger`). Wired in `AddInfrastructure`.
  - **Application:** `RequestPasswordResetCommand` + handler + validator (always 202; no enumeration; writes `PasswordResetRequested` audit; calls `IEmailService`); `CompletePasswordResetCommand` + handler + validator (validates unexpired + unconsumed token; updates `PasswordHash`; revokes ALL `RefreshToken`s for the user; writes `PasswordResetUsed` audit; issues fresh access + refresh tokens).
  - **Api:** `AuthController.RequestPasswordReset`, `AuthController.CompletePasswordReset` — both `[AllowAnonymous]`.
  - **Acceptance tests:** `Request_password_reset_returns_202_for_unknown_and_known_emails`, `Complete_password_reset_with_valid_token_signs_user_in_and_revokes_existing_sessions`, `Complete_password_reset_with_expired_token_returns_400`.
- **Specific guidance rules:** Auth §"Salted hashes from a modern password-hashing function"; REQ-AUTH-7 password policy.

### B-005 — OIDC PKCE sign-in (begin + callback)

- **Implements:** REQ-AUTH-3 AC1/AC2.
- **Depends on:** B-001 (nullable `PasswordHash`, role assignment), B-003 (refresh-token issuing).
- **Slice contents:**
  - **Application:** `IOidcClient` interface (`ExchangeCodeAsync` returns user claims).
  - **Infrastructure:** real `OidcClient` implementing `IOidcClient` (issuer-metadata discovery + token exchange + ID-token validation). Registered only if `Oidc:Enabled = true` in configuration.
  - **Application:** `BeginOidcSignInQuery` + handler (returns auth URL + state; persists `(state → code_verifier)` in a short-TTL store — implementation: a small `OidcAuthorizationRequest` table with `(State, CodeVerifier, ExpiresAt)` and a single migration row; or `IDistributedCache` if available — pick the table for v1 to avoid an extra dependency).
  - **Migration #009 `AddOidcAuthorizationRequests`:** ships unconditionally (the table exists in every environment; only the `OidcClient` registration is gated by `Oidc:Enabled`). Schema: `(State PK, CodeVerifier, ExpiresAt)`.
  - **Application:** `CompleteOidcSignInCommand` + handler + validator (looks up state, calls `IOidcClient`, provisions `User` on first sign-in with `PasswordHash = null` and `User` role, issues access + refresh tokens).
  - **Api:** `AuthController.BeginOidc` (`GET /api/auth/oidc/authorize`), `AuthController.CompleteOidc` (`POST /api/auth/oidc/callback`). When `Oidc:Enabled = false`, the endpoints return 404 (or are not mapped). REQ-AUTH-3 AC2 — frontend uses the same config flag to hide the SSO button.
  - **Acceptance tests** (env: `Oidc:Enabled = true` with a fake `IOidcClient` registered in the test factory): `Begin_oidc_returns_authorization_url_and_persists_state`, `Complete_oidc_first_time_provisions_user_with_role_and_no_password`, `Complete_oidc_returning_user_signs_in_without_provisioning`. Plus one disabled-env test: `Oidc_endpoints_return_404_when_disabled`.
- **Specific guidance rules:** Auth §"PKCE-based OAuth 2.0 / OIDC authorization code flow against an external identity provider".

### B-006 — Account: get profile + update display name

- **Implements:** REQ-ACCT-1, REQ-ACCT-2 AC1.
- **Slice contents:**
  - **Application:** `GetMyProfileQuery` + handler (returns `MyProfile { Email, DisplayName, PendingEmail? }`); `UpdateDisplayNameCommand` + handler + validator.
  - **Api:** `AccountController` with `GET /api/account/me`, `PUT /api/account/display-name`. `[Authorize(Roles="User")]` at the controller level.
  - **Acceptance tests:** `Get_me_returns_authenticated_users_profile`, `Update_display_name_persists_and_returns_new_profile`, `Update_display_name_rejects_blank_or_too_long`.
- **Specific guidance rules:** General §"Per-user isolation" (every account handler reads/writes the caller only).

### B-007 — Account: email-change request + confirm + cancel

- **Implements:** REQ-ACCT-2 AC2/AC3.
- **Depends on:** B-004 (`IEmailService`).
- **Slice contents:**
  - **Domain:** extend `User` with `PendingEmail`, `PendingEmailTokenHash`, `PendingEmailExpiresAt` (all nullable).
  - **Migration #006 `AddPendingEmailFields`.**
  - **Application:** `RequestEmailChangeCommand` + handler + validator (writes pending fields, sends verification via `IEmailService`); `ConfirmEmailChangeCommand` + handler + validator (validates token hash + expiry, swaps `Email`, clears pending, updates `MyProfile`); `CancelEmailChangeCommand` + handler.
  - **Api:** `AccountController` adds `POST /email-change/request`, `POST /email-change/confirm`, `DELETE /email-change`.
  - **Acceptance tests:** `Request_email_change_persists_pending_and_keeps_login_email`, `Confirm_email_change_with_valid_token_swaps_login_email`, `Confirm_email_change_with_expired_token_returns_400`, `Cancel_email_change_clears_pending_state`.

### B-008 — Account: change password

- **Implements:** REQ-ACCT-3, REQ-AUTH-7, REQ-NFR-4.
- **Depends on:** B-002 (audit), B-003 (revoke other sessions on success).
- **Slice contents:**
  - **Application:** `ChangePasswordCommand` + handler + validator (verify current password; reject if wrong with audit; update hash; revoke all `RefreshToken`s for the user **except** the caller's; write `PasswordChanged` audit).
  - **Api:** `AccountController.ChangePassword` (`PUT /api/account/password`).
  - **Acceptance tests:** `Change_password_with_correct_current_persists_new_hash`, `Change_password_with_wrong_current_returns_400_and_audits`, `Change_password_revokes_other_sessions_only`.

### B-009 — Account: delete account

- **Implements:** REQ-ACCT-4, REQ-NFR-4.
- **Slice contents:**
  - **Application:** `DeleteMyAccountCommand` + handler (cascade delete: `Todos`, `TodoActivityEntries`, `RefreshTokens`, `UserRoles`, `PasswordResetTokens`; write `AccountDeleted` audit BEFORE the row is removed).
  - **Infrastructure:** confirm `AppDbContext` cascades are correct for every related table.
  - **Api:** `AccountController.DeleteMyAccount` (`DELETE /api/account`).
  - **Acceptance tests:** `Delete_account_removes_user_and_all_owned_rows`, `Delete_account_invalidates_existing_access_jwt`.

### B-010 — Todo: extend create with notes + due date + activity strip

- **Implements:** REQ-TODO-1, REQ-TODO-2 (full), REQ-TODO-8 partly (`Created` activity).
- **Slice contents:**
  - **Domain:** extend `Todo` with `Notes`, `DueDate`. `TodoActivityEntry`, `TodoActivityKind` enum.
  - **Migration #007 `AddTodoNotesAndDueDate`** + **Migration #008 `AddTodoActivityEntries`** (cascade-delete by `TodoId`).
  - **Application:** extend `CreateTodoCommand` to accept `Notes`, `DueDate`. Extend `CreateTodoCommandValidator` (`Notes` ≤ 2000; `DueDate` nullable; if provided, ≥ today). Handler writes a `TodoActivityEntry { Created, OccurredAt = now }` after inserting the todo.
  - **Api:** extend `CreateTodoRequest` DTO.
  - **Acceptance tests:** `Create_todo_with_notes_and_due_date_persists_and_writes_Created_activity`, `Create_todo_rejects_notes_over_limit_and_past_due_date`.

### B-011 — Todo: get by id (with activity)

- **Implements:** REQ-TODO-4 (read leg), REQ-TODO-8 (read leg), REQ-NFR-5.
- **Depends on:** B-010 (activity table exists).
- **Slice contents:**
  - **Application:** `GetTodoByIdQuery` + handler returning `TodoDetail { Id, Title, Notes, DueDate, Status, CreatedAt, CompletedAt, Activity[] }`. Returns `NotFoundException` (mapped to 404) if not owned OR not found — same code path, no enumeration.
  - **Api:** `TodosController.GetById` (`GET /api/todos/{id:guid}`).
  - **Acceptance tests:** `Get_todo_by_id_returns_full_detail_including_activity`, `Get_todo_owned_by_other_user_returns_404`.

### B-012 — Todo: update

- **Implements:** REQ-TODO-4 (write leg).
- **Depends on:** B-010.
- **Slice contents:**
  - **Application:** `UpdateTodoCommand` + handler + validator (same shape as `CreateTodoCommandValidator`). Updates `Title`, `Notes`, `DueDate` only — never `Status` (that goes through B-013).
  - **Api:** `TodosController.Update` (`PUT /api/todos/{id:guid}`).
  - **Acceptance tests:** `Update_todo_persists_new_fields`, `Update_todo_does_not_change_status`, `Update_other_users_todo_returns_404`.

### B-013 — Todo: toggle status (with activity strip)

- **Implements:** REQ-TODO-1, REQ-TODO-5, REQ-TODO-8.
- **Depends on:** B-010.
- **Slice contents:**
  - **Application:** `ToggleTodoStatusCommand` + handler + validator (status ∈ `{Incomplete, Complete}`). On `Complete`: set `CompletedAt`; insert `TodoActivityEntry { MarkedComplete, OccurredAt = now }`. On `Incomplete`: clear `CompletedAt`; remove the latest `MarkedComplete` activity row for this todo.
  - **Api:** `TodosController.ToggleStatus` (`PATCH /api/todos/{id:guid}/status`).
  - **Acceptance tests:** `Toggle_to_complete_sets_CompletedAt_and_writes_activity`, `Toggle_back_to_incomplete_clears_CompletedAt_and_removes_activity`, `Toggle_with_invalid_status_returns_400`.

### B-014 — Todo: delete

- **Implements:** REQ-TODO-6.
- **Depends on:** B-010 (cascade includes activity rows).
- **Slice contents:**
  - **Application:** `DeleteTodoCommand` + handler.
  - **Api:** `TodosController.Delete` (`DELETE /api/todos/{id:guid}`).
  - **Acceptance tests:** `Delete_todo_removes_row_and_cascades_activity`, `Delete_other_users_todo_returns_404`.

### B-015 — Todo: list ordering rule

- **Implements:** REQ-TODO-3 AC4.
- **Depends on:** B-010.
- **Slice contents:**
  - **Application:** extend `GetTodosQueryHandler` ordering: `OrderBy(DueDate ascending nulls-last) ThenByDescending(CreatedAt)`. Same query under any future filter chip — backend returns the full owned set; frontend filters.
  - **Acceptance test:** `Get_todos_orders_by_due_date_ascending_nulls_last_then_created_at_descending`.

## Slice ordering / dependency graph

```
B-001 ──► B-005
   │
   └─► (nothing else depends on RBAC scaffolding — every other slice
        only needs the column-nullable change)

B-002 ──► B-004 ──► (every later slice that writes audit events)
   │
   └─► B-008

B-003 ──► B-004 (revoke-all on reset)
   └─► B-008 (revoke other sessions on password change)
   └─► B-005 (issue refresh on OIDC sign-in)

B-006 (independent)
B-007 ──► (depends on B-004's IEmailService)
B-009 (independent of other Account tasks)

B-010 ──► B-011, B-012, B-013, B-014, B-015 (Todo schema extensions land first)
```

Recommended landing order (BI1 will pull from this list): **B-001 → B-002 → B-003 → B-004 → B-005 → B-006 → B-007 → B-008 → B-009 → B-010 → B-015 → B-011 → B-012 → B-013 → B-014.**

## Plan-coverage cross-check

Every plan section in `docs/plans/backend.md` is touched by at least one task:

| Plan section / item                       | Covered by task(s)                                     |
|-------------------------------------------|--------------------------------------------------------|
| §2 entities — `Role` / `UserRole`         | B-001                                                  |
| §2 — `User.PasswordHash` nullable         | B-001 (migration #002)                                 |
| §2 — `User.PendingEmail*`                 | B-007                                                  |
| §2 — `Todo.Notes` / `DueDate`             | B-010                                                  |
| §2 — `TodoActivityEntry`                  | B-010 (write), B-011 / B-013 (read & write)            |
| §2 — `PasswordResetToken`                 | B-004                                                  |
| §2 — `SecurityAuditEvent`                 | B-002, B-004, B-008, B-009                             |
| §2 — `SignInAttempt`                      | B-002                                                  |
| §2 — `RefreshToken`                       | B-003                                                  |
| §3.1 Auth use cases                       | B-001, B-002, B-003, B-004, B-005                      |
| §3.2 Account use cases                    | B-006, B-007, B-008, B-009                             |
| §3.3 Todos use cases                      | B-010 / B-011 / B-012 / B-013 / B-014 / B-015          |
| §4 validator inventory                    | one per command across B-002, B-003, B-004, B-005, B-006, B-007, B-008, B-010, B-012, B-013 |
| §5 controller surface                     | controllers extended in each task                      |
| §6 auth flows                             | B-001 (RBAC), B-002 (lockout), B-003 (refresh + signout), B-004 (reset), B-005 (OIDC) |
| §6.5 `IRequestContext`                    | B-002                                                  |
| §7 migration sequencing 002–008           | B-001 (#002), B-002 (#003), B-003 (#004), B-004 (#005), B-007 (#006), B-010 (#007 + #008), B-005 (#009 — added in BT2 pass to pin the OIDC migration number) |
| §8 deferred no-op (`IEmailService`)       | B-004                                                  |

Every task is a true vertical slice (controller → command → handler → DB → test). No task crosses a layer boundary inconsistently with the guidance: every command lives in `Tickbox.Application`, every controller in `Tickbox.Api`, every entity in `Tickbox.Domain`, every EF / JWT / bcrypt / email / OIDC concrete in `Tickbox.Infrastructure`. No task introduces a repository, unit-of-work, generic CRUD service, or other forbidden abstraction.

## Acceptance gates

BT1 is done when this document is committed. BT2 evaluates: every slice is truly vertical, every task names its acceptance test, every task names which guidance rules apply, sizing is small enough that one slice = a few loop iterations, and no slice introduces a forbidden abstraction. BI1 then implements each task in order via ATDD against the Implementation Evaluation Rubric.
