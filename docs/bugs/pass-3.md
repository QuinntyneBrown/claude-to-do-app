# TP3 — Pass 3 bug log

Date: 2026-05-10.
Pass executed by: `claude@M5`.
Test plan source: `docs/qa/test-plan.md`.

## Pre-pass automated coverage

- Backend acceptance tests: **41/41 passed**.
- Playwright E2E specs: **52/52 passed** (cumulative: B-1.1 + pass-2 fixes).

## Findings

### B-3.1 — Empty-field submit silently does nothing on sign-in and password-reset-request

**Severity:** medium (UX). Real finding — confirmed by reading both forms.

**Symptom.** On `/sign-in` with both fields empty, click the "Sign in" button. The button is enabled, but nothing happens — no error, no spinner, no network call. The user can't tell whether their click was registered, whether the page is broken, or whether they need to fill something in.

Same pattern on `/password-reset/request`: empty email + click → silent.

**Root cause.** Both submit handlers had `if (this.submitting() || empty) { return; }` with no user-facing feedback. Compare `SignUpFormComponent` which renders an inline "Please fill in every field." error in the same situation, and `DisplayNameEditComponent` which renders "Display name cannot be blank.". The two affected forms diverged from the established pattern.

**Fix.** Restructured both submit handlers:
- Restored an `error` signal on `PasswordResetRequestFormComponent` (it had been removed during F-003) and added an inline `<p role="alert" aria-live="polite">` that renders when the field is blank.
- Updated `SignInComponent.submit` to set `error.set('Enter your email and password.')` when either field is blank, instead of silently returning. The existing `<tb-error-banner>` already in the template surfaces the message via `role="alert"` (added in B-2.2 last pass).

Re-running the full Playwright suite (52/52) confirms no regression: all sign-in and password-reset-request happy-path specs still pass because they always fill the fields before clicking submit. The change is invisible to specs that pass valid input; only the empty-input UX is affected.

### Other test-plan scenarios reviewed

- **Sign-up empty-field** — `SignUpFormComponent.submit` already has `this.error.set('Please fill in every field.')`. ✓
- **Change-password empty-field** — `ChangePasswordFormComponent.submit` already has `this.error.set('Enter your current and new password.')`. ✓
- **Display-name empty-field** — `DisplayNameEditComponent.save` has `this.error.set('Display name cannot be blank.')` (verified by F-007 spec `Update_display_name_rejects_blank_via_inline_error`). ✓
- **Email-change empty-field** — `EmailChangeFormComponent.submit` has `this.error.set('Enter a new email address.')`. ✓
- **Backend `RegisterCommandValidator`** — backend acceptance tests confirm 400 + `ValidationProblemDetails` on empty fields. ✓
- **REQ-AUTH-4 AC1** (no enumeration on reset request) — `PasswordResetRequestFormComponent.subscribe` calls `submitted.set(true)` from both the `next` and `error` branches, so the same confirmation copy renders regardless. ✓
- **Lazy-load chunks** — every route's `loadComponent` returns the page-component class via `.then(m => m.X)`. No accidental default imports. ✓

## Pass 3 outcome

One real bug found (split across two files with identical symptom), fixed. **52/52 frontend + 41/41 backend = 93/93 green.** Pass 3 closed.
