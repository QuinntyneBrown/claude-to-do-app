# Tickbox frontend — implementation plan (FP1)

Author: `claude@M5` (FP1)
Status: draft, awaiting FP2 evaluation
Inputs: `docs/requirements.md` (approved), `docs/plans/backend-tasks.md` (approved + delivered), accepted mocks under `mocks/`, MF1 MVP under `frontend/`, `dotnet-angular-authenticated-full-stack-workflow.html` Implementation Guidance.

This plan is the authoritative source for what frontend tasks FT1 will slice. Every plan item maps to one or more guidance bullets and to one or more `REQ-*` requirements; every frontend-relevant requirement appears here. Every accepted mock screen has at least one route + component combination.

---

## 1. Workspace layout

Already established by MF1; **no new projects** are needed. The library boundaries and dep direction are immutable for FI1:

```
frontend/
  angular.json
  package.json
  playwright.config.ts
  projects/
    api/                 ← models + service contracts (no UI; HttpClient implementations)
    components/          ← reusable UI; depends on nothing
    domain/              ← UI components that depend on api contracts
    tickbox/             ← main app; depends on api + components + domain
  e2e/
    page-objects/        ← Playwright POMs
    specs/               ← Playwright specs
```

Layer dependency direction (enforced by tsconfig path aliases):

```
components ──▶  (nothing)
domain     ──▶  api
tickbox    ──▶  api, components, domain
```

A vertical slice MAY add files to any combination of these projects but MUST NOT cross the dependency direction. FT2 verifies this.

Maps to: Frontend §"Angular workspace split into libraries and apps", §"Library Structure" all bullets, General §"One type per file".

---

## 2. Per-feature route list

Routes live in `tickbox/src/app/app.routes.ts` (lazy `loadComponent` for everything beyond the public sign-in entry). Auth-protected routes go through the existing `authGuard`.

| Path                          | Component (where)                     | Public? | Mock screen                | Requirements                              |
|-------------------------------|---------------------------------------|---------|----------------------------|-------------------------------------------|
| `/sign-in`                    | `SignInPageComponent` (tickbox)       | ✓       | `sign-in.html`             | REQ-AUTH-2, REQ-AUTH-3, REQ-AUTH-4 (link) |
| `/sign-up`                    | `SignUpPageComponent` (tickbox)       | ✓       | `sign-up.html`             | REQ-AUTH-1, REQ-AUTH-7                    |
| `/password-reset/request`     | `RequestPasswordResetPageComponent`   | ✓       | `password-reset.html`      | REQ-AUTH-4 AC1                            |
| `/password-reset/complete`    | `CompletePasswordResetPageComponent`  | ✓       | (same mock, second step)   | REQ-AUTH-4 AC2/AC3                        |
| `/email-change/confirm`       | `ConfirmEmailChangePageComponent`     | ✓       | (banner-confirm landing)   | REQ-ACCT-2 AC3                            |
| `/oidc/callback`              | `OidcCallbackPageComponent`           | ✓       | (no visual mock — invisible)| REQ-AUTH-3 AC1                           |
| `/`                           | redirect → `/todos`                   |         |                            |                                           |
| `/todos`                      | `TodosListPageComponent` (tickbox)    | guarded | `todos.html`, `todos-empty.html` | REQ-TODO-3, REQ-TODO-3a, REQ-TODO-5, REQ-TODO-7 |
| `/todos/new`                  | `TodoDetailPageComponent` (create)    | guarded | `todo-detail.html`         | REQ-TODO-2                                |
| `/todos/:id`                  | `TodoDetailPageComponent` (edit)      | guarded | `todo-detail.html`         | REQ-TODO-4, REQ-TODO-5, REQ-TODO-6, REQ-TODO-8 |
| `/profile`                    | `ProfilePageComponent` (tickbox)      | guarded | `profile.html`             | REQ-ACCT-1, REQ-ACCT-2, REQ-ACCT-3, REQ-ACCT-4, REQ-AUTH-5 |
| `/error`                      | `ErrorPageComponent` (tickbox)        | guarded | `error.html`               | REQ-ERR-1                                 |
| `**`                          | redirect → `/todos`                   |         |                            |                                           |

Maps to: Frontend §"Mobile-first web app", §"main application — depends on api, components, and domain libraries".

---

## 3. Component inventory

One component per file with separate `.html`, `.scss`, `.ts`. Selectors prefixed `tb-`.

