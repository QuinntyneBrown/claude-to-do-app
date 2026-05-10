# Tickbox — Test Plan

Author: `claude@M5` (TP1)
Status: draft (TP3 will execute this five times)
Source: `docs/requirements.md` (approved). One scenario section per requirement, traceable by requirement ID. Plus regression and edge-case scenarios at the bottom.

## How to use this plan

A pass run by TP3 walks every scenario from top to bottom against a running instance of the app (per `docs/runbooks/local.md`). For each scenario:

- **Pass** — the scenario's "Then" matches the observed behaviour exactly. Move on.
- **Fail** — note the deviation in `docs/bugs/pass-N.md`. Continue the pass; do not stop on first fail.

After a pass, fix every logged bug, then start pass N+1. Pass 5 must finish with zero new fails.

## Test environment

- OS: Windows 11 (primary), with Chromium via Playwright for browser-driven scenarios.
- Backend: `dotnet run` against the `Tickbox.Api` project, SQL Server LocalDB.
- Frontend: `ng serve` on `http://localhost:4200`, talking to the backend on `http://localhost:5217`.
- Test data: each pass starts from a fresh database (or a re-migrated dev DB; document the choice in the pass log).

Two reusable seed accounts (created by sign-up flow at the start of each pass):

- **A** — `ada@example.com` / `correct-horse-battery-staple-1`, display name "Ada Lovelace".
- **B** — `bob@example.com` / `correct-horse-battery-staple-2`, display name "Bob Babbage".

Account A is the primary actor; Account B exists to prove cross-user isolation (REQ-NFR-5).

---

## 1. Authentication

### S-AUTH-1 — Account creation (covers REQ-AUTH-1)

- **AC1.** Sign-up with valid display name + unused email + 12-char password. **Then** routed to `/todos` and signed in.
- **AC2.** Sign-up with an email already used by Account A. **Then** form shows an error; no account created.
- **AC3.** Sign-up with an 11-character password. **Then** form shows the policy error inline.
- **AC4.** After any failed sign-up attempt, inspect the database `Users` table; password column is `NULL` for the failed row (or no row exists). Plaintext passwords MUST never appear in `Users`, `RefreshTokens`, `AuditLog`, or any log file.

### S-AUTH-2 — Sign-in local (covers REQ-AUTH-2)

- **AC1.** Sign-in with Account A's correct credentials. **Then** access JWT issued; routed to `/todos`.
- **AC2.** Sign-in with Account A's email but a wrong password, AND sign-in with an unknown email. Both responses MUST be the same generic "incorrect email or password" string (no enumeration).
- **AC3.** Submit five wrong-password attempts for Account A within 15 minutes. **Then** the sixth attempt is rate-limited / locked out, and a `SignInLockout` audit entry exists.

### S-AUTH-3 — OIDC sign-in (covers REQ-AUTH-3)

- **AC1.** With OIDC enabled (`window.__TICKBOX_OIDC_ENABLED__ = true`), click "Sign in with SSO". Complete the IdP flow against the test IdP. **Then** signed in to Tickbox with the same session shape as the local flow.
- **AC2.** With OIDC disabled, the SSO entry point is hidden on `/sign-in`.

### S-AUTH-4 — Password reset (covers REQ-AUTH-4)

- **AC1.** On `/password-reset/request`, enter Account A's email. **Then** see "if that account exists, a reset link is on its way." Repeat with a never-registered email — same exact copy. (Email enumeration must not leak.)
- **AC2.** Use the most recent reset token from the audit log (or the dev mailer log) to set a new password ≥12 chars. **Then** the password is updated, A's existing JWT no longer works, and the user is signed in fresh.
- **AC3.** Re-use the same token. **Then** rejected with a clear inline error and an audit entry recording the rejected attempt.

### S-AUTH-5 — Sign-out (covers REQ-AUTH-5)

- **AC1.** Sign in as A, navigate to `/profile`, click "Sign out". **Then** routed to `/sign-in`. Open the API explorer with the now-stale JWT — request to `/api/todos` returns 401. The refresh cookie is cleared.

### S-AUTH-6 — JWT validation (covers REQ-AUTH-6)

- **AC1.** Send a request to `/api/todos` with each of: missing `Authorization`, expired token (mint via test helper or wait), tampered signature. **Then** all three return HTTP 401.
- **AC2.** Sign in as A and request `/api/todos`. **Then** only A's to-dos are in the response — none of B's, even if B has more rows. Cross-check by signing in as B and confirming the inverse.

### S-AUTH-7 — Password policy (covers REQ-AUTH-7)

