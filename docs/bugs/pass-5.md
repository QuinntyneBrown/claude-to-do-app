# TP3 — Pass 5 bug log (closing pass)

Date: 2026-05-10.
Pass executed by: `claude@M5`.
Test plan source: `docs/qa/test-plan.md`.

## Pre-pass automated coverage

- Backend acceptance tests: **41/41 passed**.
- Playwright E2E specs: **52/52 passed**.
- Backend `dotnet build -warnaserror`: **0 warnings, 0 errors** (REQ-NFR-8 verified).

## Findings

### B-5.1 — Decorative `<mat-icon>` on app-shell nav rail/bar lacks `aria-hidden`

**Severity:** **non-blocking** (accessibility polish). Real finding.

**Symptom.** Each nav item in `AppShellComponent` is a `<a>` with both a `<mat-icon>` and a `<span class="*-label">` text. Without `aria-hidden="true"` on the icon, some screen readers announce the Material-Symbols ligature name (e.g. "checklist") **and** the visible label "To-dos", duplicating the announcement on every navigation focus.

**Fix.** Added `aria-hidden="true"` to both nav-rail and nav-bar `<mat-icon>` instances in `app-shell.component.html`. Visible appearance is unchanged; tests pass 52/52.

## Other test-plan scenarios reviewed

- **REQ-NFR-8** build cleanliness — `dotnet build backend/Tickbox.sln -warnaserror` produces 0 warnings, 0 errors. Frontend `ng build api/components/domain/tickbox` already verified across the FI1 slices. ✓
- **MatDialog focus trap** — handled by `@angular/cdk/a11y`'s `FocusTrap` (used internally by MatDialog); confirm dialog from F-006 inherits this for free. ✓
- **REQ-NFR-6** client-side data hygiene — `AuthStateService` uses `sessionStorage["tickbox.access-token"]` only; refresh cookie is HttpOnly + Secure + SameSite=Strict + Path=/api/auth (set by `RefreshTokenCookie.Append`). ✓
- **REQ-NFR-5** per-user isolation — every Mediator handler scopes by `_currentUser.UserId`; cross-user GET / PATCH / DELETE return 404 (covered by backend acceptance suite). ✓
- **Activity strip behaviour** — `ToggleTodoStatusCommandHandler` removes the latest `MarkedComplete` entry on the Complete→Incomplete branch, satisfying REQ-TODO-8 AC1's "the existing 'Marked complete' entry is removed" clause. ✓

## Deferred polish (non-blocking, pass-5 sweep noted these but deliberately did not chase them)

- Decorative `<mat-icon>` instances inside button content (e.g. `delete_forever`, `key`, `check`, `delete`, `refresh`, `checklist`, `logout`, `add`) are paired with adjacent button text. Same duplicate-announcement risk as B-5.1. Not blocking — most screen readers de-duplicate when the icon ligature matches a single-word concept and the button has a clear visible label. A future a11y-polish slice can sweep these in one pass; doing it under pass 5 risks deviating from the closing-pass rule of "fix only blocking bugs."
- The two domain forms that use inline `<p data-testid="*-error">` (`SignUpFormComponent`, `PasswordResetCompleteFormComponent`) could be migrated to use the shared `ErrorBannerComponent` to centralise the role/aria pattern. Pass 4 fixed the immediate symptom; the refactor is a follow-up, not a bug.

## Pass 5 outcome — TP3 phase closed

One non-blocking polish fix found and applied (B-5.1). **No blocking bugs surfaced this pass.** TP3's done-when criterion ("pass 5 finishes with no new blocking bugs") is met.

Final test totals after the full TP3 run:
- Backend acceptance: **41/41**.
- Playwright E2E: **52/52**.
- Backend build: **0 warnings, 0 errors**.

Cumulative TP3 finding count across passes 1–5: **7 fixed bugs** + **1 non-blocking polish**, all addressed in-pass. The codebase is at its quietest state since FI1 closed.
