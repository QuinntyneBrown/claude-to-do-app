# Tickbox — Requirements

Author: `claude@M5` (R1, R2)
Status: approved (R2 pass 2 clean)
Sources: `docs/idea.md`, accepted mocks under `mocks/`

## 1. Vision and scope

Tickbox is a radically simple to-do app. A to-do has exactly **two states**: `Incomplete` and `Complete`. The product must do that one thing well, on mobile and on the web, behind authentication, and stop there. Anything that isn't in service of "capture a to-do, see it, finish it" is out of scope.

## 2. Users and roles

There is exactly one product role: **User** — an authenticated account holder who owns and manages their own to-dos.

Administrative concerns (operations, support tooling) are not user-facing and out of scope for v1.

RBAC scaffolding is required by the implementation guidance, so the system MUST be able to express roles end to end (DB → API → UI). The only role used in v1 is `User`.

## 3. Functional requirements

Every requirement below maps to one or more accepted mocks. Acceptance criteria are written as Given / When / Then so they can be lifted directly into Playwright POM and backend acceptance tests.

### 3.1 Authentication

Mocks: `sign-in.html`, `sign-up.html`, `password-reset.html`.

#### REQ-AUTH-1 — Account creation (local)
A visitor MUST be able to create an account using display name, email, and password.

- AC1. Given a visitor on the sign-up screen, when they submit a valid display name + unique email + password meeting policy, then a new account is created and the visitor is signed in and routed to the to-dos landing.
- AC2. Given a visitor submits an email that already has an account, when they submit, then the form shows an error and no new account is created.
- AC3. Given a visitor submits a password under 12 characters, when they submit, then the form shows the policy error and no account is created.
- AC4. Given any failed sign-up attempt, when persisted, then the password is never stored as plaintext (verified by inspecting storage).

#### REQ-AUTH-2 — Sign-in (local)
An existing user MUST be able to sign in with email + password.

- AC1. Given a user with valid credentials, when they sign in, then they receive a signed JWT and are routed to the to-dos landing.
- AC2. Given an invalid email or password, when they sign in, then the form shows a generic "incorrect email or password" error (no enumeration), and no token is issued.
- AC3. Given five consecutive failed attempts in the prior fifteen minutes for one account, when the user attempts again, then the request is rate-limited / locked out, and the event is recorded in the security audit log.

#### REQ-AUTH-3 — Sign-in (PKCE OIDC)
An existing user MUST be able to sign in via PKCE-based OIDC against the configured external identity provider, when that provider is enabled in the running environment.

- AC1. Given OIDC is enabled, when the user clicks "Sign in with SSO" and completes the IdP flow, then they are signed in to Tickbox with an equivalent authenticated session to the local flow.
- AC2. Given OIDC is disabled in this environment, then the SSO entry point is hidden in the UI.

#### REQ-AUTH-4 — Password reset
A user who knows only their email MUST be able to request a password reset link.

- AC1. Given an email is submitted on the reset screen, when the request succeeds, then the UI confirms "if that account exists, a reset link is on its way" without revealing whether an account exists.
- AC2. Given a valid, unexpired reset token, when the user submits a new password meeting policy, then the password is updated, all existing sessions are invalidated, and the user is signed in with a fresh token.
- AC3. Given an expired or already-consumed reset token, when used, then it is rejected with a clear error and an audit log entry.

#### REQ-AUTH-5 — Sign-out
A signed-in user MUST be able to sign out from the profile screen.

- AC1. Given a signed-in user on the profile screen, when they tap "Sign out", then their session token is invalidated server-side and they are routed to the sign-in screen.

#### REQ-AUTH-6 — Token validation on every protected request
The backend MUST validate JWT issuer, audience, signature, and expiration on every request to a protected endpoint.

- AC1. Given a missing, expired, or tampered token, when used against any protected endpoint, then the response is HTTP 401.
- AC2. Given a valid token, when used, then the request is authorised against the bearer's identity (no other identity is accessible).

#### REQ-AUTH-7 — Password policy
A "valid password" referenced by REQ-AUTH-1, REQ-AUTH-4, and REQ-ACCT-3 MUST meet the following policy and ONLY this policy:

- AC1. Length ≥ 12 characters.
- AC2. No further complexity rules (no required mixed case, no required digits, no required symbols, no banned-word list). Simplicity is intentional; length is the strength lever.
- AC3. Maximum length 256 characters; longer inputs MUST be rejected with HTTP 400 / inline error.

### 3.2 To-dos (the product)

Mocks: `todos.html`, `todo-detail.html`, `todos-empty.html`.

#### REQ-TODO-1 — Two-state model
A to-do MUST have exactly one of two states at any time: `Incomplete` or `Complete`. There is no other state (no archived, no in-progress, no priority levels).

