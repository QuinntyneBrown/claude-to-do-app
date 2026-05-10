# FP2 — Evaluate frontend plan

Evaluator: `claude@M5`. Reviewed `docs/plans/frontend.md` against `docs/requirements.md` (approved), the accepted mocks under `mocks/`, the MF1 frontend MVP under `frontend/`, and the Frontend / Library Structure / Authentication (frontend side) / Testing / General sections of the workflow's Implementation Guidance.

## FP2 explicit checks

- Three libraries (`api`, `components`, `domain`) plus app planned with correct dependency direction (components depends on nothing; domain depends on api; app depends on all three).
- Every planned service has a `*.service.contract.ts`.
- Angular Material 3 components specified.
- Design tokens specified.
- BEM naming assumed.
- Per-file split assumed (separate `.html` / `.scss` / `.ts`).
- Playwright POM tests planned for important flows.
- Both auth flows planned (PKCE OIDC + local).
- Mock-to-screen mapping is exhaustive.

## Pass 1 — findings

Three findings. One blocking, two non-blocking.

- **F1 — BEM naming and per-file split were implied but not explicitly committed in the component-inventory section.** §10 mentioned both; §3 jumped straight to component tables without stating the conventions. The MF1 MVP follows them, but the plan must say so explicitly so FT1 can hold every slice to the rule. **Blocking.**
- **F2 — REQ-NFR-8 frontend leg missing from the §9 coverage matrix.** The backend leg lives in `docs/plans/backend.md`; the frontend's contribution (every `ng build` 0/0 across the four projects) was not in §9. **Non-blocking** (the convention is real and the FI1 slices will honour it; the omission is paperwork). Fixed in pass 2 prep.
- **F3 — Per-component Material 3 primitive list not enumerated.** §10 mentions "components from `@angular/material`" but didn't enumerate which Material primitives each component would use. The mocks make the choices obvious (mat-button, mat-form-field, mat-input, mat-checkbox, mat-icon, mat-progress-bar, mat-toolbar, mat-list, mat-dialog, mat-snackbar, mat-chip-set, mat-divider, mat-menu, mat-tabs), but the plan didn't list them. **Non-blocking note** — folded into the new §3.0 conventions block.

### Fixes applied between Pass 1 and Pass 2

- **F1.** Added `### 3.0 Conventions every component must follow` to §3, listing nine bullets that apply to every component table below it: one-type-per-file, `tb-` selector prefix, BEM naming with no utility-class frameworks, Material 3 primitives only, system-token-only colour/type/shape, standalone OnPush components, `@if` / `@for` / `@switch` control-flow, `*.service.contract.ts` injection-token consumption, `data-testid` only when a Playwright POM reads it.
- **F2.** Added a `REQ-NFR-8` row to the §9 coverage matrix referencing the §3.0 conventions plus the per-slice `ng build` 0/0 obligation in FI1.
- **F3.** §3.0 enumerates the Material 3 primitives the frontend uses; per-slice tasks in FT1 will name the specific primitives each component imports.
- Doc status line updated to `approved (FP2 pass 2 clean)`.

## Pass 2 — findings

Re-ran every FP2 explicit check plus the two acceptance gates from §11.

### Explicit checks

1. **Three libraries with correct dep direction** — pass. §1 specifies `components → ⌀; domain → api; tickbox → all three`. §10 calls out the rule and lists the violations the plan does NOT make.
2. **Every planned service has a `*.service.contract.ts`** — pass. §4 lists three services, each paired with its contract file.
3. **Angular Material 3 components specified** — pass after F3. §3.0 enumerates the M3 primitives consumed; §5 enumerates the system-token surface.
4. **Design tokens specified** — pass. §5 enumerates colour-role, type-scale, shape, and elevation tokens.
5. **BEM naming assumed** — pass after F1. §3.0 makes BEM explicit and bans utility-class frameworks.
6. **Per-file split assumed** — pass after F1. §3.0 spells out `.html` / `.scss` / `.ts`.
7. **Playwright POM tests planned for important flows** — pass. §7.1 lists 10 page objects; §7.2 lists 17 specs each tied to one or more requirements.
8. **Both auth flows planned (PKCE OIDC + local)** — pass. §6.1 covers local sign-in, sign-up, refresh, sign-out; §6.2 covers PKCE OIDC begin + callback; REQ-AUTH-3 AC2 is honoured by the OIDC button hiding when the env flag is off.
9. **Mock-to-screen mapping is exhaustive** — pass. Verified against `mocks/`:
   - `sign-in.html` → §2 `/sign-in`
   - `sign-up.html` → §2 `/sign-up`
   - `password-reset.html` → §2 `/password-reset/request` and `/password-reset/complete` (both phases of the same mock)
   - `todos.html` → §2 `/todos`
   - `todos-empty.html` → §2 `/todos` (empty-state branch of the same page component)
   - `todo-detail.html` → §2 `/todos/new` and `/todos/:id` (same component, two modes)
   - `profile.html` → §2 `/profile`
   - `error.html` → §2 `/error`
   - `index.html` → mock directory only; correctly omitted (not a product surface).

### Acceptance gates

A. **Every requirement appears in the plan.** Pass. After F2's fix, the coverage matrix in §9 covers REQ-AUTH-1..5, REQ-AUTH-7, REQ-TODO-1..8, REQ-TODO-3a, REQ-ACCT-1..4, REQ-ERR-1..2, REQ-NFR-1, REQ-NFR-2, REQ-NFR-6, REQ-NFR-7, REQ-NFR-8. Backend-only requirements (REQ-AUTH-6 JWT validation, REQ-NFR-3 password storage, REQ-NFR-4 audit log, REQ-NFR-5 per-user isolation) are correctly omitted from the frontend plan.

B. **Every plan item maps to a guidance rule, no plan item conflicts.** Pass. §10 walks every plan section to a guidance bullet and explicitly enumerates the conflicts the plan does NOT make (no utility CSS framework; no single-file components; no class-based service consumption; no cross-library dep-direction violations; no data-annotations equivalent on TS request shapes).

**Result:** zero blocking findings on Pass 2. Frontend plan approved. FP2 done.
