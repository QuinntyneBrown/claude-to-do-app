# BT2 — Evaluate backend tasks

Evaluator: `claude@M5`. Reviewed `docs/plans/backend-tasks.md` against the approved `docs/plans/backend.md` and the Backend / Validation / Authentication / General sections of the workflow's Implementation Guidance.

## BT2 explicit checks

- Every task is a true vertical slice.
- No "scaffolding only" tasks with no end-to-end value.
- No task introduces a repository, unit-of-work, or other forbidden abstraction.
- Every task names its acceptance test.
- Every task names which guidance rules it must satisfy.
- Sizing is small enough that one task = a few loop iterations.

## Pass 1 — findings

Two findings. One blocking, one non-blocking note.

- **F1 — B-005's migration number was unspecified.** The original B-005 entry said "Numbering: this slice introduces migration #002b alongside #002, OR a separate migration after #008 depending on relative landing order; BT2 will pin the number." That punt belongs to BT2 by definition; leaving it for BI1 invites two slices to fight over a migration number. **Blocking** (procedural — easy fix).
- **F2 — B-001 is the thinnest acceptable slice.** B-001 (RBAC scaffolding + role on register + JWT role claim + `[Authorize(Roles="User")]` tightening) does not ship a new user-visible feature; it changes the JWT and tightens authorization. The behavior change is observable via the named acceptance test (`Register_creates_user_with_User_role_and_token_carries_role_claim`) and via every later slice that depends on `Roles="User"`, so it is not "scaffolding only" — but it is the thinnest a slice can be. **Non-blocking note**: any future infrastructure-only proposal MUST be paired with a user-visible improvement before it can become a slice.

### Walk of every task

I checked each B-NNN against the six BT2 checks:

| Task   | Vertical?                         | End-to-end value                              | Forbidden abstractions? | Acceptance test named?                                                                                                          | Guidance rules?                              | Size           |
|--------|-----------------------------------|-----------------------------------------------|-------------------------|---------------------------------------------------------------------------------------------------------------------------------|----------------------------------------------|----------------|
| B-001  | ✓ controller→command→DB→migration | RBAC + role claim observable                  | none                    | `Register_creates_user_with_User_role_and_token_carries_role_claim`                                                             | RBAC + General one-type-per-file             | 1–2 iters      |
| B-002  | ✓                                  | lockout + audit                               | none                    | 2 named tests                                                                                                                   | "Repeated failed sign-ins are rate-limited"  | 2 iters        |
| B-003  | ✓                                  | `/refresh` and `/sign-out` endpoints          | none                    | 3 named tests                                                                                                                   | JWT validation, REQ-NFR-6                    | 2 iters        |
| B-004  | ✓                                  | password reset + email no-op                  | none                    | 3 named tests                                                                                                                   | Salted hashes, REQ-AUTH-7                    | 2 iters        |
| B-005  | ✓                                  | OIDC PKCE sign-in                             | none                    | 4 named tests                                                                                                                   | "PKCE-based OAuth 2.0 / OIDC"                | 2–3 iters      |
| B-006  | ✓                                  | profile read + update                         | none                    | 3 named tests                                                                                                                   | Per-user isolation                           | 1 iter         |
| B-007  | ✓                                  | email-change request/confirm/cancel           | none                    | 4 named tests                                                                                                                   | Per-user isolation                           | 2 iters        |
| B-008  | ✓                                  | change password                               | none                    | 3 named tests                                                                                                                   | REQ-AUTH-7, REQ-NFR-4                        | 1–2 iters      |
| B-009  | ✓                                  | delete account                                | none                    | 2 named tests                                                                                                                   | Per-user isolation, REQ-NFR-4                | 1 iter         |
| B-010  | ✓                                  | extended Create + activity write              | none                    | 2 named tests                                                                                                                   | one-type-per-file                            | 1–2 iters      |
| B-011  | ✓                                  | `GET /api/todos/{id}` with activity           | none                    | 2 named tests                                                                                                                   | Per-user isolation                           | 1 iter         |
| B-012  | ✓                                  | `PUT /api/todos/{id}`                         | none                    | 3 named tests                                                                                                                   | Per-user isolation                           | 1 iter         |
| B-013  | ✓                                  | `PATCH /api/todos/{id}/status` + activity     | none                    | 3 named tests                                                                                                                   | one-type-per-file                            | 1–2 iters      |
| B-014  | ✓                                  | `DELETE /api/todos/{id}`                      | none                    | 2 named tests                                                                                                                   | Per-user isolation                           | 1 iter         |
| B-015  | ✓                                  | server-side ordering                          | none                    | `Get_todos_orders_by_due_date_ascending_nulls_last_then_created_at_descending`                                                  | one-type-per-file                            | 1 iter         |

`grep` over `backend-tasks.md` for `Repository`, `UnitOfWork`, `IRepository`, `IUnitOfWork`, `GenericService`, `EventBus`, `MessageBus`, `Mediator(?!R)` returned no matches. No forbidden abstraction in any slice.

### Fixes applied between Pass 1 and Pass 2

- **F1.** B-005 now pins migration **#009 `AddOidcAuthorizationRequests`** with the schema `(State PK, CodeVerifier, ExpiresAt)`. The table ships unconditionally so the only OIDC-conditional registration is the real `OidcClient` itself. The plan-coverage cross-check row for §7 was extended to mention #009.
- **F2.** No code change; recorded as a guardrail for future task-list updates.

## Pass 2 — findings

Re-ran every BT2 check.

- Every task is a true vertical slice. ✓ (every row above ticks the controller → command → handler → DB → test column.)
- No scaffolding-only task. ✓ (with B-001's note acknowledged.)
- No forbidden abstractions. ✓ (`grep` clean.)
- Every task names ≥ 1 acceptance test. ✓ (counted above; 41 named tests in total across 15 slices.)
- Every task names which guidance rules apply. ✓ (Common rules at the top + per-task "Specific guidance rules" or per-row column.)
- Sizing. ✓ (every task lands in 1–3 loop iterations. B-005 is the largest at 2–3 iters; not large enough to demand splitting since splitting would produce non-end-to-end fragments.)

**Result:** zero blocking findings on Pass 2. Backend task list approved. BT2 done.
