# BP2 — Evaluate backend plan

Evaluator: `claude@M5`. Reviewed `docs/plans/backend.md` against `docs/requirements.md` (approved), the accepted MB1 backend MVP under `backend/`, and the Backend / Validation / Authentication / General sections of the workflow's Implementation Guidance.

Two checks define BP2 done:

- **A.** every requirement from `docs/requirements.md` appears in the plan;
- **B.** every plan item maps to a guidance rule, and **no** plan item conflicts with the guidance.

Plus the explicit BP2 checks: Clean Architecture layering planned; CQS via MediatR planned; no repository or unit-of-work in the plan; FluentValidation per command planned; `IAppDbContext` abstraction planned; auth flows planned (both OIDC PKCE and local) with hashing + JWT validation + RBAC; SQL Server; one-type-per-file convention assumed; deferred integrations explicitly enumerated as no-op logging services; no speculative abstractions.

## Pass 1 — findings

Five findings. Three blocking, two non-blocking notes.

- **F1 — Missing token-refresh endpoint.** §3.1 listed `SignOutCommand` but no `RefreshAccessTokenCommand`. With short-lifetime access JWTs (15 min) and HttpOnly-cookie refresh tokens (per REQ-NFR-6), the backend MUST expose a `POST /api/auth/refresh` endpoint to rotate the refresh token and re-issue a fresh access JWT. Without it, the user is forcibly signed out after every access-token expiry, contradicting REQ-AUTH-5's session model. **Blocking.**
- **F2 — `User.PendingEmailToken` stored in plaintext.** §2 listed `PendingEmailToken` rather than `PendingEmailTokenHash`. `PasswordResetToken.TokenHash` (already in §2) hashes its token; the email-change verification token MUST use the same pattern. Storing the verification token in plaintext means anyone with DB read access can complete an in-flight email change. **Blocking** (security parity).
- **F3 — `User.PasswordHash` schema does not accommodate OIDC-only accounts.** §2 implied `PasswordHash` is required (the MB1 schema has it as `NOT NULL`), but §6.2 step 5 provisions an OIDC-only `User` with no password hash. Either the OIDC provisioning has to fabricate a sentinel hash (bad — can be brute-forced if the DB leaks; also confuses the local sign-in flow into thinking the account has a password) or the column has to be nullable. The right call is nullable, with the local sign-in handler treating null as "no local password" → 401 generic. **Blocking.**
- **F4 — Audit/lockout depend on caller IP, but no `IRequestContext` abstraction was planned.** §2's `SecurityAuditEvent` has `IpAddress`, and §6.1's lockout window is per-(email, IP). Handlers in `Tickbox.Application` cannot reach `HttpContext` directly; they need an Application-layer abstraction in the same shape as the existing `ICurrentUserService`. Without this, every relevant slice will reinvent the wheel. **Non-blocking note** (any slice can introduce it on first need, but spelling it out in the plan keeps the slices uniform).
- **F5 — `SignInAttempt` row growth unbounded.** §6.1's lockout reads attempts from the last 15 minutes only, but rows are never deleted. For v1 the perf cost is negligible; long-term a retention policy is required. **Non-blocking note** — record so a future ops slice picks it up; do not pollute v1 with a cleanup background job.

### Fixes applied between Pass 1 and Pass 2

- **F1.** Added `RefreshAccessTokenCommand` to §3.1, `POST /api/auth/refresh` to §5 `AuthController`. The handler reads the cookie, verifies the hashed token, rotates it, and returns a `SignInUserResult` (access JWT + new refresh-token cookie).
- **F2.** Renamed `PendingEmailToken` → `PendingEmailTokenHash` in §2 with an explicit note that the plaintext token is sent in the email and never persisted.
- **F3.** Marked `PasswordHash` nullable in §2 with the rationale; updated §6.1 sign-in handler step 2 to treat a null hash the same as "user not found" — return the generic 401 + write `SignInFailed`. Updated migration #002 in §7 to make `Users.PasswordHash` nullable as part of the RBAC slice.
- **F4.** Added §6.5 specifying `IRequestContext` in `Tickbox.Application` (exposing `RemoteIp` and `UserAgent`), implemented in `Tickbox.Api.RequestContext` against `IHttpContextAccessor`. §10 explicitly justifies it as non-speculative (lockout + audit log already require it).
- **F5.** No plan change; recorded as a non-blocking note for a future slice.

## Pass 2 — findings

