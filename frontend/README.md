# Tickbox frontend

Angular 21 workspace for Tickbox. Reference implementation for the patterns every frontend slice will follow downstream of MF1.

## Workspace layout

```
frontend/
  angular.json
  package.json
  playwright.config.ts
  projects/
    api/                   # data + service contracts (no UI)
      src/lib/
        api-config.ts            # base URL injection token
        todo.ts                  # Todo + TodoStatus types
        todos.service.contract.ts  # ITodosService + TODOS_SERVICE token
        todos.service.ts         # HttpClient implementation
        auth.service.contract.ts # IAuthService + AUTH_SERVICE token
        auth.service.ts          # HttpClient implementation
    components/            # reusable UI (no api dependency)
      src/lib/
        brand-icon.component.{ts,html,scss}
    domain/                # UI bound to api contracts
      src/lib/
        todos-list.component.{ts,html,scss}
    tickbox/               # main app — depends on all three libs
      src/app/
        auth/{auth-state.service.ts, auth.interceptor.ts, auth.guard.ts}
        sign-in/sign-in.component.{ts,html,scss}
        todos/todos-page.component.{ts,html,scss}
        app.{ts,html,scss}
        app.config.ts
        app.routes.ts
      src/styles.scss            # Material 3 theme via mat.theme(...)
  e2e/                     # Playwright Page Object Model acceptance tests
    page-objects/{sign-in.page.ts, todos.page.ts}
    specs/sample-flow.spec.ts
```

Layer dependencies:

```
components ──▶  (nothing)
domain     ──▶  api
tickbox    ──▶  api, components, domain
```

The `tsconfig.json` `paths` map points `api`, `components`, `domain` at `dist/<name>` — so libraries are built once and the main app consumes them via the published surface.

## Patterns proved by the MVP

- **Interface-driven service consumption.** Every service has a `*.service.contract.ts` exporting an interface and an `InjectionToken<T>`. Components consume the token, not the concrete class. The main app's `app.config.ts` wires the concrete `TodosService` and `AuthService` against the tokens. Swapping in a fake for tests is a one-line provider override.
- **One type per file.** Every component is split into `.ts`, `.html`, and `.scss` (no inline templates or styles). Every other class, interface, type, and `InjectionToken` lives in its own file.
- **BEM CSS.** Block / Block__Element / Block--Modifier throughout (`brand-icon`, `brand-icon__mark`, `brand-icon__name`; `todos-list`, `todos-list__item`, `todos-list__item--complete`; `sign-in`, `sign-in__card`, `sign-in__error`; etc.). No utility-class frameworks.
- **Material 3.** `@use '@angular/material' as mat;` then `mat.theme(...)` in `styles.scss` to emit the M3 token set. Components consume the system tokens (`var(--mat-sys-primary)`, `var(--mat-sys-on-surface-variant)`, `var(--mat-sys-headline-medium)`, etc.) — no per-component palette overrides.
- **Auth.** A bearer token from `/api/auth/sign-in` is held by `AuthStateService` (in `sessionStorage`), attached to outbound requests by a functional `authInterceptor`, and gates routes via an `authGuard` that redirects unauthenticated visits to `/sign-in`.
- **Zoneless change detection** (`provideZonelessChangeDetection()`) — the Angular 21 default; no `zone.js` runtime.

## Run locally

Prerequisites: Node 20+ / npm 10+.

```powershell
cd frontend
npm install
npx ng build api
npx ng build components
npx ng build domain
npx ng serve tickbox          # http://localhost:4200
```

The app expects the backend on `http://localhost:5217` by default. Override at runtime by setting `window.__TICKBOX_API__` before bootstrap, or by editing `app.config.ts`.

To exercise the full stack: in another shell, `cd backend && dotnet run --project src/Tickbox.Api`. Register a user with `POST /api/auth/register` (the MVP does not yet ship a sign-up screen; that comes in FI1), then sign in via the UI.

## Test

Playwright Page Object Model end-to-end acceptance tests live under `e2e/`. The MVP ships one acceptance spec (`sample-flow.spec.ts`) that exercises:

- Sign in via the form (network-mocked).
- The to-dos page hits `GET /api/todos` with the bearer token.
- Adding a to-do hits `POST /api/todos` and updates the list.

```powershell
cd frontend
npx playwright install chromium    # first time only
npx ng serve tickbox                # in one shell
npx playwright test                 # in another shell
```

The tests mock the backend at the network layer with `page.route(...)`, so they do not require the .NET API to be running.

## Build

```powershell
npx ng build api
npx ng build components
npx ng build domain
npx ng build tickbox
```

Each `ng build` produces zero warnings and zero errors.

## Deferred (no-op) integrations

None yet. When a real OAuth identity provider is wired in for REQ-AUTH-3 (PKCE OIDC), it will sit behind `IAuthService`'s contract and the existing UI will not change.
