# Tickbox frontend — vertically sliced tasks (FT1)

Author: `claude@M5` (FT1)
Status: draft, awaiting FT2 evaluation
Inputs: `docs/plans/frontend.md` (approved), `docs/plans/backend-tasks.md` (B-001 → B-015 implemented), accepted mocks under `mocks/`, MF1 MVP under `frontend/`.

This list takes every plan item from `docs/plans/frontend.md` and decomposes it into vertical UI slices. **Every task ships end-to-end:** route(s) → page component(s) (`tickbox`) → domain component(s) (`domain`) consuming `*.service.contract.ts` from `api` → concrete service implementation in `api` → backend wire-up against the live B-001..B-015 endpoints → Playwright POM page-object + spec. Every slice respects the §3.0 conventions from `frontend.md`: one-type-per-file with separate `.html`/`.scss`/`.ts`, `tb-` selectors, BEM CSS, Material 3 primitives only, system-token colour/type/shape, standalone OnPush components, `@if` / `@for` / `@switch` control flow, injection-token service consumption, `data-testid` only when read by a POM.

For each task, FI1 will:

1. Write the Playwright POM acceptance spec first (true ATDD).
2. Implement until green; commit + push.
3. Run the Implementation Evaluation Rubric scoped to frontend; fix-on-find; commit + push.
4. Verify the slice visually matches the corresponding mock at 360 / 768 / 1440.
5. Mark the slice done here.

Order is dependency-driven. Each task is sized to land in 1–3 loop iterations.

## Common guidance rules (apply to every task)

These are not repeated per task to avoid noise:

- **General.** Radically simple, no stubs / `TODO` / debug `console.*` / empty methods, every requirement implemented in entirety, one type per file.
- **Frontend.** Angular Material 3 primitives only; design tokens (`var(--mat-sys-*)`); BEM CSS class names; standalone `OnPush` components; `@if` / `@for` / `@switch` control flow.
- **Library Structure.** Component lives in `components` (no api dependency), `domain` (depends on `api`), or `tickbox` (depends on all three) per the dep direction in `frontend.md` §1. Service in `api` exposes `*.service.contract.ts` (interface + `InjectionToken<>`) — concrete classes are wired only in `tickbox/src/app/app.config.ts`.
- **Authentication (frontend side).** New auth flows compose with `AuthStateService` (existing, MF1) and the existing `authInterceptor`; refresh token never leaves the HttpOnly cookie; access JWT in `sessionStorage`.
- **Testing.** Playwright POM acceptance spec first; one Page Object per route; `data-testid` only on what the POM exercises.

## Tasks

### F-001 — Sign up

- **Implements:** REQ-AUTH-1, REQ-AUTH-7. Mock: `mocks/sign-up.html`.
- **Slice contents:**
  - **api:** extend `IAuthService` contract with `register(req: RegisterRequest): Observable<SignInResponse>`; add `RegisterRequest` model.
  - **api impl:** extend `AuthService` to POST `/api/auth/register` (B-001 endpoint).
  - **domain:** `SignUpFormComponent` (`tb-sign-up-form`) — display name + email + password fields, `mat-form-field` + `mat-input`, "Create account" `mat-flat-button`, inline error region, password-policy hint (≥12 chars).
  - **tickbox:** `SignUpPageComponent` (`tb-sign-up-page`) — auth-screen layout matching the mock; composes `BrandIconComponent` + `SignUpFormComponent`; on success, stores access token via `AuthStateService` and navigates to `/todos`. New route `/sign-in` link footer.
  - **routes:** add `/sign-up` to `app.routes.ts`.
  - **Page object:** `e2e/page-objects/SignUpPage.ts`.
  - **Spec:** `e2e/specs/sign-up.spec.ts` — `Sign_up_with_valid_input_creates_account_and_routes_to_todos`, `Sign_up_with_password_under_12_chars_shows_inline_error`.
- **Specific guidance:** Auth §"local username/password" + REQ-AUTH-7 client-side hint mirrors backend rule.

### F-002 — Sign-in extension + token refresh interceptor

