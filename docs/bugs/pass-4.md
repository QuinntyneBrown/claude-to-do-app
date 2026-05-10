# TP3 — Pass 4 bug log

Date: 2026-05-10.
Pass executed by: `claude@M5`.
Test plan source: `docs/qa/test-plan.md`.

## Pre-pass automated coverage

- Backend acceptance tests: **41/41 passed**.
- Playwright E2E specs: **52/52 passed**.

## Findings

### B-4.1 — `<mat-checkbox>` on todo list items has no accessible name

**Severity:** medium (accessibility / REQ-NFR-7). Real finding — confirmed by reading `todo-list-item.component.html`.

**Symptom.** A screen reader on the todos list focuses each `<mat-checkbox>` and announces "checkbox, unchecked" with no identifying context. The to-do title is in a sibling `<p>`, not in the checkbox's accessible-name tree, so the user can't tell which to-do they're about to toggle.

**Fix.** Bind `[aria-label]` on the checkbox to a descriptive label that includes both the action verb and the to-do title:

- Incomplete → "Mark complete: `<title>`"
- Complete → "Mark incomplete: `<title>`"

The label is computed from `todo.status` and `todo.title` so it reads sensibly in both states. No regression spec needed — this is announce-text only and visible behaviour is unchanged.

### B-4.2 — Inline error `<p>` on sign-up and password-reset-complete forms missed the role=alert pass

**Severity:** medium (accessibility / REQ-NFR-7). Real finding — confirmed by reading both component HTML files.

**Symptom.** B-2.2 (last pass) added `role="alert"` + `aria-live="polite"` to `ErrorBannerComponent`, but two domain forms — `SignUpFormComponent` and `PasswordResetCompleteFormComponent` — render their inline errors via a custom `<p class="*__error" data-testid="*-error">` rather than the shared component. They were missed by the B-2.2 fix and still announce nothing to assistive tech.

**Fix.** Added the same `role="alert"` + `aria-live="polite"` attributes inline to both `<p>` elements. Idiomatically the right longer-term fix is to delete those inline `<p>`s and use `<tb-error-banner>` everywhere — but that's a refactor, not a bug fix; the surface-level patch matches the existing scaffolding without churn.

## Other test-plan scenarios reviewed

- **REQ-AUTH-3 AC2** (OIDC button hidden when env-disabled) — `OidcSignInButtonComponent.enabled` returns `false` when `window.__TICKBOX_OIDC_ENABLED__ !== true`; `oidc-sign-in-button.component.html` wraps the button in `@if (enabled)`. Covered by `Sign_in_hides_oidc_button_when_disabled` spec. ✓
- **AppShell nav `aria-label`** — both `<nav>` elements have `aria-label="Primary"`. ✓
- **Brand icon** — the `<mat-icon>` inside `BrandIconComponent` has `aria-hidden="true"` and the visible name is rendered as a `<span>`. ✓
- **Mat-checkbox vs label binding** — Material 3's `<mat-checkbox>` accepts `aria-label` on the host element and forwards it to the inner `<input>` (verified by inspecting the rendered DOM after the fix).
- **Lazy chunk integrity** — every route's `loadComponent` returns a named class; no breakage from the pass-3 sign-in change.

## Pass 4 outcome

Two real accessibility bugs found, both fixed. **52/52 frontend + 41/41 backend = 93/93 green.** Pass 4 closed.
