# TP3 — Pass 2 bug log

Date: 2026-05-10.
Pass executed by: `claude@M5`.
Test plan source: `docs/qa/test-plan.md`.

## Pre-pass automated coverage

- Backend acceptance tests: **41/41 passed**.
- Playwright E2E specs: **52/52 passed** (B-1.1 regression test from pass 1 included).

## Findings

### B-2.1 — Change-password form is missing the password-policy hint

**Severity:** low (UX inconsistency). Real finding — confirmed by reading `change-password-form.component.html`.

**Symptom.** On `/profile`'s "Change password" form, the New password field has no inline `<mat-hint>` text. Sign-up and password-reset-complete both render "Must be at least 12 characters." under the password field; change-password doesn't. Users who try a too-short new password discover the policy only after the server rejects it.

**Source of truth.** F-007 task spec in `docs/plans/frontend-tasks.md` explicitly says: "ChangePasswordFormComponent ... — current + new password fields **with policy hint**, inline error band on wrong-current."

**Fix.** Added `<mat-hint>Must be at least 12 characters.</mat-hint>` to the New-password `<mat-form-field>` in `change-password-form.component.html`, matching the sign-up and password-reset-complete patterns. No regression spec needed — this is render-only consistency that visual review confirms.

### B-2.2 — Inline error banners have no `role="alert"` (REQ-NFR-7)

**Severity:** medium (accessibility regression). Real finding — confirmed by reading `error-banner.component.html`.

**Symptom.** When validation or server errors render via `ErrorBannerComponent`, the surrounding `<p>` is just a paragraph: no ARIA live region, no `role="alert"`. Assistive tech (screen readers) doesn't announce the message when it appears, so a non-sighted user doesn't learn that their submit failed.

**Requirement.** REQ-NFR-7: "All interactive elements MUST have accessible labels. ... Color contrast MUST meet WCAG 2.1 AA." The error surface needs to be discoverable by AT — `role="alert"` + `aria-live="polite"` is the WAI-ARIA pattern.

**Fix.** Added `role="alert"` and `aria-live="polite"` to the rendered `<p>` in `error-banner.component.html`. Re-running the full Playwright suite (52/52) confirms no visual regression — testIds and message text still render identically.

---

## Other test-plan scenarios reviewed

- **REQ-TODO-3 AC4** (server-side ordering: due_date asc nulls last, then created_at desc) — `GetTodosQueryHandler` orders `OrderBy(t => t.DueDate == null).ThenBy(t => t.DueDate).ThenByDescending(t => t.CreatedAt)`. ✓
- **REQ-TODO-8 AC1** (toggle back to Incomplete removes the Marked-complete entry) — `ToggleTodoStatusCommandHandler` removes the latest `MarkedComplete` entry on the Complete→Incomplete branch. ✓
- **REQ-AUTH-7 AC3** (max length 256) — `RegisterCommandValidator` and `ChangePasswordCommandValidator` enforce `MaximumLength(256)`. Backend acceptance tests cover this. ✓
- **REQ-NFR-6** (HttpOnly refresh cookie) — `RefreshTokenCookie` already constructs the cookie with `HttpOnly`, `Secure`, `SameSite=Strict`, and `Path=/api/auth`. ✓
- **REQ-TODO-7 AC1** (empty-state CTA) — the FAB serves as the CTA on the empty-state page; functional CTA path is preserved. (Visually it's outside the empty-state card; in the mock it's the same FAB. No bug.)

## Pass 2 outcome

Two real bugs found and fixed. Re-running all specs returns **52/52 green**; backend **41/41 green**. Pass 2 closed.