### 3.1 `components` library — reusable, no api/domain dependency

| Component               | Purpose                                                                                | Used by mocks                                            |
|-------------------------|----------------------------------------------------------------------------------------|----------------------------------------------------------|
| `BrandIconComponent`    | Logo + wordmark. **Already in MF1** — no change.                                       | sign-in, sign-up, password-reset, app-shell             |
| `AppShellComponent`     | Top-bar + nav-rail (≥600px) / bottom nav-bar (<600px) + content slot.                  | todos, todos-empty, todo-detail, profile, error         |
| `AppShellNavRailComponent` | Vertical nav rail (≥600px); collapsed icon-only on tablet, expanded with labels at ≥1240px. | todos, todos-empty, todo-detail, profile, error |
| `AppShellNavBarComponent` | Bottom nav-bar for mobile (<600px).                                                  | todos, todos-empty, todo-detail, profile                |
| `EmptyStateComponent`   | Icon + title + supporting text + slotted CTA.                                          | todos-empty, error, profile (delete confirm)            |
| `LoadingBarComponent`   | Indeterminate `mat-progress-bar`-shaped element with consistent token usage.           | every async-data screen                                  |
| `ErrorBannerComponent`  | Material 3 error-container shaped inline error block.                                  | every form / list with API errors                        |
| `ConfirmDialogComponent`| Dialog component for destructive confirms (delete account, delete todo).              | profile, todo-detail                                     |

The MVP's existing `BrandIconComponent` is the seed; everything else listed here is new.

### 3.2 `domain` library — UI bound to api contracts

| Component               | Purpose / api contract used                                                                | Mock fragment              |
|-------------------------|--------------------------------------------------------------------------------------------|----------------------------|
| `TodosListComponent`    | **Already in MF1** — extend to consume `ITodosService` (list, create, toggle status). Adds two-section layout (Incomplete / Complete), filter chips, FAB, empty-state slot. | todos.html (list region)   |
| `TodoListItemComponent` | One row in the list; renders title, due-date label ("Due today"/"Tomorrow"/`d MMM`), checkbox toggle. | todos.html                 |
| `TodoFilterChipsComponent` | All / Incomplete / Complete chips; emits the chosen filter; UI-only (REQ-TODO-3 AC2 — server returns the full set, frontend filters). | todos.html             |
| `TodoEditFormComponent` | Title, notes, due-date, status chip-set; consumes `ITodosService` for create / update / toggle / delete. | todo-detail.html         |
| `TodoActivityListComponent` | Read-only activity strip rendered from `TodoDetail.Activity[]`.                        | todo-detail.html (sidebar) |
| `SignInFormComponent`   | Already-MVP pattern; consumes `IAuthService.signIn`. Form-only; routing handled by parent page. | sign-in.html              |
| `SignUpFormComponent`   | Consumes `IAuthService.register`.                                                          | sign-up.html               |
| `PasswordResetRequestFormComponent` | Consumes `IAuthService.requestPasswordReset`.                                  | password-reset.html        |
| `PasswordResetCompleteFormComponent` | Consumes `IAuthService.completePasswordReset`.                                | (second-step mock variant) |
| `OidcSignInButtonComponent` | "Sign in with SSO" button; calls `IAuthService.beginOidcSignIn`; hidden when env disables OIDC (REQ-AUTH-3 AC2). | sign-in.html      |
| `ProfileSummaryComponent` | Avatar + display name + email + pending-email banner.                                    | profile.html (top section) |
| `EmailChangeBannerComponent` | "Pending email change to <new-email> — check your inbox" + Cancel button.              | profile.html (REQ-ACCT-2 AC2 banner) |
| `DisplayNameEditComponent` | Inline edit field + save button; consumes `IAccountService.updateDisplayName`.          | profile.html               |
| `ChangePasswordFormComponent` | current/new fields, calls `IAccountService.changePassword`.                          | profile.html (security section) |
| `DeleteAccountSectionComponent` | Danger-zone block; delegates to `ConfirmDialogComponent`; calls `IAccountService.deleteMyAccount`. | profile.html      |

### 3.3 `tickbox` app — page-level orchestration

Each page owns the `AppShellComponent` placement (or auth-screen layout) and composes domain components. None of these contain business logic — pages route, glue, and navigate.