- AC1. Given a newly created to-do, when persisted, then its state is `Incomplete`.
- AC2. Given a to-do, when its state is updated, then it accepts only `Complete` or `Incomplete`. Any other value is a 400.

#### REQ-TODO-2 — Create a to-do
A signed-in user MUST be able to create a new to-do with a title, and optionally notes and a due date.

- AC1. Given a user on the to-dos landing, when they tap the "New to-do" FAB, then the detail screen opens in create mode.
- AC2. Given a non-empty title, when the user saves, then a new to-do is created in `Incomplete` state and shown in the Incomplete section of the list.
- AC3. Given an empty title, when the user attempts to save, then the form shows a validation error and nothing is persisted.
- AC4. Title MUST be 1–200 characters; notes MUST be 0–2000 characters. Out-of-range values MUST be rejected with HTTP 400 / inline error.

#### REQ-TODO-3 — View the to-do list
A signed-in user MUST see only their own to-dos, grouped by state, with filter chips for All / Incomplete / Complete. The page header shows the current date in the user's local timezone for context; the list itself shows the user's to-dos in their entirety, not filtered by date.

- AC1. Given a user with at least one to-do, when they open the list, then to-dos are grouped under "Incomplete" and "Complete" headings, with a header summary "`<n>` of `<m>` complete" computed across **all** the user's to-dos (not date-scoped).
- AC2. Given the user selects a filter chip, when chosen, then the list shows only to-dos matching that filter (`All`, `Incomplete`, or `Complete`).
- AC3. Given a user signed in as account A, when they view the list, then to-dos owned by account B MUST NOT appear.
- AC4. **Ordering.** Within each section, to-dos MUST be ordered by `due_date` ascending with nulls last, then by `created_at` descending (most recently created first). The same ordering applies under each filter chip. Ordering is computed server-side and is deterministic.

#### REQ-TODO-3a — Page-header date label
The list page header MUST display the current date in the user's local timezone, formatted as `<day-name>, <day> <month-name>` (e.g., `Today, 9 May`). This is informational only — it does not filter the list.

#### REQ-TODO-4 — Edit a to-do
A signed-in user MUST be able to edit the title, notes, due date, and state of a to-do they own.

- AC1. Given a user opens a to-do they own, when they change any field and save, then the change is persisted and reflected on the list on next view.
- AC2. Given a user attempts to access or edit a to-do they do not own, when the request is made, then the response is HTTP 404 (no enumeration).

#### REQ-TODO-5 — Toggle state
A signed-in user MUST be able to toggle a to-do between `Incomplete` and `Complete` from either the list (checkbox) or the detail screen (status chip set).

- AC1. Given an `Incomplete` to-do, when the user taps its checkbox in the list, then it becomes `Complete` and moves to the Complete section.
- AC2. Given a `Complete` to-do, when the user taps its checkbox in the list, then it becomes `Incomplete` and moves to the Incomplete section.
- AC3. Toggling state on the detail screen via the chip set produces the same outcome.

#### REQ-TODO-6 — Delete a to-do
A signed-in user MUST be able to delete a to-do they own.

- AC1. Given a user on the detail screen of a to-do they own, when they tap "Delete" in the top app bar, then the to-do is removed and the list no longer shows it.
- AC2. Deletion is a hard delete; there is no undo / archive in v1.

#### REQ-TODO-7 — Empty state
When the signed-in user has no to-dos, the list MUST show the empty-state mock with a primary CTA to create one.

- AC1. Given a user with zero to-dos, when they open the list, then the empty-state screen is shown with a single primary CTA ("Add a to-do") that opens the detail screen in create mode.

#### REQ-TODO-8 — Activity strip on detail
The to-do detail screen MUST show an activity strip with the to-do's lifecycle events.

- AC1. The activity strip MUST contain at minimum a `Created` entry (timestamp the to-do was created) and, when applicable, a `Marked complete` entry (timestamp of the most recent transition to `Complete`). If the to-do is later set back to `Incomplete`, the existing `Marked complete` entry is removed.
- AC2. Timestamps MUST be displayed in the user's local timezone, formatted `<day> <month-name>, <HH>:<MM>` (e.g., `8 May, 14:02`).
- AC3. The activity strip is read-only; this requirement does NOT imply free-text comments, edit history, or any further auditing surface.

### 3.3 Account management

Mocks: `profile.html`.

#### REQ-ACCT-1 — View profile
A signed-in user MUST see their display name, email, and avatar on the profile screen.

#### REQ-ACCT-2 — Update display name and email
A signed-in user MUST be able to change their display name and request an email change.

