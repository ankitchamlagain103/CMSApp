# Portal account provisioning for Employees & Students (2026-07-27)

Lets HR/admission staff give an `Employee` or `Student` record a real login, on request, instead
of every hire/admission implicitly getting one. Complements the summary sections in
`employee_management_implementation_guide.md`, `student_management_implementation_guide.md`, and
`UI-Implementation-Guide.md` — this page is the shared mechanic those three point back to.

## The two flows

Every record can get a portal account two ways:

1. **At creation** — pass `registerUserAccount: true` on `POST /api/employees` or
   `POST /api/students` (plus `roleIds` for Employees — see below).
2. **Retrofit, for an existing record** — `POST /api/employees/{id}/register-account` /
   `POST /api/students/{id}/register-account`. Use this for a record created before this feature
   existed, or one whose admin skipped the checkbox at creation time.

Both paths run the exact same provisioning logic and produce an identical result — the retrofit
endpoint is not a lesser version of the create-time flag.

## Employee vs Student: who picks the role

| | Employee | Student |
|---|---|---|
| Role | Admin picks one or more (`roleIds`, same shape as `POST /api/users`'s `roleIds` — resolve via `GET /api/roles` first) | Always the fixed `Student` role, no picker |
| Required field | `email` + at least one `roleId` | `email` only |
| Create-time field | `CreateEmployeeCommand.registerUserAccount` + `roleIds` | `CreateStudentCommand.registerUserAccount` |
| Retrofit endpoint body | `{ "roleIds": ["<role-guid>", ...] }` | none |

The `Student` role starts with **zero granted permissions** (same convention as `Admin`/`User`
until someone grants specific claims via `POST /api/roles/claims`) — there is no "view my
results/schedule" module in this codebase yet for it to be granted against. This feature only
gets the login itself working; building the actual student-facing endpoints is a separate,
future piece of work.

## What happens when an account is provisioned

1. The email is checked against every existing `ApplicationUser` — if already registered, the
   whole request fails with `Conflict` and nothing is created (for the create-time flow, the
   Employee/Student row itself is never persisted either — the request fails atomically).
2. A username is derived from the email's local part, de-duplicated with a numeric suffix if
   taken (same logic Google sign-in already uses to name an account with no admin-chosen
   username).
3. The `ApplicationUser` is created **with no password** — same shape as a Google-created account.
   `EmailConfirmed` is set `true` directly (the person is already a verified record via HR/
   admission, unlike an anonymous self-registration), so there's no separate email-verification
   step to get through first.
4. The requested role(s) are assigned.
5. A password-reset token is generated immediately and emailed as a "set your password to
   activate your account" link — this reuses the existing anonymous `POST /api/auth/reset-password`
   endpoint. **There is no separate activation endpoint or flag**: the account simply has no
   usable password until that link is used, exactly like a Google-only account before it calls
   `POST /api/auth/set-password`.
6. The Employee/Student row's `userId` is set to the new account's id, and its `Phone` is copied
   onto the account's `PhoneNumber` (base Identity property) so it can be used to log in.

## Login accepts username, email, or phone number

Since a provisioned account gets a system-generated username the person never chose,
`POST /api/auth/login`'s `userName` field (name kept for backward compatibility) now resolves the
identifier against `UserName`, then `Email`, then `PhoneNumber`, in that order. Phone number has
no unique-index constraint (unlike username/email), so it's a best-effort match — the password
check right after still has to pass for that specific user, so a shared phone number can't grant
access to the wrong account. This applies to every login, not just provisioned accounts.

## Failure table

| Condition | Response code | Notes |
|---|---|---|
| `registerUserAccount: true` but `email` blank/invalid | `ValidationError` | Employee also requires non-empty `roleIds` in this case |
| Email already registered to another account | `Conflict` | Applies to both create-time and retrofit |
| Record already has a `userId` (retrofit only) | `Conflict` | "This employee/student already has a portal account." |
| Retrofit called with no email on the record | `ValidationError` | Add an email via `PUT /api/employees/{id}` or `PUT /api/students/{id}` first |
| A supplied `roleId` doesn't exist (Employee only) | `ValidationError` | Deliberately not `NotFound` — a coarser catch-all, consistent with how most other provisioning failures here are reported |

## Known gaps

- No UI concept of "portal account pending activation" — `GET /api/employees/{id}` /
  `GET /api/students/{id}` only expose `userId` (non-null once provisioned); whether the emailed
  link has actually been used yet isn't surfaced (would need `UserManager.HasPasswordAsync`, not
  wired up here).
- No resend-activation-email endpoint — if the original email is lost, the fallback today is the
  normal `POST /api/auth/forgot-password` flow (works once `EmailConfirmed` is `true`, which it is
  for every account this feature creates).
- Students get zero permissions and there is no results/schedule module yet — the account can log
  in but has nothing to see beyond whatever `DefaultEnabledMenu` already grants every authenticated
  user (e.g. `Roles/GetUserRoles`).
