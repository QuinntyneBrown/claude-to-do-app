# MF2 — Evaluate frontend MVP

Evaluator: `claude@M5`. Ran the Implementation Evaluation Rubric against the MF1 frontend MVP, scoped to the **General**, **Frontend (Angular)**, **Library Structure**, **Authentication (frontend side)**, and **Testing** sections of the workflow's Implementation Guidance, plus the explicit MF2 checks. Fix-on-find applied between passes.

## Pass 1 — findings

### Findings

- **F1 — Debug-style `console.error` in `todos-list.component.ts`.** The `catchError` callback in `TodosListComponent.refresh()` logged the error to the console while also surfacing a user-visible error message. The MF2 explicit checks list "no `console.log`-debug" — `console.error` isn't strictly debug, but in this case the UI surface already covers the diagnostic and the console call is redundant noise. **Non-blocking** (borderline) — fixed in pass 2 prep by removing the call. The bootstrap `console.error` in `main.ts` (a CLI default) remains; that is the genuine last-ditch error log when bootstrapping fails.
- **F2 — ATDD test-first not strictly followed for the MVP scaffold commit.** The Playwright POM page objects, the spec, and the implementation that satisfies them shipped in the same commit (`d57cb78`). Same finding as MB2 F2; recorded so FI1 enforces test-first on every vertical UI slice. **Non-blocking note.**
- **F3 — MVP visual coverage of the mocks is intentionally partial.** The MVP renders sign-in and a to-dos page that establish the M3 patterns from the mocks (color tokens, type scale, Material components, BEM, mobile-first layout) but does **not** implement the full app shell from `mocks/todos.html` — specifically the navigation rail / bottom nav-bar pair, the extended "New to-do" FAB, the All / Incomplete / Complete filter chip set, the two-section list (Incomplete / Complete), or the sticky page header. Those chrome elements are FI1 vertical-slice deliverables per the workflow ("MVPs exist to lock in patterns before scaling implementation"). **Non-blocking acknowledgement** — the MF1 article frames the MVP as "thin … proves the UI architecture", and the MF2 explicit checks list M3 / tokens / BEM / one-type-per-file / Playwright POM, all satisfied.

### Rubric walk

1. **Guidance adherence (Frontend / Library Structure / Auth / General).**
   - Three libraries — `api`, `components`, `domain` — plus the `tickbox` main app. ✓
   - Library dependency direction: `components` imports nothing from `api`/`domain` (`grep` returns no matches); `api` imports nothing from `domain` (no matches); `domain` imports from `api` (`TodosListComponent` consumes `ITodosService`/`TODOS_SERVICE`); `tickbox` imports from all three. ✓
   - Every api service has a `*.service.contract.ts` with an interface + `InjectionToken<T>`: `todos.service.contract.ts` exports `ITodosService` + `TODOS_SERVICE`; `auth.service.contract.ts` exports `IAuthService` + `AUTH_SERVICE`. ✓
   - Concrete services (`TodosService`, `AuthService`) are wired to their tokens in `tickbox/src/app/app.config.ts`. ✓
   - Angular Material components used: `mat-button`, `mat-form-field`, `mat-input`, `mat-checkbox`, `mat-icon`, `mat-progress-bar`, `mat-toolbar`. ✓
   - Material 3: `styles.scss` uses `@use '@angular/material' as mat;` and `mat.theme((color: (theme-type: light, primary: mat.$violet-palette, tertiary: mat.$rose-palette), typography: Roboto, density: 0))`. Angular Material 21 emits the M3 token set from `mat.theme(...)` (the M2 entry points are removed). ✓
   - Design tokens: components consume `var(--mat-sys-primary)`, `var(--mat-sys-on-surface-variant)`, `var(--mat-sys-headline-medium)`, `var(--mat-sys-surface-container-low)`, `var(--mat-sys-error-container)`, etc. — never per-component palette overrides. ✓
   - BEM naming: `brand-icon` / `brand-icon__mark` / `brand-icon__name`; `todos-list` / `todos-list__form` / `todos-list__items` / `todos-list__item` / `todos-list__item--complete` / `todos-list__empty` / `todos-list__error`; `sign-in` / `sign-in__brand` / `sign-in__card` / `sign-in__error` / `sign-in__submit`; `todos-page` / `todos-page__toolbar` / `todos-page__main` / `todos-page__spacer`. No utility-class frameworks. ✓
   - One type per file with separate `.html`, `.scss`, `.ts`. `grep` for inline `template:` / `styles:` arrays returns no matches. ✓
   - Auth: `AuthStateService` (sessionStorage + signal), functional `authInterceptor` attaching `Bearer` to outbound HTTP, `authGuard` redirecting unauthenticated visits to `/sign-in`. Sign-in calls the api library's `IAuthService` via the `AUTH_SERVICE` token. Token storage is `sessionStorage` (cleared on tab close), not `localStorage`, satisfying REQ-NFR-6 for the MVP scope. ✓
   - Testing: Playwright Page Object Model — `e2e/page-objects/sign-in.page.ts`, `e2e/page-objects/todos.page.ts`, spec at `e2e/specs/sample-flow.spec.ts`. ✓