- **AC1.** Sign-up / change-password with exactly 12 characters. **Then** accepted.
- **AC2.** Sign-up with 12 chars all-lowercase, no digits, no symbols. **Then** accepted (no complexity rules).
- **AC3.** Sign-up with 257 characters. **Then** HTTP 400 with inline error.

---

## 2. To-dos

### S-TODO-1 — Two-state model (covers REQ-TODO-1)

- **AC1.** Create a to-do via `POST /api/todos`. **Then** the response shape has `status: "Incomplete"`.
- **AC2.** PATCH the status to `"Pending"`. **Then** HTTP 400 / `ValidationProblemDetails`.

### S-TODO-2 — Create a to-do (covers REQ-TODO-2)

- **AC1.** From `/todos`, tap the FAB. **Then** routed to `/todos/new`.
- **AC2.** Enter title `"Buy milk"` and save. **Then** the new to-do appears in the Incomplete section after returning to `/todos`.
- **AC3.** Enter an empty title and save. **Then** inline "Title is required" error; no POST is sent.
- **AC4.** Enter a 201-character title. **Then** HTTP 400 / inline error. Repeat for a 2001-character notes value — same outcome.

### S-TODO-3 — View list (covers REQ-TODO-3)

- **AC1.** As A with two Incomplete + one Complete to-do, open `/todos`. **Then** sections labelled "Incomplete" (2 items) and "Complete" (1 item); summary "1 of 3 complete".
- **AC2.** Tap each filter chip in turn. **Then** All / Incomplete / Complete each shows the correct subset.
- **AC3.** Sign in as B (whose data is empty) and confirm B sees zero of A's items.
- **AC4.** Create three to-dos with due dates (today, tomorrow, no-due-date) and watch the order: today first, tomorrow second, no-due-date last; within tied due dates, the most recently created appears first.

### S-TODO-3a — Page-header date label (covers REQ-TODO-3a)

- **AC1.** Open `/todos` on 9 May. **Then** header reads "Today, 9 May" (or local equivalent if the test machine is in a different locale).

### S-TODO-4 — Edit a to-do (covers REQ-TODO-4)

- **AC1.** Open one of A's to-dos at `/todos/<id>`, change title + notes + due date, save. **Then** values persist on next list view.
- **AC2.** As B, GET `/api/todos/<A's id>`. **Then** HTTP 404 (not 403 — no enumeration).

### S-TODO-5 — Toggle state (covers REQ-TODO-5)

- **AC1.** From `/todos`, tap the checkbox of an Incomplete item. **Then** it moves to the Complete section without a page reload.
- **AC2.** From `/todos`, tap the checkbox of a Complete item. **Then** it moves to Incomplete.
- **AC3.** From `/todos/<id>`, click the "Complete" status chip. **Then** the same outcome as AC1.

### S-TODO-6 — Delete a to-do (covers REQ-TODO-6)

- **AC1.** From the detail screen, tap "Delete", confirm. **Then** routed back to `/todos`; the to-do is gone. The DB `Todos` table no longer contains the row (hard delete).

### S-TODO-7 — Empty state (covers REQ-TODO-7)

- **AC1.** Sign in as a brand-new account with zero to-dos. **Then** `/todos` renders the empty-state mock with the "Add a to-do" CTA, which routes to `/todos/new`.

### S-TODO-8 — Activity strip (covers REQ-TODO-8)

- **AC1.** On a freshly created to-do detail, the strip shows one entry: "Created · `<DD MMM, HH:mm>`".
- **AC2.** Mark Complete, refresh detail. **Then** strip adds "Marked complete · `<DD MMM, HH:mm>`". Toggle back to Incomplete — the "Marked complete" entry disappears (no history of toggles is kept).
- **AC3.** Strip is read-only (no input field, no edit affordance).

---

## 3. Account management

### S-ACCT-1 — View profile (covers REQ-ACCT-1)

- **AC1.** Visit `/profile` as A. **Then** display name, email, and avatar (or the initials placeholder) are rendered. Email matches sign-up.

### S-ACCT-2 — Update display name and email (covers REQ-ACCT-2)

- **AC1.** Change display name and save. **Then** new name shows on the profile screen and in the avatar (initials).
- **AC2.** Request an email change to a new address. **Then** the inline pending-email banner appears with a "Cancel" action. The original email still authenticates sign-in.
- **AC3.** Click the verification link from the dev mailer log within its expiry window. **Then** new email becomes the sign-in identifier; the banner disappears.

### S-ACCT-3 — Change password (covers REQ-ACCT-3)

