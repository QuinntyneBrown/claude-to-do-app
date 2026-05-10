# Tickbox — Frontend Runbook

Operational reference for the Angular workspace. For the single-command bring-up, see `local.md`.

## Stack

- Angular 21, standalone components, OnPush change detection, zoneless (`provideZonelessChangeDetection`).
- Angular Material 3 via the `mat.theme()` SCSS API; design tokens via `var(--mat-sys-*)`.
- Workspace: four projects in dependency order — **api** → **components** → **domain** → **tickbox** (the app).
  - `api`: service contracts (`*.service.contract.ts`), injection tokens, models. No DOM.
  - `components`: presentation components, no api dep.
  - `domain`: components that consume api services.
  - `tickbox`: the route shell, page components, app config.

`tsconfig.json` paths point at `dist/` (the built libraries), not at source — that's what makes the Angular package format honour the public-api boundary.

## Run

```powershell
cd frontend
npx ng serve
```

Port: **4200**. Default API base URL: `http://localhost:5217` (`app.config.ts`); override at runtime by setting `window.__TICKBOX_API__` before bootstrap.

The app uses lazy `loadComponent` routes. The first request to a new route triggers a fresh chunk download — visible as a momentary delay on the first navigation after `ng serve` starts.

## Build order on a fresh clone

Libraries must be built before the app the first time:

```powershell
cd frontend
npm ci
npx ng build api
npx ng build components
npx ng build domain
npx ng build tickbox            # or `npx ng serve` for dev mode
```

The library builds emit to `dist/api`, `dist/components`, `dist/domain`. Once those exist, `ng serve` watches sources and rebuilds the app — but library sources are NOT auto-rebuilt. After editing a file in `projects/api` you must run `ng build api` (and the chained downstream libraries) for `ng serve` to pick up the change.

## Tests

```powershell
cd frontend
npx playwright test
```

51 specs across 11 spec files. Specs run against `http://localhost:4200` (the Playwright config picks up `baseURL`). Network calls to the backend are mocked per-test via `page.route('http://localhost:5217/...', ...)` — no live backend needed for any spec.

Selected test patterns (codified during FI1):

- Page Object Model in `e2e/page-objects/`. One POM per route.
- `data-testid` only on what the POM exercises. Don't sprinkle it on internals.
- For async assertions of mutable counters, use `expect.poll(() => count, { timeout: 5_000 }).toBeGreaterThanOrEqual(2)` — bare `expect(count)` races.
- For redirects to external URLs, intercept with `page.route('https://idp.test/**', fulfill-html)` rather than mocking `window.location.assign` (the latter is non-configurable in Chromium).

## Library dependency direction (enforced)

| Library     | May import from                        |
|-------------|----------------------------------------|
| `components`| (nothing in this workspace)            |
| `domain`    | `api`, `components`                    |
| `tickbox`   | `api`, `components`, `domain`          |

A `components` file that imports from `api` or `domain` is a blocking finding — move the file to `domain`. A `domain` file that imports from `tickbox` is also blocking — that means the dependency arrow is going the wrong way; refactor.

## Known issues

- **Vite error overlay intercepts pointer events.** When a library build fails (e.g., `Cannot find module 'domain'`), Vite's overlay sits on top of the page and blocks Playwright clicks. Fix by killing `ng serve`, running `ng build api && ng build components && ng build domain`, then restarting `ng serve`.
- **Orphan port 4200.** Sometimes a previous `ng serve` lingers and the new `ng serve` silently fails to bind. Detect with `Get-NetTCPConnection -LocalPort 4200`; kill with `Stop-Process -Id <pid> -Force`. The `start-local.ps1` script doesn't auto-kill orphans because it could clobber a deliberately-running peer.

## Smoke check

After `ng serve` is ready and the backend is up:

1. Open http://localhost:4200 → sign-in page renders.
2. `/sign-up` an account → routed to `/todos` showing the empty-state mock.
3. Tap the FAB → `/todos/new` opens with an empty form.

Anything earlier than (1) failing is a frontend bring-up problem; (2) failing means the backend is not reachable or CORS is wrong; (3) failing usually means the lazy-load chunk for `todo-detail-page-component` didn't build.