2. **Requirements coverage in entirety.** The MVP scope is "prove the UI architecture and ship one auth slice + one todo slice". Both slices are real, end-to-end, and consume the api library's contracts. Non-MVP requirements (sign-up screen, password reset screen, profile management, todo detail screen, filter chips, two-section list, FAB, nav-rail) are out of MVP scope per the MF1 article and will be picked up by FI1 vertical slices.

3. **Radically simple.** Two routes, two pages, three libraries, one main app. No speculative abstractions, no dead code, no commented-out experiments. Every interface earns its keep: `ITodosService` and `IAuthService` are SOLID-justified by the library boundary.

4. **No temp code or stubs.** `grep` over `frontend/projects/` for `TODO|FIXME|XXX|HACK|NotImplementedException` returns no matches. After the F1 fix, `console.error` is restricted to the bootstrap default in `main.ts`. No empty methods; no hard-coded sentinel returns.

5. **One type per file, with separate `.html`/`.scss`/`.ts` for components.** Verified by inspection of every component (`brand-icon`, `todos-list`, `sign-in`, `todos-page`, `app`). Other types (services, contracts, tokens, guards, interceptors, models) each live in their own file.

6. **SOLID + interface-driven services.** Components depend on tokens (`TODOS_SERVICE`, `AUTH_SERVICE`), not on concrete classes. The main app's composition root is the only place that wires concretes. Library boundaries are respected: a component in `components` can be reused without dragging in HttpClient or any service contract.

7. **ATDD evidence.** See F2.

8. **Mobile-first + responsive.** Base styles target the smallest viewport; `@media (min-width: 600px)` and `@media (min-width: 1240px)` add tablet and desktop refinements. Spot-checked: sign-in card stretches full width below 600px and constrains to 420px above; the sign-in `padding` increases at 600px+. The todos page's toolbar and main padding scale at 600px and 1240px breakpoints. The todos-list form stacks vertically on mobile and goes inline at 600px+. Touch targets are Material defaults (≥48dp).

9. **Build and run clean.** `ng build` is clean for all four projects (`api`, `components`, `domain`, `tickbox`) — 0 warnings, 0 errors. The dev server (`ng serve tickbox`) on http://localhost:4200 boots and serves the SPA. The Playwright POM acceptance spec runs against the live dev server (mocking the backend at the network layer) and **passes green**.

### Fixes applied between Pass 1 and Pass 2

- **F1 fix.** Removed `console.error(err)` from `TodosListComponent.refresh()`'s `catchError`. The user-visible error surface (`.todos-list__error` element with `data-testid="todos-error"`) remains the diagnostic. The Playwright spec was re-run and still passes.
- **F2.** No code change; recorded for FI1.
- **F3.** No code change; the MVP is intentionally thinner than the full mock. FT1 will plan the FI1 slices that fill in the chrome.

## Pass 2 — findings

Re-ran every check against the post-fix tree.

1. Guidance adherence — pass.
2. Requirements coverage — pass.
3. Radically simple — pass.
4. No temp code / stubs — pass. After F1 fix, no `console.*` debug calls remain in product code (only `console.error` in `main.ts`'s bootstrap catch).
5. One type per file — pass.
6. SOLID + interface-driven — pass.
7. ATDD evidence — see F2.
8. Mobile-first + responsive — pass.
9. Build and run clean — pass. `ng build api/components/domain/tickbox` all 0 warnings 0 errors. Playwright spec green (1/1).

**Result:** zero blocking findings on Pass 2. MVP accepted as the frontend pattern reference. MF2 done.