- **AC1.** Submit current password = wrong. **Then** clear inline error; the password is unchanged. An audit entry of type `PasswordChangeRejected` is recorded.
- **AC2.** Submit current password = correct, new password = ≥12 chars. **Then** the new password authenticates; any other browser tab still using the old JWT becomes 401-bound on next request.

### S-ACCT-4 — Delete account (covers REQ-ACCT-4)

- **AC1.** Click "Delete account", confirm in the dialog. **Then** routed to `/sign-in`. Attempts to sign in with A's credentials return the generic 401. A's `Users` row, `Todos` rows, `RefreshTokens`, and active sessions are all removed.

---

## 4. Error handling

### S-ERR-1 — Network / server failure (covers REQ-ERR-1)

- **AC1.** Stop the backend; navigate to `/todos`. **Then** the error empty-state appears with a "Try again" button. Restart the backend, click "Try again". **Then** the list loads.
- **AC2.** On `/todos/new`, fill title + notes, then stop the backend, click Save. **Then** an inline error appears AND the title/notes input are still populated. Restart backend, click Save again. **Then** the to-do is created.

### S-ERR-2 — Validation errors (covers REQ-ERR-2)

- **AC1.** Trigger a 400 from each form (sign-up too-short password, todo empty title, change-password wrong current). **Then** every error renders inline against the offending form, never as an unhandled toast or console-only.

---

## 5. Non-functional

### S-NFR-1 — Mobile-first responsive (covers REQ-NFR-1)

- Open every route at viewport widths 360 / 768 / 1440 in the browser dev tools.
- **Then** at 360 the bottom nav-bar is visible and the side rail is hidden; at 768 the rail is collapsed and the bar is hidden; at 1440 the rail is expanded with labels. No horizontal scroll on any route at any width.

### S-NFR-2 — Material 3 visual language (covers REQ-NFR-2)

- Compare each route to its corresponding mock under `mocks/`.
- **Then** colour roles, type scale, and shape match (within the 5-pixel margin of "looks identical"). Any new component uses M3 primitives, not custom-rolled.

### S-NFR-3 — Password storage (covers REQ-NFR-3)

- Open the live DB and inspect the `Users.PasswordHash` column.
- **Then** every value starts with `$2a$` or `$2b$` (bcrypt). No row contains a recognisable plaintext fragment of the seed password.

### S-NFR-4 — Audit log (covers REQ-NFR-4)

- Trigger each event during the pass: failed sign-in, lockout, password change, password reset request, password reset use, account deletion.
- **Then** each leaves a row in the audit log table (or its analogue) with the event kind, timestamp, and user-id (where applicable). No password / token plaintext appears in the row.

### S-NFR-5 — Per-user isolation (covers REQ-NFR-5)

- As A, retrieve the access token. Sign in as B in another browser; capture B's id and a B-owned to-do id.
- Now in a clean curl session, with A's token, call `GET /api/todos/<B's id>` and `PATCH /api/todos/<B's id>/status`.
- **Then** both return HTTP 404. The audit log records nothing for B.

### S-NFR-6 — Client-side data hygiene (covers REQ-NFR-6)

- After signing in, open dev tools.
- **Then** `localStorage` contains nothing about Tickbox auth. `sessionStorage["tickbox.access-token"]` contains the access JWT only. The refresh-token cookie is `HttpOnly; Secure; SameSite=Strict; Path=/api/auth`.

### S-NFR-7 — Accessibility (covers REQ-NFR-7)

- Run axe-core (or an equivalent automated WCAG 2.1 AA scanner) against `/sign-in`, `/todos`, `/todos/new`, `/profile`, `/error`.
- **Then** zero violations on the level "serious" and above. Tab order on every form is logical. Every interactive element on `/todos` and `/profile` has bounding box ≥ 48 × 48 px at viewport 360.

### S-NFR-8 — Build cleanliness (covers REQ-NFR-8)

- Run `dotnet build backend/Tickbox.sln -warnaserror` from a clean checkout.
- Run `ng build api && ng build components && ng build domain && ng build tickbox` from the frontend folder.
- **Then** both produce zero errors and zero new warnings. The single command in `docs/runbooks/local.md` brings both up.

---

## 6. Regression scenarios

A regression scenario re-runs a previously-found-and-fixed bug surface to make sure it stays fixed. Seed this section as bugs are logged in `docs/bugs/pass-N.md`. Each entry:

- Title (e.g., "R-001 — Refresh interceptor double-fires on race")
- Original symptom
- Steps to reproduce
- Expected behaviour after fix

The first run (TP3 pass 1) starts with this section empty. By pass 5 it should be the longest section in the document.

---

## 7. Edge cases

These don't map 1:1 to a requirement; they probe boundaries and stress points uncovered by the rubric.

