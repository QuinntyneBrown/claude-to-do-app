# FT2 — Evaluate frontend tasks

Evaluator: `claude@M5`. Reviewed `docs/plans/frontend-tasks.md` against the approved `docs/plans/frontend.md`, the accepted mocks under `mocks/`, the MF1 frontend MVP under `frontend/`, and the Frontend / Library Structure / Authentication (frontend side) / Testing / General sections of the workflow's Implementation Guidance.

## FT2 explicit checks

- Every task is a true vertical UI slice.
- No "scaffolding only" tasks.
- Library boundaries are respected (a task in `components` never imports from `api`).
- Every task names its Playwright POM acceptance test.
- Every task names which guidance rules it must satisfy.
- Sizing is small enough that one task = a few loop iterations.

## Pass 1 — findings

Two findings. Zero blocking, two non-blocking notes.

- **F1 — F-005 and F-006 are at the upper end of the 1–3 iteration sizing.** F-005 ships 5 components in the `components` library, 3 in `domain`, 1 page in `tickbox`, plus an `ITodosService` extension and a Playwright spec set. F-006 ships 1 reusable (`ConfirmDialogComponent`), 2 domain components (`TodoEditFormComponent`, `TodoActivityListComponent`), 1 page, 4 service-method extensions, and 4 specs. Tried to split each — F-005a "AppShell only" and F-006a "ConfirmDialog only" would both be **scaffolding-only** tasks, which the FT2 explicit checks forbid. The infrastructure is correctly bundled with the first feature that consumes it. **Non-blocking note** — record so FI1 budgets 3 iterations for these and doesn't try to split them mid-stream.
- **F2 — F-011 (responsive sweep) is the thinnest acceptable slice.** It ships **one** new Playwright spec (`responsive.spec.ts`) that snapshots every prior slice's routes at 360/768/1440 and asserts breakpoint behaviour (nav-rail vs nav-bar visibility), no-horizontal-scroll, and ≥48dp touch-target hit-boxes. It does not add new components — but it ships a real cross-cutting acceptance test that depends on every prior slice. Borderline. Considered folding it into each prior slice's per-slice "Verify visually at 360/768/1440" loop step (FI1 step 4), but that's a manual visual check; the automated assertions belong in one place after all routes exist. Keeping F-011 as the slice that lands the spec; it's small (1 file) but has a real, observable deliverable. **Non-blocking note**.

### Walk of every task

I checked each F-NNN against the six FT2 checks:

| Task   | Vertical?                                  | End-to-end value                                  | Forbidden abstractions / boundary breaks? | POM spec named?                                                                                       | Guidance rules?                                | Size           |
|--------|--------------------------------------------|---------------------------------------------------|-------------------------------------------|--------------------------------------------------------------------------------------------------------|-----------------------------------------------|----------------|
| F-001  | ✓ route → page → form → service → backend  | sign-up new account                               | none                                       | `sign-up.spec.ts` × 2 cases                                                                            | Auth local + REQ-AUTH-7                       | 1 iter         |
| F-002  | ✓                                          | sign-in error UX + silent renewal + SSO toggle    | none                                       | `sign-in.spec.ts` × 4 cases (incl. silent-renewal)                                                     | Auth §"JWTs validated"; REQ-AUTH-3 AC2        | 2 iters        |
| F-003  | ✓                                          | password reset request + complete                 | none                                       | `password-reset.spec.ts` × 3 cases                                                                     | REQ-AUTH-4, REQ-AUTH-7                        | 2 iters        |
| F-004  | ✓                                          | OIDC PKCE callback                                | none                                       | `oidc-sign-in.spec.ts` × 3 cases                                                                       | Auth §"PKCE OIDC"                             | 1–2 iters      |
| F-005  | ✓                                          | full Todos list with mock fidelity (rebuild MVP)  | none — `components` lib has no api import | `todos-list.spec.ts` × 5 cases + `todo-toggle.spec.ts` (list-side)                                     | Frontend §"Mobile-first"; Library Structure   | 3 iters        |
| F-006  | ✓                                          | full Todo detail (create / edit / toggle / delete / activity) | none                          | `todo-create.spec.ts`, `todo-edit.spec.ts` × 2, `todo-toggle.spec.ts` (detail-side), `todo-delete.spec.ts` | REQ-TODO-1..8                          | 2–3 iters      |
| F-007  | ✓                                          | profile view + display-name + change-password + sign-out | none                              | `profile-view.spec.ts`, `profile-update-display-name.spec.ts` × 2, `profile-change-password.spec.ts` × 2, `sign-out.spec.ts` | REQ-ACCT-1..3, REQ-AUTH-5 | 2 iters        |
| F-008  | ✓                                          | email-change request banner + confirm + cancel    | none                                       | `profile-email-change.spec.ts` × 4 cases                                                               | REQ-ACCT-2 AC2/AC3                            | 2 iters        |
| F-009  | ✓                                          | delete account                                     | none                                       | `profile-delete-account.spec.ts` × 2 cases                                                             | REQ-ACCT-4                                    | 1 iter         |
| F-010  | ✓                                          | error page + global snackbar + retry              | none                                       | `error-state.spec.ts` × 3 cases                                                                        | REQ-ERR-1, REQ-ERR-2                          | 1–2 iters      |
| F-011  | ✓ (cross-cutting spec)                     | responsive correctness across every route          | none                                       | `responsive.spec.ts` (single file, asserts every route)                                                | REQ-NFR-1, REQ-NFR-2, REQ-NFR-7               | 1 iter         |

`grep` over `frontend-tasks.md` for utility-class names (`tailwind`, `bootstrap`, `flex-`, `p-` patterns), forbidden service-consumption phrases ("inject the concrete", "useClass: TodosService" outside `app.config`), and cross-library imports against the dep direction returns no matches. Every component placement honours `components → ⌀; domain → api; tickbox → all three`.

### Fixes applied between Pass 1 and Pass 2

- F1 / F2: no plan changes; recorded as guardrails for FI1 (budget 3 iterations for F-005 / F-006; do not attempt to split AppShell or ConfirmDialog out of their consumers).
- Doc status line updated to `approved (FT2 pass 2 clean)`.

## Pass 2 — findings

Re-ran every FT2 explicit check.

- Every task is a true vertical UI slice. ✓ Each row above ticks the route → page → component → service contract → backend → spec column.
- No scaffolding-only task. ✓ The two infrastructure-heavy slices (F-005 AppShell + Todos list, F-006 ConfirmDialog + Todo detail) bundle the new infra with the first feature that consumes it; F-002's `ErrorBannerComponent` and F-007's `IAccountService` are similarly bundled. F-011 ships an observable cross-cutting spec — the thinnest acceptable slice but a real one.
- Library boundaries respected. ✓ Every component placed in `components` consumes only Material primitives (no api / domain imports). Every component placed in `domain` consumes one or more `api` `*.service.contract.ts` injection tokens. Every page placed in `tickbox` may compose freely across the three libraries. No reverse-direction imports.
- Every task names ≥ 1 Playwright POM acceptance spec. ✓ Counted in the table above; 38 named test cases across 17 spec files (matches FP1 §7.2).
- Every task names which guidance rules apply. ✓ Common rules at the top of the task list + per-row "Specific guidance" or REQ references in the right-hand column above.
- Sizing. ✓ Every task lands in 1–3 loop iterations. F-005 and F-006 are at the top of that range; F1 records the rationale for not splitting.

**Result:** zero blocking findings on Pass 2. Frontend task list approved. FT2 done.