| Page component                           | Composes                                                                                                              |
|------------------------------------------|------------------------------------------------------------------------------------------------------------------------|
| `SignInPageComponent`                    | `BrandIconComponent`, `SignInFormComponent`, `OidcSignInButtonComponent`                                              |
| `SignUpPageComponent`                    | `BrandIconComponent`, `SignUpFormComponent`                                                                           |
| `RequestPasswordResetPageComponent`      | `BrandIconComponent`, `PasswordResetRequestFormComponent`                                                             |
| `CompletePasswordResetPageComponent`     | `BrandIconComponent`, `PasswordResetCompleteFormComponent`                                                            |
| `OidcCallbackPageComponent`              | minimal — handles the OIDC redirect, calls `IAuthService.completeOidcSignIn`, navigates to `/todos`                   |
| `ConfirmEmailChangePageComponent`        | minimal — calls `IAccountService.confirmEmailChange(token from query)`, surfaces 200/400 outcome                      |
| `TodosListPageComponent`                 | `AppShellComponent` + `TodosListComponent` + `EmptyStateComponent` (when no todos)                                    |
| `TodoDetailPageComponent`                | `AppShellComponent` + `TodoEditFormComponent` + `TodoActivityListComponent`. Same component handles `new` and `:id`.  |
| `ProfilePageComponent`                   | `AppShellComponent` + `ProfileSummaryComponent` + `EmailChangeBannerComponent` + `DisplayNameEditComponent` + `ChangePasswordFormComponent` + `DeleteAccountSectionComponent` |
| `ErrorPageComponent`                     | `AppShellComponent` + `EmptyStateComponent` (error-icon variant)                                                     |

Maps to: Frontend §"Components library — reusable UI components ... does not depend on api library", §"Domain library — UI components that depend on api library", §"Main application — depends on api, components, and domain libraries", §"One type per file".

---

## 4. Service inventory in `api`

Every service has a `*.service.contract.ts` exporting an `I<X>Service` interface and an `<X>_SERVICE` `InjectionToken<I<X>Service>`. Components and pages consume the token (`@Inject(<X>_SERVICE)`); concrete classes are wired in `tickbox/src/app/app.config.ts` only.

| Service              | Contract file                          | Concrete file               | Methods                                                                                                                                      | Backend endpoints                              | Requirements                                  |
|----------------------|----------------------------------------|-----------------------------|----------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------|-----------------------------------------------|
| `IAuthService`       | `auth.service.contract.ts` (extend)    | `auth.service.ts` (extend)  | `register`, `signIn`, `refreshAccessToken`, `signOut`, `requestPasswordReset`, `completePasswordReset`, `beginOidcSignIn`, `completeOidcSignIn` | `/api/auth/*`                                  | REQ-AUTH-1..7                                 |
| `ITodosService`      | `todos.service.contract.ts` (extend)   | `todos.service.ts` (extend) | `list`, `getById`, `create`, `update`, `toggleStatus`, `delete`                                                                              | `/api/todos`, `/api/todos/{id}`, `/api/todos/{id}/status` | REQ-TODO-1..8                              |
| `IAccountService`    | `account.service.contract.ts` (new)    | `account.service.ts` (new)  | `getMyProfile`, `updateDisplayName`, `requestEmailChange`, `confirmEmailChange`, `cancelEmailChange`, `changePassword`, `deleteMyAccount`     | `/api/account/*`                               | REQ-ACCT-1..4                                 |

Models (also in `api`):

- existing: `Todo`, `TodoStatus`, `ApiConfig`, `SignInRequest`, `SignInResponse`
- new: `MyProfile`, `TodoDetail`, `TodoActivity`, `TodoActivityKind`, `CreateTodoRequest`, `UpdateTodoRequest`, `ChangePasswordRequest`, `EmailChangeRequest`, `OidcBeginResult`, `OidcCallbackRequest`, `RegisterRequest`

Maps to: Frontend §"api library — models and services needed to communicate with the backend", §"Exposes an interface for each service", §"Libraries use interface-driven service consumption".

---

## 5. Design tokens

Tokens are the Angular Material 3 system tokens emitted by `mat.theme(...)` in `tickbox/src/styles.scss` (already in MF1). All component SCSS files reference them via `var(--mat-sys-*)` — never per-component palette overrides, and never hard-coded colour values.

The token list components consume:

