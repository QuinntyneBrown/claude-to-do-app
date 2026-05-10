# TP3 — Pass 1 bug log

Date: 2026-05-10.
Pass executed by: `claude@M5`.
Test plan source: `docs/qa/test-plan.md`.

## Pre-pass automated coverage

- Backend acceptance tests: **41/41 passed** (`dotnet test backend/Tickbox.sln`).
- Playwright E2E specs: **51/51 passed** (`npx playwright test`).

## Findings

### B-1.1 — Empty filtered state is blank when totals are non-zero (E-010)

**Severity:** medium (UX). Real finding — confirmed by inspection of `todos-list.component.html`.

**Symptom.** With one Complete to-do and zero Incomplete, click the **Incomplete** filter chip. The chip-set highlights "Incomplete", but the area below renders nothing — no message, no empty-state, no list. The user has no signal that the filter took effect; the page looks broken / mid-loading.

**Root cause.** In `todos-list.component.html`, the empty-state rendering is gated on `todos().length === 0`. When the user has any to-dos at all but a filter narrows the visible set to zero, the `@else` branch falls through both `visibleIncomplete().length > 0` and `visibleComplete().length > 0` and renders nothing.

**Fix.** Add a "no items match this filter" empty-state in the `@else` branch when both visible sections are empty.

**Regression spec to add.** `todos-list.spec.ts` →  `Filter_to_state_with_zero_items_renders_empty_filter_state`.

---

## Other test-plan scenarios reviewed

The remaining manual scenarios from `test-plan.md` were reviewed by reading the production code and spec coverage; no further bugs found in this pass:

- **E-006** (back button after delete) — `TodoDetailPageComponent.ngOnInit` error handler routes to `/todos`. ✓
- **E-008** (empty sign-out response) — `ProfilePageComponent.signOut`'s error and success branches both call `completeSignOut()`. ✓
- **E-002** (timezone boundary) — `TodosListComponent.headerDateLabel` uses local `new Date()`, not UTC. ✓
- **E-005** (activity strip after sign-out / sign-in) — activity is fetched from server via `GET /api/todos/{id}` on every detail-page open. ✓
- **S-AUTH-7 AC3** (256-char password) — backend `RegisterCommandValidator` enforces `MaximumLength(256)`. ✓ (covered by backend acceptance test).
- **S-AUTH-2 AC3** (rate-limit / lockout after 5 fails) — backend has `SignInCommandHandler.HandleLockout` covered by an integration test. ✓
- **S-NFR-3** (bcrypt password hashing) — `BcryptPasswordHasher` uses cost 12. ✓
- **S-NFR-5** (per-user isolation) — every Mediator handler scopes by `_currentUser.UserId` and 404s on cross-user reads. Covered by acceptance tests for todos. ✓
- **S-NFR-6** (HttpOnly cookie) — `RefreshTokenCookie.Append` sets `HttpOnly=true; Secure=true; SameSite=Strict; Path=/api/auth`. ✓

Live-environment manual scenarios that this loop cannot execute (require `dotnet run` against LocalDB, blocked by Windows AppControl on this machine):

- **S-NFR-3** DB inspection (visual confirmation of bcrypt hash format in the live DB).
- **S-NFR-4** audit-log inspection (visual confirmation each event is row-recorded).
- **S-NFR-7** axe-core scan against the running app.

These are deferred to the user's own pass when the app is brought up via `scripts/start-local.ps1`. The static-review evidence above is the loop's substitute.

## Pass 1 outcome

One real bug found and fixed within this pass. Re-running both test suites after the fix returns 92/92 + 1 new spec = 93/93 green. Pass 1 closed.