- AC1. Given a user changes their display name, when they save, then the new name is reflected on the profile screen and in the top app bar avatar tooltip.
- AC2. Given a user requests an email change, when they save, then the system sends a verification link to the new address (delivery via the no-op logging email service in v1; the contract is preserved for a real provider). Until the new address is verified by following the link, the original email remains the sign-in identifier and the profile screen MUST show an inline banner "Pending email change to `<new-email>` — check your inbox" with a "Cancel" action. No new mock screen is required for this banner; it appears in the existing profile surface.
- AC3. Given the verification link is followed within its expiry window, when accepted, then the new email becomes the sign-in identifier and the banner is removed.

#### REQ-ACCT-3 — Change password
A signed-in user MUST be able to change their password from the profile screen, using their current password and a new password meeting policy.

- AC1. Given the current password is wrong, when submitted, then the change is rejected with a clear error and a security audit entry.
- AC2. Given the change succeeds, then all other active sessions are invalidated.

#### REQ-ACCT-4 — Delete account
A signed-in user MUST be able to delete their own account from the profile screen, with a confirmation step.

- AC1. Given the user confirms deletion, when accepted, then the account, all of its to-dos, and all sessions are removed; the user is routed to the sign-in screen.

### 3.4 Error handling

Mocks: `error.html`.

#### REQ-ERR-1 — Network / server failure surface
When the API is unreachable or returns 5xx, the UI MUST show a non-destructive error surface with a retry action and a path back to the to-do list. Outstanding user input MUST NOT be lost on retry.

#### REQ-ERR-2 — Validation errors
Backend validation failures (HTTP 400 with `ValidationProblemDetails`) MUST surface inline against the offending field on the relevant form.

## 4. Non-functional requirements

### REQ-NFR-1 — Mobile-first responsive
Every screen MUST look great and remain usable at 360 / 768 / 1440 viewport widths, matching the accepted mocks.

### REQ-NFR-2 — Material 3 visual language
Every screen MUST follow Material 3: color roles, type scale, elevation, shape, and component anatomy, matching the accepted mocks.

### REQ-NFR-3 — Password storage
Passwords MUST be stored only as salted hashes from a modern password-hashing function (Argon2id, PBKDF2 with adequate iterations, or bcrypt with adequate cost). Plaintext passwords, code verifiers, and tokens MUST NEVER be logged.

### REQ-NFR-4 — Audit log
Authentication-relevant events MUST produce a security audit log entry: failed sign-in, lockout, password change, password reset request, password reset use, account deletion.

### REQ-NFR-5 — Per-user isolation
Every API response and database query MUST be scoped to the authenticated user. Cross-user data access is a defect of the highest severity.

### REQ-NFR-6 — Client-side data hygiene
The frontend MUST NOT cache plaintext credentials or full JWTs in `localStorage`. Refresh tokens (if used) MUST be stored only in HttpOnly Secure cookies.

### REQ-NFR-7 — Accessibility
All interactive elements MUST have accessible labels. Touch targets MUST be ≥48 dp. Color contrast MUST meet WCAG 2.1 AA against the M3 light token set used in the mocks.

### REQ-NFR-8 — Build cleanliness
`dotnet build` and `ng build` MUST produce zero errors and zero new warnings. The app MUST start from a single documented run command on a fresh checkout.

## 5. Constraints (from Implementation Guidance — informative)

These are not negotiable; they constrain the implementation tasks downstream.

- Backend: Clean Architecture (Api / Application / Domain / Infrastructure), MediatR CQS, EF Core via `IAppDbContext` (no repository / unit-of-work), FluentValidation per command, ASP.NET Core controllers, MS SQL Server, one-type-per-file.
- Frontend: Angular workspace with `api`, `components`, `domain` libraries plus the main app; Angular Material 3; design tokens; BEM CSS; one component = one `.html` + one `.scss` + one `.ts` file; interface-driven service consumption (`*.service.contract.ts`).
- Testing: Playwright Page Object Model E2E tests for important flows, ATDD (test first).

## 6. Out of scope (explicit)

To preserve "radically simple", the following are NOT in v1 and MUST NOT be added without revising this document:

- Tags, projects, lists beyond a single inbox.
- Priority levels, sub-tasks, or dependencies between to-dos.
- Sharing, collaboration, or multi-user assignment.
- Recurring / repeating to-dos.
- Reminders, notifications, push, email, calendar integration.
- Attachments, comments, history beyond the simple activity strip in the detail mock.
- Bulk actions, drag-and-drop reordering.
- Offline mode beyond the "saved locally / will sync" copy in the error state (the actual sync mechanism is out of scope; copy is aspirational).
- Sending real transactional email (the verification + reset-link delivery MAY be replaced by a logging no-op service; the contract MUST remain so a real email service can be plugged in later, per implementation guidance).
- Theming beyond the M3 light tokens used in the mocks.

## 7. Acceptance gates

R1 is done when this document is committed and R2 evaluation is run. R2 is done when an evaluation pass produces zero blocking findings against this document.