Re-ran the two BP2 checks plus the explicit checklist against the updated plan.

### A. Every backend-relevant requirement appears in the plan

Cross-checked `docs/requirements.md` § 3 (functional), § 4 (NFR), § 5 (constraints), § 6 (out-of-scope) against the post-fix coverage matrix in §9 of the plan.

- REQ-AUTH-1 → present. ✓
- REQ-AUTH-2 → present, including the lockout (5/15 min) and audit hook. ✓
- REQ-AUTH-3 → present (OIDC PKCE begin/callback flow, env-toggled). ✓
- REQ-AUTH-4 → present (request + complete + audit + IEmailService no-op). ✓
- REQ-AUTH-5 → present (sign-out **and** refresh, both manipulating `RefreshToken`). ✓
- REQ-AUTH-6 → present (already wired in MB1). ✓
- REQ-AUTH-7 → present (every password validator references the 12–256 length policy). ✓
- REQ-TODO-1 to REQ-TODO-8 → all present. ✓
- REQ-TODO-3a → correctly marked frontend-only. ✓
- REQ-TODO-7 → correctly marked frontend (backend returns `200 []`). ✓
- REQ-ACCT-1 to REQ-ACCT-4 → all present. ✓
- REQ-ERR-1 → frontend. ✓ (out of backend scope)
- REQ-ERR-2 → present (the MB1 middleware + every new validator). ✓
- REQ-NFR-3 → present (bcrypt work factor 12 already wired in MB1). ✓
- REQ-NFR-4 → present (`SecurityAuditEvent` table; every relevant handler writes one). ✓
- REQ-NFR-5 → present (every Todo and Account handler is per-user). ✓
- REQ-NFR-6 → present after F1 fix (refresh tokens HttpOnly Secure cookie, hashed in DB; access JWT in-memory client-side). ✓
- REQ-NFR-1, REQ-NFR-2, REQ-NFR-7 → frontend; correctly omitted from backend plan. ✓
- REQ-NFR-8 → backend's contribution is `dotnet build -warnaserror` 0/0 across new slices; explicitly stated in §9. ✓

A — pass.

### B. Every plan item maps to a guidance rule; no conflicts

Walked the explicit BP2 checklist:

- Clean Architecture layering planned. ✓ (§1, no new projects, layer dependency direction explicit.)
- CQS via MediatR planned. ✓ (§3 organises every use case as a command or query; the MB1 `ValidationBehavior` pipeline is reused; assembly-scanning registration in `AddApplication`.)
- No repository or unit-of-work in the plan. ✓ (handlers consume `IAppDbContext` directly; checked by inspection — no `IRepository`, `IUnitOfWork`, `IRepository<T>`, or "Repository" naming anywhere in §2 / §3.)
- FluentValidation per command planned. ✓ (§4 lists 12 validators, one per command. No queries with input require a validator. No data-annotations.)
- `IAppDbContext` abstraction planned. ✓ (§2 says handlers consume `IAppDbContext` directly; §6.5 introduces only the additional `IRequestContext` (justified) and the existing `ICurrentUserService` from MB1.)
- Auth flows planned (both OIDC PKCE and local) with hashing + JWT validation + RBAC. ✓ (§6.1 local with bcrypt, lockout, audit; §6.2 PKCE OIDC; §6.3 RBAC seeded with `User`; §6.4 JWT validation reused from MB1.)
- SQL Server. ✓ (§2, §7, no other provider mentioned.)
- One-type-per-file convention assumed. ✓ (§1, §2, §3, §4 all reiterate the one-type-per-file rule. No plan item bundles types.)
- Deferred integrations explicitly enumerated as no-op logging services. ✓ (§8: only `IEmailService` → `LoggingEmailService`. OIDC is **not** a no-op — env-toggled real impl, with the rationale stated.)
- No speculative abstractions in the plan. ✓ — verified by walking every interface introduced beyond MB1:
  - `IEmailService` — required by REQ-AUTH-4 and REQ-ACCT-2 (deferred no-op).
  - `IOidcClient` — required by REQ-AUTH-3.
  - `IRequestContext` — required by REQ-NFR-4 audit IP and REQ-AUTH-2 lockout.
  - No others. Specifically: no repository, no unit-of-work, no generic CRUD service, no "facade" services over MediatR, no event/messaging abstraction, no pre-emptive caching layer.

B — pass.

**Result:** zero blocking findings on Pass 2. Plan approved. BP2 done.