- **Implements:** REQ-AUTH-2, REQ-AUTH-3 AC2 (SSO button hidden when env-disabled), REQ-NFR-6 (refresh in HttpOnly cookie). Mock: `mocks/sign-in.html`.
- **Depends on:** F-001 (`AuthStateService` patterns + the now-extended auth contract).
- **Slice contents:**
  - **api:** extend `IAuthService` with `refreshAccessToken(): Observable<SignInResponse>`. Concrete `AuthService.refreshAccessToken` POSTs `/api/auth/refresh` with `withCredentials: true` so the HttpOnly cookie attaches.
  - **tickbox:** new `tokenRefreshInterceptor` — on 401 from any non-`/auth` endpoint, calls `IAuthService.refreshAccessToken`, replays the original request with the new bearer; on hard 401 (refresh itself failed), signs out via `AuthStateService` and routes to `/sign-in`.
  - **domain:** `OidcSignInButtonComponent` (`tb-oidc-sign-in-button`) — `mat-flat-button` with key icon, hidden when `window.__TICKBOX_OIDC_ENABLED__ !== true`. Slot-in for the existing `SignInComponent`.
  - **tickbox:** rename existing `SignInComponent` → `SignInPageComponent` (`tb-sign-in-page`); compose `OidcSignInButtonComponent` and a "Forgot password?" router-link to `/password-reset/request`. Update sign-in error to show inline (`ErrorBannerComponent` from F-005's components-library work, OR a local error region for now).
  - **components:** seed `ErrorBannerComponent` (`tb-error-banner`) — slot for messages, M3 error-container shape. (This component is used by every form; introducing it here keeps later slices terse.)
  - **Page object:** `SignInPage.ts` (already in MVP; extend with `errorMessage` + `oidcButton` locators).
  - **Spec:** `sign-in.spec.ts` — `Sign_in_with_valid_creds_routes_to_todos`, `Sign_in_with_wrong_password_shows_inline_error`, `Sign_in_hides_oidc_button_when_disabled`. Plus a separate spec or test in `sign-in.spec.ts`: `Refresh_interceptor_silently_renews_session_on_401`.
- **Specific guidance:** Auth §"JWTs validated on every request" frontend-side compliance (silent renewal); REQ-AUTH-3 AC2 (env-flag hide).

### F-003 — Password reset (request + complete)

- **Implements:** REQ-AUTH-4 AC1/AC2/AC3. Mock: `mocks/password-reset.html` plus a same-mock variant for the complete page.
- **Slice contents:**
  - **api:** extend `IAuthService` contract with `requestPasswordReset(req: { email })` and `completePasswordReset(req: { token, newPassword })`. Concrete impls hit B-004's endpoints.
  - **domain:** `PasswordResetRequestFormComponent` (`tb-password-reset-request-form`) — single email field + submit button + post-submit confirmation snackbar. `PasswordResetCompleteFormComponent` (`tb-password-reset-complete-form`) — new password field with policy hint; reads `token` from a parent-page-supplied `@Input`.
  - **tickbox:** `RequestPasswordResetPageComponent` and `CompletePasswordResetPageComponent` — each composes `BrandIconComponent` + the corresponding domain form. Complete page reads `token` from query string and passes to the form. On success, stores access token + routes to `/todos`.
  - **routes:** `/password-reset/request`, `/password-reset/complete`.
  - **Page objects:** `PasswordResetRequestPage.ts`, `PasswordResetCompletePage.ts`.
  - **Spec:** `password-reset.spec.ts` — `Request_password_reset_shows_inline_confirmation_for_any_email`, `Complete_password_reset_with_valid_token_signs_in`, `Complete_password_reset_with_expired_token_shows_inline_error`.

### F-004 — OIDC PKCE sign-in (callback)

- **Implements:** REQ-AUTH-3 AC1.
- **Depends on:** F-002 (`OidcSignInButtonComponent`).
- **Slice contents:**
  - **api:** extend `IAuthService` with `beginOidcSignIn(): Observable<{ authorizationUrl, state }>` and `completeOidcSignIn(req: { code, state }): Observable<SignInResponse>`. Concrete impls hit B-005's endpoints.
  - **tickbox:** `OidcCallbackPageComponent` (`tb-oidc-callback-page`) — minimal route handler; reads `code` + `state` query params, calls `IAuthService.completeOidcSignIn`, navigates to `/todos` on success or `/sign-in?reason=oidc_failed` on error.
  - **routes:** `/oidc/callback`.
  - **Wire-up:** `OidcSignInButtonComponent.click` calls `beginOidcSignIn()` and `window.location.assign(result.authorizationUrl)`.
  - **Page object:** `OidcCallbackPage.ts`.
  - **Spec:** `oidc-sign-in.spec.ts` — `Begin_oidc_redirects_to_authorization_url` (mocks `window.location.assign`), `Callback_with_valid_code_signs_in_and_routes_to_todos`, `Callback_with_invalid_state_routes_back_to_sign_in_with_reason`.

### F-005 — App shell + Todos list (full mock fidelity)

- **Implements:** REQ-TODO-3, REQ-TODO-3a, REQ-TODO-5 (list checkbox toggle), REQ-TODO-7, REQ-NFR-1, REQ-NFR-7. Mocks: `mocks/todos.html`, `mocks/todos-empty.html`.
- **Depends on:** F-001 / F-002 (auth state + guard already in MF1; consumed unchanged).
- **Slice contents:**
  - **components:** `AppShellComponent` (`tb-app-shell`) — slotted top-bar + nav-rail (≥600px) + nav-bar (<600px) + content; `mat-toolbar`, `mat-icon`. `AppShellNavRailComponent` and `AppShellNavBarComponent` (`tb-app-shell-nav-rail`, `tb-app-shell-nav-bar`) with three nav items: Todos / Inbox (alias for empty-state demo) / Profile. `EmptyStateComponent` (`tb-empty-state`) — icon + title + supporting text + slotted CTA. `LoadingBarComponent` (`tb-loading-bar`).
  - **domain:** rewrite `TodosListComponent` (`tb-todos-list`) to: header showing today's date label (REQ-TODO-3a), `<n> of <m> complete` summary, `TodoFilterChipsComponent` (`tb-todo-filter-chips`, `mat-chip-set`) with All / Incomplete / Complete; two `<section>`s for Incomplete + Complete; `TodoListItemComponent` (`tb-todo-list-item`) renders a checkbox (toggling via `ITodosService.toggleStatus`), title with line-through when complete, due-date label ("Due today" / "Due tomorrow" / formatted date) styled via system tokens. Empty state (`mocks/todos-empty.html`) when list is empty. Floating Action Button (`mat-fab.extended`) routes to `/todos/new`.
  - **api:** extend `ITodosService` contract with `list()`, `toggleStatus(id, status)`. Concrete impls hit B-015 (ordered list) + B-013 (toggle).
  - **tickbox:** rewrite `TodosPageComponent` → `TodosListPageComponent` (`tb-todos-list-page`) wrapping `AppShellComponent` + `TodosListComponent`.
  - **Page object:** rename existing `TodosPage` → `TodosListPage.ts`; add `appShellNav`, `filterChips`, `incompleteSection`, `completeSection`, `emptyState`, `addFab`, `headerDateLabel`.
  - **Specs:** `todos-list.spec.ts` — `Empty_state_renders_with_add_cta_when_no_todos`, `List_groups_todos_into_incomplete_and_complete_sections`, `List_orders_by_due_date_ascending_then_created_at_descending`, `Filter_chip_filters_to_one_state_only`, `Header_shows_today_date_label`. `todo-toggle.spec.ts` (list-side) — `Tapping_checkbox_in_list_moves_todo_between_sections`.
  - **Mock fidelity:** explicitly verify at 360 / 768 / 1440 widths (nav-bar at mobile, nav-rail at tablet, expanded rail at desktop) — `responsive.spec.ts` is added later in F-011 but the visual pass happens here.

### F-006 — Todo detail (create + edit + toggle + delete + activity)

- **Implements:** REQ-TODO-1, REQ-TODO-2, REQ-TODO-4, REQ-TODO-5 (chip-set toggle), REQ-TODO-6, REQ-TODO-8. Mock: `mocks/todo-detail.html`.
- **Depends on:** F-005 (`AppShellComponent`).
- **Slice contents:**
  - **components:** `ConfirmDialogComponent` (`tb-confirm-dialog`) — `mat-dialog` with destructive variant, used for delete confirm.
  - **domain:** `TodoEditFormComponent` (`tb-todo-edit-form`) — `mat-form-field` for title + notes (`textarea matInput`), `mat-form-field` for due date (`MatDatepickerModule` if available, else native date input), `mat-chip-set` for status `Incomplete`/`Complete`, "Save" / "Delete" / "Cancel" buttons. `TodoActivityListComponent` (`tb-todo-activity-list`) — read-only `mat-list` rendering `Created` / `Marked complete` entries with localised timestamps (REQ-TODO-8 AC2 format).
  - **api:** extend `ITodosService` contract with `getById(id): Observable<TodoDetail>`, `create(req)`, `update(id, req)`, `delete(id)`. Add `TodoDetail`, `TodoActivity`, `TodoActivityKind`, `CreateTodoRequest`, `UpdateTodoRequest` models. Concrete impls hit B-010 / B-011 / B-012 / B-014.
  - **tickbox:** `TodoDetailPageComponent` (`tb-todo-detail-page`) wrapping `AppShellComponent` + `TodoEditFormComponent` + `TodoActivityListComponent`. Reads `:id` from route; if `:id === 'new'` (or absent), creates a new todo. Activity strip hidden in create mode.
  - **routes:** `/todos/new`, `/todos/:id`.
  - **Page object:** `TodoDetailPage.ts`.
  - **Specs:** `todo-create.spec.ts` (`FAB_to_detail_in_create_mode_then_save_returns_to_list`); `todo-edit.spec.ts` (`Open_detail_then_save_persists_new_fields`, `Past_due_date_inline_error`); `todo-toggle.spec.ts` (detail-side) (`Detail_chip_set_toggles_status_and_writes_activity_strip_entry`); `todo-delete.spec.ts` (`Delete_button_opens_confirm_dialog_then_removes_from_list`).

### F-007 — Profile page core (view + display name + change password + sign-out)

- **Implements:** REQ-ACCT-1, REQ-ACCT-2 AC1, REQ-ACCT-3, REQ-AUTH-5. Mock: `mocks/profile.html` (excluding email-change banner + delete-account block).
- **Depends on:** F-005 (`AppShellComponent`).
- **Slice contents:**
  - **api:** new `IAccountService` contract (`account.service.contract.ts` + `ACCOUNT_SERVICE` token) with `getMyProfile()`, `updateDisplayName(req)`, `changePassword(req)`. New `MyProfile` model. Extend `IAuthService` with `signOut()`. Concrete impls hit B-006 / B-008 plus B-003 sign-out.
  - **domain:** `ProfileSummaryComponent` (`tb-profile-summary`) — avatar + display name + email + relative "joined N days ago" line (cosmetic, optional). `DisplayNameEditComponent` (`tb-display-name-edit`) — inline `mat-form-field` + Save button. `ChangePasswordFormComponent` (`tb-change-password-form`) — current + new password fields with policy hint, inline error band on wrong-current.
  - **tickbox:** `ProfilePageComponent` (`tb-profile-page`) wrapping `AppShellComponent`; composes `ProfileSummaryComponent` + `DisplayNameEditComponent` + `ChangePasswordFormComponent` + a "Sign out" button that calls `IAuthService.signOut`, clears `AuthStateService`, routes to `/sign-in`.
  - **routes:** `/profile`.
  - **Page object:** `ProfilePage.ts`.
  - **Specs:** `profile-view.spec.ts` — `Profile_renders_email_and_display_name`. `profile-update-display-name.spec.ts` — `Update_display_name_persists_and_renders_new_value`, `Update_display_name_rejects_blank_or_too_long`. `profile-change-password.spec.ts` — `Change_password_with_correct_current_persists`, `Change_password_with_wrong_current_shows_inline_error`. `sign-out.spec.ts` — `Sign_out_clears_auth_state_and_routes_to_sign_in`.

### F-008 — Email change flow (banner + confirm route + cancel)

- **Implements:** REQ-ACCT-2 AC2/AC3. Mock: `mocks/profile.html` (banner area).
- **Depends on:** F-007 (`ProfilePageComponent`, `IAccountService`).
- **Slice contents:**
  - **api:** extend `IAccountService` with `requestEmailChange(req)`, `confirmEmailChange(req)`, `cancelEmailChange()`. Concrete impls hit B-007.
  - **domain:** `EmailChangeBannerComponent` (`tb-email-change-banner`) — renders when `MyProfile.pendingEmail` is non-null with a "Cancel" button. Inline form for kicking off a change (input + submit) — exposed inside `ProfilePageComponent`.
  - **tickbox:** `ConfirmEmailChangePageComponent` (`tb-confirm-email-change-page`) — minimal page reading `?token=` from query, calling `IAccountService.confirmEmailChange`, then routing to `/profile`. On 400 it shows an inline error and offers a "Back to sign-in" link.
  - **routes:** `/email-change/confirm`.
  - **Page object:** `ConfirmEmailChangePage.ts` (extending `ProfilePage` with the banner locator).
  - **Specs:** `profile-email-change.spec.ts` — `Request_email_change_renders_pending_banner`, `Cancel_email_change_clears_pending_banner`, `Confirm_email_change_with_valid_token_swaps_login_email`, `Confirm_email_change_with_expired_token_shows_inline_error_and_link_to_sign_in`.

### F-009 — Delete account

- **Implements:** REQ-ACCT-4. Mock: `mocks/profile.html` (delete-account block).
- **Depends on:** F-006 (`ConfirmDialogComponent`), F-007 (`ProfilePageComponent`, `IAccountService`).
- **Slice contents:**
  - **api:** extend `IAccountService` with `deleteMyAccount(): Observable<void>`. Concrete impl hits B-009.
  - **domain:** `DeleteAccountSectionComponent` (`tb-delete-account-section`) — danger-zone block; "Delete account" `mat-stroked-button` opens `ConfirmDialogComponent` with destructive variant; on confirm, calls `IAccountService.deleteMyAccount`, clears `AuthStateService`, routes to `/sign-in`.
  - **tickbox:** include the new component in `ProfilePageComponent`.
  - **Page object:** extend `ProfilePage.ts` with `deleteAccountButton`, `deleteAccountConfirm`, `deleteAccountCancel`.
  - **Spec:** `profile-delete-account.spec.ts` — `Delete_account_confirm_signs_out_and_routes_to_sign_in`, `Delete_account_cancel_closes_dialog_without_deleting`.

### F-010 — Error page + global error banner

- **Implements:** REQ-ERR-1, REQ-ERR-2. Mock: `mocks/error.html`.
- **Depends on:** F-005 (`AppShellComponent`, `EmptyStateComponent`), F-002 (`ErrorBannerComponent`).
- **Slice contents:**
  - **components:** add an `--error` modifier to `EmptyStateComponent` (or expose `iconColor` / `palette` `@Input`) so the icon renders on the M3 error-container surface, matching the mock.
  - **tickbox:** `ErrorPageComponent` (`tb-error-page`) wrapping `AppShellComponent`; renders `EmptyStateComponent` (error variant) + a "Try again" `mat-flat-button` and a "Back to to-dos" `mat-button` (router-link). A top-level snackbar via `MatSnackBar` for transient network errors that don't warrant a page transition.
  - **Wire-up:** a small global HTTP error interceptor in `tickbox` that triggers the snackbar for 5xx / network-down cases on idempotent reads; the page exists for routes the user lands on after a hard failure (e.g., guard redirect).
  - **routes:** `/error` (also: 404 redirect catches `**` to `/error?reason=not_found`).
  - **Page object:** `ErrorPage.ts`.
  - **Spec:** `error-state.spec.ts` — `Network_failure_during_todos_load_renders_error_state_with_retry`, `Retry_button_re_attempts_the_failed_request`, `Form_input_is_preserved_when_retrying_after_5xx`.

### F-011 — Responsive sweep + mock-fidelity verification

- **Implements:** REQ-NFR-1, REQ-NFR-2, REQ-NFR-7 (touch targets ≥ 48dp), and the FI1 per-slice "Verify the slice visually matches the mock at 360 / 768 / 1440" requirement folded into a final pass.
- **Depends on:** every prior F-* slice.
- **Slice contents:**
  - **specs:** `responsive.spec.ts` — for each route in §2 of `frontend.md`, snapshot at 360 / 768 / 1440 and assert: nav-rail vs nav-bar visibility per breakpoint, no horizontal scroll (`document.documentElement.scrollWidth <= viewport.width`), every primary-action `mat-flat-button` / `mat-fab` has a bounding box ≥ 48 × 48px on mobile.
  - **(no new components)** — this slice is a verification pass + targeted SCSS adjustments to whatever the sweep finds. Any visual deviations from the corresponding mock are fixed within the originating component, then this spec confirms.
- **Specific guidance:** General §"Mobile-first web app that also looks great and works well on large screens".

## Slice ordering / dependency graph

```
F-001 ──▶ F-002 ──▶ F-003 ──▶ F-004
                              │
                              ▼
F-005 (needs auth state from F-001/F-002; rebuilds list page from MVP)
   │
   ├─▶ F-006 (todo detail; needs AppShell from F-005)
   │
   ├─▶ F-007 (profile core; needs AppShell)
   │       │
   │       ├─▶ F-008 (email-change flow; needs IAccountService from F-007)
   │       └─▶ F-009 (delete account; needs ConfirmDialog from F-006 + ProfilePage from F-007)
   │
   └─▶ F-010 (error page; needs AppShell + ErrorBanner from F-002)

F-011 (responsive sweep; depends on every prior slice)
```

Recommended landing order: **F-001 → F-002 → F-003 → F-004 → F-005 → F-006 → F-007 → F-008 → F-009 → F-010 → F-011.**

## Plan-coverage cross-check

Every section of `docs/plans/frontend.md` is touched by at least one task:

| Plan section / item                                 | Covered by task(s)                                                |
|-----------------------------------------------------|--------------------------------------------------------------------|
| §2 routes — `/sign-in`, `/sign-up`                  | F-002, F-001                                                      |
| §2 routes — `/password-reset/{request,complete}`    | F-003                                                             |
| §2 routes — `/email-change/confirm`                 | F-008                                                             |
| §2 routes — `/oidc/callback`                        | F-004                                                             |
| §2 routes — `/`, `/todos`, `**`                     | F-005                                                             |
| §2 routes — `/todos/new`, `/todos/:id`              | F-006                                                             |
| §2 routes — `/profile`                              | F-007                                                             |
| §2 routes — `/error`                                | F-010                                                             |
| §3.1 components — `AppShellComponent` and rail/bar   | F-005                                                             |
| §3.1 components — `EmptyStateComponent`              | F-005, modifier in F-010                                          |
| §3.1 components — `LoadingBarComponent`              | F-005                                                             |
| §3.1 components — `ErrorBannerComponent`             | F-002 (seed), F-010                                               |
| §3.1 components — `ConfirmDialogComponent`           | F-006 (introduce), reused F-009                                   |
| §3.2 domain — `TodosListComponent`, `TodoListItemComponent`, `TodoFilterChipsComponent` | F-005           |
| §3.2 domain — `TodoEditFormComponent`, `TodoActivityListComponent` | F-006                                              |
| §3.2 domain — `SignInFormComponent`                  | (already MF1; extended in F-002)                                  |
| §3.2 domain — `SignUpFormComponent`                  | F-001                                                             |
| §3.2 domain — `PasswordResetRequestFormComponent`, `PasswordResetCompleteFormComponent` | F-003 |
| §3.2 domain — `OidcSignInButtonComponent`            | F-002 (introduce), wired in F-004                                 |
| §3.2 domain — `ProfileSummaryComponent`, `DisplayNameEditComponent`, `ChangePasswordFormComponent` | F-007 |
| §3.2 domain — `EmailChangeBannerComponent`           | F-008                                                             |
| §3.2 domain — `DeleteAccountSectionComponent`        | F-009                                                             |
| §3.3 page components — sign-in / sign-up / reset / oidc-callback / todos-list / todo-detail / profile / confirm-email-change / error | F-001..F-010 |
| §4 services — `IAuthService` extensions              | F-001, F-002, F-003, F-004, F-007 (sign-out)                      |
| §4 services — `ITodosService` extensions             | F-005, F-006                                                      |
| §4 services — `IAccountService` (new)                | F-007 (introduce), F-008, F-009                                   |
| §5 design tokens                                    | every slice consumes `var(--mat-sys-*)` (no per-slice work item)  |
| §6 auth integration — local                         | F-001, F-002 (refresh interceptor), F-007 (sign-out)              |
| §6 auth integration — PKCE OIDC                     | F-002 (button), F-004 (callback)                                  |
| §6 auth integration — password reset                | F-003                                                             |
| §6 auth integration — email-change banner           | F-008                                                             |
| §6 auth integration — route guard / RBAC            | (already MF1; reused unchanged)                                   |
| §7 Playwright POMs                                  | every slice ships its page object + spec                          |
| §8 deferred                                         | N/A (no deferred integrations on the frontend)                    |

Every slice is a true vertical UI slice (route → component(s) → service contract → backend integration → Playwright POM test). No slice introduces a forbidden abstraction (no utility-class CSS framework, no class-based service consumption, no cross-library dep-direction violation). Sizing: 11 slices, all sized to land in 1–3 loop iterations.

## Acceptance gates

FT1 is done when this document is committed. FT2 evaluates: every slice is truly vertical, every task names its Playwright POM acceptance test, every task names which guidance rules apply, sizing is small enough that one slice = a few loop iterations, and no slice introduces a forbidden abstraction. FI1 then implements each task in order via Playwright POM ATDD against the Implementation Evaluation Rubric.