- **Color roles:** `--mat-sys-primary`, `--mat-sys-on-primary`, `--mat-sys-primary-container`, `--mat-sys-on-primary-container`, `--mat-sys-secondary-container`, `--mat-sys-on-secondary-container`, `--mat-sys-tertiary-container`, `--mat-sys-on-tertiary-container`, `--mat-sys-error`, `--mat-sys-on-error`, `--mat-sys-error-container`, `--mat-sys-on-error-container`, `--mat-sys-surface`, `--mat-sys-on-surface`, `--mat-sys-surface-variant`, `--mat-sys-on-surface-variant`, `--mat-sys-surface-container-low`, `--mat-sys-surface-container`, `--mat-sys-surface-container-high`, `--mat-sys-outline`, `--mat-sys-outline-variant`, `--mat-sys-inverse-surface`, `--mat-sys-inverse-on-surface`, `--mat-sys-inverse-primary`.
- **Type-scale:** `--mat-sys-display-large`, `--mat-sys-display-medium`, `--mat-sys-display-small`, `--mat-sys-headline-large`, `--mat-sys-headline-medium`, `--mat-sys-headline-small`, `--mat-sys-title-large`, `--mat-sys-title-medium`, `--mat-sys-title-small`, `--mat-sys-body-large`, `--mat-sys-body-medium`, `--mat-sys-body-small`, `--mat-sys-label-large`, `--mat-sys-label-medium`, `--mat-sys-label-small`.
- **Shape:** `--mat-sys-corner-extra-small`, `--mat-sys-corner-small`, `--mat-sys-corner-medium`, `--mat-sys-corner-large`, `--mat-sys-corner-extra-large`, `--mat-sys-corner-full`.
- **Elevation:** `--mat-sys-level1` … `--mat-sys-level5`.