### E-001 — Concurrent toggle from two tabs

- Sign in as A in two tabs. In tab 1 mark a to-do Complete. In tab 2 (without refreshing) toggle the same to-do via the list checkbox.
- **Then** the second toggle either succeeds (the to-do bounces to Incomplete) or returns a clear conflict error. Either is acceptable; what's NOT acceptable is silent corruption (UI showing Complete while DB has Incomplete).

### E-002 — Time-zone boundary on header date

- Set the test machine's timezone to UTC+13 just before midnight UTC. Open `/todos`.
- **Then** the header date matches the local calendar day, not the UTC day. (Catches naive `DateTime.UtcNow.Date` bugs.)

### E-003 — Very long display name in the avatar tooltip

- Set display name to 120 characters of letters.
- **Then** the avatar still renders cleanly (initials are derived from the first one or two characters); no layout overflow on the top app bar at 360px.

### E-004 — Pasting a JWT into the email field

- On `/sign-in`, paste a 600-character string into the email input.
- **Then** form validation rejects it before any network call (well-formed-email check + length cap).

### E-005 — Activity strip after sign-out / sign-in

- Mark a to-do Complete, sign out, sign back in, open the same to-do.
- **Then** the activity strip still shows Created + Marked complete. (Catches a bug where activity is held only in memory.)

### E-006 — Browser back button after delete

- Delete a to-do, then press the browser Back button.
- **Then** the user lands on `/todos/<deleted-id>` which immediately redirects to `/todos` (because the GET returns 404). The user never sees a stale form.

### E-007 — Email change cancel during pending state

- Request an email change, then click "Cancel" in the banner.
- **Then** the banner disappears immediately, the original email stays the sign-in identifier, and any later use of the previously-sent verification link fails (token is invalidated server-side, audit entry recorded).

### E-008 — Empty SignOut response

- Server returns 502 to `POST /api/auth/sign-out`.
- **Then** the user is still signed out client-side and routed to `/sign-in`. (The local clear runs unconditionally to avoid a stuck-half-signed-out state — confirmed by `ProfilePageComponent.signOut`.)

### E-009 — Deleted account still has refresh-cookie on disk

- Delete account A, then in the same browser visit `/todos`.
- **Then** the refresh interceptor's silent-renewal attempt fails; the user is bounced to `/sign-in`. No 500 from the API.

### E-010 — Filter chip after deleting last item in a state

- With one Incomplete and zero Complete to-dos, mark the Incomplete one Complete via the list checkbox. Then click the "Incomplete" filter chip.
- **Then** the list shows the empty-section state for Incomplete (or a graceful "Nothing here yet" — but never a stuck loading bar or a duplicated entry).

---

## 8. Coverage matrix

Every requirement in `docs/requirements.md` maps to at least one scenario above:

| Requirement       | Scenarios               |
|-------------------|-------------------------|
| REQ-AUTH-1        | S-AUTH-1                |
| REQ-AUTH-2        | S-AUTH-2                |
| REQ-AUTH-3        | S-AUTH-3                |
| REQ-AUTH-4        | S-AUTH-4                |
| REQ-AUTH-5        | S-AUTH-5                |
| REQ-AUTH-6        | S-AUTH-6                |
| REQ-AUTH-7        | S-AUTH-7                |
| REQ-TODO-1        | S-TODO-1                |
| REQ-TODO-2        | S-TODO-2                |
| REQ-TODO-3        | S-TODO-3                |
| REQ-TODO-3a       | S-TODO-3a               |
| REQ-TODO-4        | S-TODO-4                |
| REQ-TODO-5        | S-TODO-5                |
| REQ-TODO-6        | S-TODO-6                |
| REQ-TODO-7        | S-TODO-7                |
| REQ-TODO-8        | S-TODO-8                |
| REQ-ACCT-1        | S-ACCT-1                |
| REQ-ACCT-2        | S-ACCT-2                |
| REQ-ACCT-3        | S-ACCT-3                |
| REQ-ACCT-4        | S-ACCT-4                |
| REQ-ERR-1         | S-ERR-1                 |
| REQ-ERR-2         | S-ERR-2                 |
| REQ-NFR-1         | S-NFR-1                 |
| REQ-NFR-2         | S-NFR-2                 |
| REQ-NFR-3         | S-NFR-3                 |
| REQ-NFR-4         | S-NFR-4                 |
| REQ-NFR-5         | S-NFR-5, E-001          |
| REQ-NFR-6         | S-NFR-6                 |
| REQ-NFR-7         | S-NFR-7                 |
| REQ-NFR-8         | S-NFR-8                 |

Every L2 requirement has at least one scenario. TP1 done.