Spacing uses a 4dp grid via the existing `--space-*` tokens defined in `mocks/styles.css`; `tickbox/src/styles.scss` mirrors those (FT1 lifts the values into the live SCSS so the components don't redefine them).

Maps to: Frontend §"Design tokens for consistent colors and spacing across the app", §"Visual language is Material 3".

---

## 6. Auth integration (frontend side)

### 6.1 Local sign-in / sign-up / sign-out

- `IAuthService.register` and `signIn` POST to `/api/auth/register` / `/api/auth/sign-in`. Response body includes `userId` + `accessToken`. The HttpOnly `tickbox.refresh` cookie is set by the backend; the browser sends it automatically on cross-origin requests when `withCredentials = true` is configured on the HttpClient.
- The existing `AuthStateService` (MF1) holds the access token in `sessionStorage` (REQ-NFR-6 ✓). FT1 extends it to track the `MyProfile` snapshot and the role list.
- The existing `authInterceptor` attaches `Authorization: Bearer <token>` and turns `withCredentials = true` on for the `/api/auth/*` paths so the refresh cookie is included.
- A new `tokenRefreshInterceptor` watches for 401 responses; on the first 401 in a request lifetime, it attempts `POST /api/auth/refresh`, replays the original request with the new access token, and signs the user out on a hard 401.
- Sign-out calls `POST /api/auth/sign-out` and then `AuthStateService.signOut()`.

### 6.2 PKCE OIDC

`IAuthService.beginOidcSignIn()` calls `GET /api/auth/oidc/authorize`, gets `{ authorizationUrl, state }`, and `window.location.assign(authorizationUrl)`. The `OidcCallbackPageComponent` reads `code` + `state` from the query string and calls `IAuthService.completeOidcSignIn({ code, state })` → `POST /api/auth/oidc/callback`. On success, the same access-token-and-cookie state as local sign-in is established. The `OidcSignInButtonComponent` is hidden when `window.__TICKBOX_OIDC_ENABLED__ !== true` so REQ-AUTH-3 AC2 is honoured.

### 6.3 Password reset

`/password-reset/request` form posts via `IAuthService.requestPasswordReset({ email })` and renders an inline confirmation regardless of outcome. `/password-reset/complete` reads `token` from the query string and posts via `IAuthService.completePasswordReset({ token, newPassword })`; on success the response sets the access-JWT + refresh-cookie session and the page navigates to `/todos`.

### 6.4 Email-change inline banner

When `MyProfile.pendingEmail` is non-null, `EmailChangeBannerComponent` renders on the `ProfilePageComponent` with a Cancel button. There is no separate route for the request flow (the inline form is on the profile page). The dedicated `/email-change/confirm` route handles the click-through from the verification email.

### 6.5 Route guard + RBAC

- `authGuard` (MF1) stays as the gate to authenticated routes.
- A new `roleGuard(roles: string[])` checks the JWT's `role` claim (decoded once into `AuthStateService`); v1 only seeds the `User` role so this guard's only consumer is the wildcard `[Authorize(Roles="User")]` on the backend mirrored in the frontend by hiding admin-only UI (none in v1, but the guard exists to make adding admin pages later trivial).

Maps to: Authentication §"Two supported sign-in flows", §"Passwords stored only as salted hashes" (frontend never stores plaintext), §"JWTs validated on every request" (frontend honors 401), §"Full user management — registration, sign-in, sign-out, password reset, profile management, account deletion", §"RBAC implementation from database to frontend".

---

## 7. Playwright POM test inventory

Playwright Page Object Model under `e2e/`. Each PageObject mirrors one component+route pair and exposes the testIds the component declares. Specs run against a network-mocked backend (`page.route(...)`) so they stay fast and don't require a running .NET host.

### 7.1 Page objects (one type per file, in `e2e/page-objects/`)

`SignInPage`, `SignUpPage`, `PasswordResetRequestPage`, `PasswordResetCompletePage`, `OidcCallbackPage`, `TodosListPage`, `TodoDetailPage`, `ProfilePage`, `ErrorPage`, `AppShellNav`. The MVP already ships `SignInPage` and `TodosPage`; FT1 renames `TodosPage → TodosListPage` and lists the rest as new.

### 7.2 Specs (one feature per file, in `e2e/specs/`)

| Spec file                              | Coverage                                                                                              | Requirements                                  |
|----------------------------------------|-------------------------------------------------------------------------------------------------------|-----------------------------------------------|
| `sign-up.spec.ts`                      | sign up form happy path; password-policy error                                                        | REQ-AUTH-1, REQ-AUTH-7                        |
| `sign-in.spec.ts`                      | local sign-in success; wrong-password error; SSO button hidden when disabled                           | REQ-AUTH-2, REQ-AUTH-3 AC2                    |
| `oidc-sign-in.spec.ts`                 | begin redirects, callback navigates to `/todos`                                                       | REQ-AUTH-3 AC1                                |
| `password-reset.spec.ts`               | request returns 202 inline confirmation; complete with valid token signs in                           | REQ-AUTH-4 AC1/AC2                            |
| `sign-out.spec.ts`                     | sign-out clears state and redirects                                                                   | REQ-AUTH-5                                    |
| `todos-list.spec.ts`                   | empty-state; ordering; filter chips; "Today" header label                                             | REQ-TODO-3, REQ-TODO-3a, REQ-TODO-7           |
| `todo-create.spec.ts`                  | FAB → detail (create mode) → save → returns to list                                                   | REQ-TODO-2                                    |
| `todo-edit.spec.ts`                    | open detail; edit title + notes + due date; save                                                       | REQ-TODO-4                                    |
| `todo-toggle.spec.ts`                  | tick checkbox in list moves Incomplete↔Complete; activity strip on detail reflects                    | REQ-TODO-1, REQ-TODO-5, REQ-TODO-8            |
| `todo-delete.spec.ts`                  | confirm dialog; delete removes from list                                                              | REQ-TODO-6                                    |
| `profile-view.spec.ts`                 | profile renders email + display name; pending-email banner appears after request                       | REQ-ACCT-1, REQ-ACCT-2 AC2                    |
| `profile-update-display-name.spec.ts`  | inline edit; persists; errors on too-long                                                              | REQ-ACCT-2 AC1                                |
| `profile-email-change.spec.ts`         | request shows banner; confirm-route swaps email; cancel clears banner                                 | REQ-ACCT-2 AC3                                |
| `profile-change-password.spec.ts`      | wrong current → 400 inline; correct current persists                                                   | REQ-ACCT-3                                    |
| `profile-delete-account.spec.ts`       | confirm dialog; delete signs out and routes to `/sign-in`                                              | REQ-ACCT-4                                    |
| `error-state.spec.ts`                  | network failure surface with retry preserves form input                                               | REQ-ERR-1                                     |
| `responsive.spec.ts`                   | render at 360 / 768 / 1440; nav-bar↔nav-rail toggle; touch-target ≥ 48dp on mobile primary actions    | REQ-NFR-1, REQ-NFR-7                          |

The MF1 sample-flow spec is renamed and folded into `sign-in.spec.ts` + `todo-create.spec.ts` so its coverage is preserved without duplication.

Maps to: Testing §"E2E testing using Playwright Page Object Model for important functionality", General §"ATDD: write the Playwright POM acceptance test first".

---

## 8. Deferred (no-op) integrations

None. The frontend has no third-party SDK that needs deferring — `@angular/material`, `@angular/animations`, and Playwright are real, all on the wire from MF1.

The OIDC button hides itself when `window.__TICKBOX_OIDC_ENABLED__` is falsy, mirroring the backend's `Oidc:Enabled` flag. That's a **feature flag**, not a deferred integration — the SSO flow is fully built.

Maps to: General §"optional integrations explicitly deferred ... may be replaced by a clearly-named no-op service" — explicitly N/A for the frontend.

---

## 9. Requirements coverage matrix

Every frontend-relevant requirement from `docs/requirements.md` maps to at least one plan section above. Backend-only requirements (REQ-AUTH-6, REQ-NFR-3, REQ-NFR-4, REQ-NFR-5, REQ-NFR-8 backend-build leg) are correctly omitted.

| Requirement   | Plan section(s)                                                                                                               |
|---------------|-------------------------------------------------------------------------------------------------------------------------------|
| REQ-AUTH-1    | §2 `/sign-up` · §3.2 `SignUpFormComponent` · §3.3 `SignUpPageComponent` · §4 `IAuthService.register` · §7.2 `sign-up.spec.ts` |
| REQ-AUTH-2    | §2 `/sign-in` · §3.2 `SignInFormComponent` · §4 `IAuthService.signIn` · §6.1 · §7.2 `sign-in.spec.ts`                          |
| REQ-AUTH-3    | §2 `/oidc/callback` · §3.2 `OidcSignInButtonComponent` · §3.3 `OidcCallbackPageComponent` · §4 OIDC methods · §6.2 · §7.2 `oidc-sign-in.spec.ts`, `sign-in.spec.ts` (AC2) |
| REQ-AUTH-4    | §2 `/password-reset/{request,complete}` · §3.2 `PasswordResetRequestFormComponent` / `PasswordResetCompleteFormComponent` · §4 · §6.3 · §7.2 `password-reset.spec.ts` |
| REQ-AUTH-5    | §3.3 `ProfilePageComponent` · §4 `IAuthService.signOut` · §6.1 · §7.2 `sign-out.spec.ts`                                       |
| REQ-AUTH-7    | §3.2 `SignUpFormComponent`, `PasswordResetCompleteFormComponent`, `ChangePasswordFormComponent` (client-side 12–256 hint) · §7.2 `sign-up.spec.ts` |
| REQ-TODO-1    | §3.2 `TodoEditFormComponent` (status chip-set restricts to two values) · §4 `ITodosService.toggleStatus` · §7.2 `todo-toggle.spec.ts` |
| REQ-TODO-2    | §2 `/todos/new` · §3.2 `TodoEditFormComponent` (create mode) · §3.3 `TodoDetailPageComponent` · §4 `ITodosService.create` · §7.2 `todo-create.spec.ts` |
| REQ-TODO-3    | §2 `/todos` · §3.2 `TodosListComponent`, `TodoFilterChipsComponent` · §3.3 `TodosListPageComponent` · §4 `ITodosService.list` · §7.2 `todos-list.spec.ts` |
| REQ-TODO-3a   | §3.2 `TodosListComponent` page header (today's date label) · §7.2 `todos-list.spec.ts`                                        |
| REQ-TODO-4    | §2 `/todos/:id` · §3.2 `TodoEditFormComponent` (edit mode) · §4 `ITodosService.getById`, `update` · §7.2 `todo-edit.spec.ts`   |
| REQ-TODO-5    | §3.2 `TodoListItemComponent` (checkbox) + `TodoEditFormComponent` (chip-set) · §4 `ITodosService.toggleStatus` · §7.2 `todo-toggle.spec.ts` |
| REQ-TODO-6    | §3.2 `TodoEditFormComponent` (delete) + `ConfirmDialogComponent` · §4 `ITodosService.delete` · §7.2 `todo-delete.spec.ts`     |
| REQ-TODO-7    | §3.1 `EmptyStateComponent` · §3.3 `TodosListPageComponent` (renders empty-state when `list.length === 0`) · §7.2 `todos-list.spec.ts` |
| REQ-TODO-8    | §3.2 `TodoActivityListComponent` · §4 `ITodosService.getById` returns `Activity[]` · §7.2 `todo-toggle.spec.ts`                |
| REQ-ACCT-1    | §2 `/profile` · §3.2 `ProfileSummaryComponent` · §3.3 `ProfilePageComponent` · §4 `IAccountService.getMyProfile` · §7.2 `profile-view.spec.ts` |
| REQ-ACCT-2    | §3.2 `DisplayNameEditComponent`, `EmailChangeBannerComponent` · §4 `IAccountService.updateDisplayName`, `requestEmailChange`, `confirmEmailChange`, `cancelEmailChange` · §6.4 · §7.2 `profile-update-display-name.spec.ts`, `profile-email-change.spec.ts` |
| REQ-ACCT-3    | §3.2 `ChangePasswordFormComponent` · §4 `IAccountService.changePassword` · §7.2 `profile-change-password.spec.ts`              |
| REQ-ACCT-4    | §3.2 `DeleteAccountSectionComponent` + `ConfirmDialogComponent` · §4 `IAccountService.deleteMyAccount` · §7.2 `profile-delete-account.spec.ts` |
| REQ-ERR-1     | §2 `/error` · §3.1 `ErrorBannerComponent`, `EmptyStateComponent` (error-icon variant) · §3.3 `ErrorPageComponent` · §7.2 `error-state.spec.ts` |
| REQ-ERR-2     | §3.1 `ErrorBannerComponent` (renders `ValidationProblemDetails.errors` per field)                                            |
| REQ-NFR-1     | §3.1 `AppShellComponent`, `AppShellNavRailComponent`, `AppShellNavBarComponent` (mobile-first responsive at 600 / 1240 breakpoints) · §7.2 `responsive.spec.ts` |
| REQ-NFR-2     | §5 design tokens — every component consumes `var(--mat-sys-*)`; M3 components from `@angular/material`                       |
| REQ-NFR-6     | §6.1 `AuthStateService` keeps the access token in `sessionStorage`; refresh token only in HttpOnly cookie (server-set)       |
| REQ-NFR-7     | §3 every interactive control uses Angular Material defaults (≥48dp); §7.2 `responsive.spec.ts` asserts touch-target sizes; aria-labels on every icon-only button |

---

## 10. Plan-item → guidance-rule map

Sanity check: every plan item references at least one rule from the Frontend / Library Structure / Authentication (frontend side) / Testing / General sections of the Implementation Guidance.

- §1 layout → Frontend §"Angular workspace split into libraries and apps", Library Structure §"main application — depends on api, components, and domain libraries".
- §2 routes → Frontend §"Angular Material" (every page hosts `mat-*` components), §"Mobile-first web app".
- §3.1 components → Library Structure §"components library — reusable UI components ... does not depend on api library".
- §3.2 domain components → Library Structure §"domain library — UI components that depend on api library", §"Exposes an interface for each service".
- §3.3 pages → Library Structure §"main application — depends on api, components, and domain libraries".
- §4 services → Library Structure §"api library — models and services needed to communicate with the backend", §"Exposes an interface for each service" (`*.service.contract.ts` + InjectionToken).
- §5 tokens → Frontend §"Design tokens for consistent colors and spacing", §"Visual language is Material 3 (https://m3.material.io/)".
- §6 auth → Authentication §"Two supported sign-in flows", §"Full user management — registration, sign-in, sign-out, password reset, profile management, account deletion", §"RBAC implementation from database to frontend".
- §7 tests → Testing §"E2E testing using Playwright Page Object Model for important functionality", General §"ATDD".
- §8 deferred → General §"optional integrations explicitly deferred" — explicitly N/A for the frontend.

No plan item conflicts with the guidance:

- No utility-class CSS framework (every component uses BEM + `var(--mat-sys-*)`).
- No single-file components — every component is `.ts`/`.html`/`.scss` triple.
- No service consumed by class — every service consumed via its `InjectionToken`.
- No cross-library imports against the dep direction (components ↛ api/domain, api ↛ domain, domain ↛ tickbox).
- No data-annotations equivalent on TypeScript request shapes (they're plain interfaces; backend FluentValidation owns the rules).

---

## 11. Acceptance gates

FP1 is done when this document is committed. FP2 evaluates §9 (every requirement appears) and §10 (every plan item maps and nothing conflicts). FT1 then takes this plan and decomposes each plan item into a vertically-sliced UI task. FI1 implements each slice via Playwright POM ATDD against the Frontend / Library Structure / Authentication / Testing / General sections of the guidance.
