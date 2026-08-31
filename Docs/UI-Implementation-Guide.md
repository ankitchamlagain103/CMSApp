# CMSApp — UI Implementation Guide

Complete API reference for frontend integration: every endpoint with its exact request and response JSON. In development, Swagger UI at `/swagger` mirrors everything here.

## Conventions (read first)

- **Every** response uses the same envelope: `{ "responseCode", "responseMessage", "data" }`. Branch on `responseCode`, not only HTTP status — e.g. a duplicate email is HTTP `400` with `responseCode: "CONFLICT"`.
- `responseCode` values: `SUCCESS`, `VALIDATION_ERROR`, `NOT_FOUND`, `CONFLICT`, `UNAUTHORIZED`, `FORBIDDEN`, `TOO_MANY_REQUESTS`, `SERVER_ERROR`. On `VALIDATION_ERROR`, `responseMessage` contains all failed rules joined into one string.
- **Rate limiting**: all `/api/auth/*` endpoints are limited per client IP (default 10 requests/60s). Exceeding it returns HTTP `429` with `responseCode: "TOO_MANY_REQUESTS"` `"Too many requests. Please try again later."` — back off and retry after a pause; don't hammer login/refresh in a loop.
- Property names are camelCase. Enums are **numbers**: `gender` — `0` Male, `1` Female, `2` Other; `userType` — `0` SuperAdmin, `1` Admin, `2` User. Dates are ISO-8601 strings.
- Send the access token as `Authorization: Bearer <token>` on everything not marked *anonymous*.
- **401** = bad/expired token or credentials → try refresh, then re-login. **403** = valid token, missing permission → access denied screen, don't retry. (SuperAdmin accounts never get 403 — they bypass permission checks entirely.)
- Refresh tokens are **single-use**: redeeming one revokes it and returns a new pair. Store both new values; never run two refresh calls concurrently.
- Abandoned requests: the server honors client-side aborts (`AbortController`) — cancelling a fetch actually stops the corresponding server work. Cancelled requests get no response.
- Unhandled server faults return HTTP `500`:

```json
{ "responseCode": "SERVER_ERROR", "responseMessage": "An unexpected error occurred. Please try again later.", "data": null }
```

---

# Auth — `/api/auth`

## POST /api/auth/login — *anonymous*

**Request**:
```json
{ "userName": "superadmin", "password": "Str0ng!Pass" }
```

`userName` (field name kept for backward compatibility) accepts a **username, email, or phone
number** (2026-07-27) — tried in that order. Added because a portal account provisioned for an
Employee/Student (`registerUserAccount`/`register-account`, see `portal_account_provisioning_implementation_guide.md`)
gets a system-generated username the person never chose, so email or phone is realistically what
they'll type. Phone number has no unique constraint (unlike username/email), so it's a best-effort
match — the password check right after still has to pass against that specific user.

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Login successful.",
  "data": {
    "userId": "7b9f3f4e-5c2a-4d1e-9f6b-2a8c4e0d1b3a",
    "userName": "superadmin",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAtUtc": "2026-07-09T11:30:00Z",
    "refreshToken": "u8Zk3vJxN2mQ...base64...==",
    "refreshTokenExpiresAtUtc": "2026-07-16T10:30:00Z",
    "roles": ["SuperAdmin"]
  }
}
```

**Failures** — `401` with `responseCode: "UNAUTHORIZED"` and one of:
- `"Invalid username or password."` (unknown user **or** wrong password — deliberately indistinguishable)
- `"Account is locked due to multiple failed login attempts. Try again later."`
- `"This account has been deactivated."`
- `"Please verify your email address before logging in."` → offer resend-verification
- `"Login is not allowed from this IP address."` (account has an IP allowlist and this network isn't on it — an admin must update it via `PUT /api/users/{id}`)
- `"Your password has expired. Please reset it before logging in."` → send to forgot-password

`400 VALIDATION_ERROR` if either field is empty.

## POST /api/auth/google — *anonymous*

Client obtains a **Google ID token** (Google Identity Services / Google Sign-In SDK, configured with the same OAuth Client ID as the backend's `Authentication:Google:ClientId`).

**Request**:
```json
{ "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6..." }
```

**Response** (`200`): identical shape to login. First Google sign-in auto-creates a passwordless account with the default `User` role (or links Google to an existing account with the same email). The account has no password until the user adds one via `set-password` (below). Full client walkthrough: `Docs/google_signin_implementation_guide.md`.

**Failures**: `401 UNAUTHORIZED` `"Invalid Google sign-in token."` or `"This account has been deactivated."`; `400 VALIDATION_ERROR` if `idToken` empty.

## POST /api/auth/refresh-token — *anonymous*

**Request**:
```json
{ "refreshToken": "u8Zk3vJxN2mQ...base64...==" }
```

**Response** (`200`): identical shape to login (`responseMessage: "Token refreshed successfully."`) — a **new** access token *and* a **new** refresh token; the presented one is now revoked.

**Failures**: `401 UNAUTHORIZED` `"Invalid or expired refresh token."` → clear stored tokens, go to login. `400 VALIDATION_ERROR` if empty.

> ⚠️ **Reuse detection (2026-07-12)**: presenting an already-rotated (revoked) refresh token is treated as token theft — the backend revokes **every** active session for that user, on all devices. This is another reason the client must never retry a refresh with an old token and never run two refresh calls concurrently: an accidental replay logs the user out everywhere.

## POST /api/auth/logout — authenticated

**Request**: no body.

**Response** (`200`):
```json
{ "responseCode": "SUCCESS", "responseMessage": "Logged out successfully.", "data": true }
```

Revokes **all** the user's refresh tokens (every device). Discard both stored tokens client-side.

## POST /api/auth/change-password — authenticated

**Request**:
```json
{ "currentPassword": "Old!Pass123", "newPassword": "New!Pass456" }
```

**Response** (`200`):
```json
{ "responseCode": "SUCCESS", "responseMessage": "Password changed successfully. Please log in again.", "data": true }
```

All sessions are revoked — route the user to login.

**Failures** (`400 VALIDATION_ERROR`): password-policy violations (below), `"New password must be different from your current password."`, or Identity's own `"Incorrect password."` when `currentPassword` is wrong.

## POST /api/auth/set-password — authenticated

For accounts created via Google sign-in, which have **no password**. Lets the signed-in user add one so they can also log in with username/password. Show this (instead of change-password) when the account is passwordless.

**Request**:
```json
{ "newPassword": "New!Pass456" }
```

**Response** (`200`):
```json
{ "responseCode": "SUCCESS", "responseMessage": "Password set successfully. You can now also log in with your username and password.", "data": true }
```

Sessions are **not** revoked — the user stays logged in (nothing pre-existing was invalidated, unlike change/reset-password).

**Failures**: `400` with `responseCode: "CONFLICT"` `"This account already has a password. Use change-password instead."` — use this to fall back to the change-password UI if you can't tell locally whether the account is passwordless; `400 VALIDATION_ERROR` for password-policy violations (same rules as registration).

## POST /api/auth/verify-email — *anonymous*

Called from the page your verification email links to (`{ClientBaseUrl}/verify-email?userId=...&token=...`). Pass the token exactly as received.

**Request**:
```json
{ "userId": "7b9f3f4e-5c2a-4d1e-9f6b-2a8c4e0d1b3a", "token": "CfDJ8P1x...longIdentityToken..." }
```

**Response** (`200`):
```json
{ "responseCode": "SUCCESS", "responseMessage": "Email verified successfully. You can now log in.", "data": true }
```

**Failures**: `404 NOT_FOUND` `"User was not found."`; `400 VALIDATION_ERROR` `"Invalid or expired verification token."` (tokens expire in 30 minutes) → offer resend.

## POST /api/auth/resend-verification-email — *anonymous*

**Request**:
```json
{ "email": "jdoe@example.com" }
```

**Response** — **always** `200` regardless of whether the account exists (anti-enumeration):
```json
{ "responseCode": "SUCCESS", "responseMessage": "If that email exists and is not yet verified, a verification link has been sent.", "data": true }
```

## POST /api/auth/forgot-password — *anonymous*

**Request**:
```json
{ "email": "jdoe@example.com" }
```

**Response** — **always** `200` (anti-enumeration):
```json
{ "responseCode": "SUCCESS", "responseMessage": "If that email exists, a password reset link has been sent.", "data": true }
```

Reset links expire in 30 minutes.

## POST /api/auth/reset-password — *anonymous*

From the reset page (`{ClientBaseUrl}/reset-password?userId=...&token=...`).

**Request**:
```json
{
  "userId": "7b9f3f4e-5c2a-4d1e-9f6b-2a8c4e0d1b3a",
  "token": "CfDJ8P1x...longIdentityToken...",
  "newPassword": "New!Pass456"
}
```

**Response** (`200`):
```json
{ "responseCode": "SUCCESS", "responseMessage": "Password reset successfully. Please log in with your new password.", "data": true }
```

All sessions are revoked.

**Failures** (`400 VALIDATION_ERROR`): `"Invalid or expired reset token."` or password-policy violations.

---

# Users — `/api/users`

`UserDto` (the `data` shape for all user endpoints):

```json
{
  "id": "7b9f3f4e-5c2a-4d1e-9f6b-2a8c4e0d1b3a",
  "userName": "jdoe",
  "email": "jdoe@example.com",
  "emailConfirmed": false,
  "firstName": "John",
  "middleName": null,
  "lastName": "Doe",
  "gender": 0,
  "userType": 2,
  "dob": "1995-04-23T00:00:00",
  "phoneCountryCode": "+1",
  "phoneNumber": "+14155552671",
  "countryIso3": "USA",
  "isTosAgreed": true,
  "isActive": true,
  "isIpRestricted": false,
  "userIpAllowed": null,
  "lastLoginTs": null,
  "lastPasswordChangedTs": "2026-07-09T10:30:00+00:00",
  "roleIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"]
}
```

`roleIds` holds the guids of the user's current roles (empty array = no roles) — resolve names via `GET /api/roles`. It's returned by **all** user endpoints (get-by-id, list, create, update), so an edit form can pre-select roles straight from `GET /api/users/{id}` without a separate lookup.

## POST /api/users — create user (**admin-gated — no longer anonymous as of 2026-07-12**)

Requires a valid JWT plus the `USER_CREATE` permission (SuperAdmin always passes). **There is no public self-registration** — do not build a register page; users are created from the admin user-management screen. An optional `roleIds` array (role guids) may be included to assign roles at creation — see `user_role_ids_implementation_guide.md`.

**Request**:
```json
{
  "userName": "jdoe",
  "email": "jdoe@example.com",
  "password": "Str0ng!Pass",
  "firstName": "John",
  "middleName": null,
  "lastName": "Doe",
  "gender": 0,
  "dob": "1995-04-23",
  "phoneCountryCode": "+1",
  "phoneNumber": "+14155552671",
  "countryIso3": "USA",
  "isTosAgreed": true
}
```

Validation to mirror client-side — **password**: 8–128 chars, ≥1 upper, ≥1 lower, ≥1 digit, ≥1 special, ≠ email/username, not a common password. **firstName/lastName**: required, ≤256, no `<`/`>` (middleName: same character rule, optional). **dob**: optional, age 13–120. **phoneNumber**: optional, E.164 (`+14155552671`). **countryIso3**: optional, exactly 3 uppercase letters. **isTosAgreed**: must be `true`. There is **no `userType` field** — creation always produces a normal user (`userType: 2`); promote afterwards via `PUT /api/users/{id}` (SuperAdmin only, see below).

**Response** (`200`, `data` = `UserDto` above):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "User created successfully. Please check your email to verify your account.",
  "data": { "id": "7b9f3f4e-...", "userName": "jdoe", "emailConfirmed": false, "userType": 2, "...": "see UserDto" }
}
```

The user **cannot log in until they verify their email**.

**Failures** (`400`): `VALIDATION_ERROR` (joined rule messages) or `CONFLICT` — `"Username 'jdoe' is already taken."` / `"Email 'jdoe@example.com' is already registered."`; `403 FORBIDDEN` when the caller lacks `USER_CREATE` (or isn't authenticated).

## GET /api/users/{id}

**Response** (`200`): `data` = `UserDto`. **Failure**: `404 NOT_FOUND` `"User with id '...' was not found."`

## GET /api/users?page=1&pageSize=20

**Response** (`200`, ordered by username):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "items": [ { "id": "7b9f3f4e-...", "userName": "jdoe", "...": "UserDto" } ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 57,
    "totalPages": 3,
    "hasPreviousPage": false,
    "hasNextPage": true
  }
}
```

## PUT /api/users/{id}

**Request** (note: `userType` **is** editable here — this is the path for promoting users, and changing it requires the **caller** to be a SuperAdmin; `userName`/`email`/`password` are deliberately absent):
```json
{
  "firstName": "John",
  "middleName": null,
  "lastName": "Doe",
  "gender": 0,
  "userType": 1,
  "dob": "1995-04-23",
  "phoneCountryCode": "+1",
  "phoneNumber": "+14155552671",
  "countryIso3": "USA",
  "isActive": true,
  "isIpRestricted": false,
  "userIpAllowed": null
}
```

`isIpRestricted`/`userIpAllowed` — per-user IP allowlist: `userIpAllowed` is a comma-separated list of full IPv4/IPv6 addresses (`"203.0.113.7,2001:db8::1"`), required non-empty when `isIpRestricted` is `true`, may be kept while `isIpRestricted` is `false` (stored but inert — lets an admin toggle the restriction without retyping the list). ✅ **Enforced as of 2026-07-12** — at login/refresh and on every authenticated request; warn the admin before they save a list that excludes the target user's current network, because it locks that user out immediately. Details in `user_ip_restriction_implementation_guide.md`.

**Response** (`200`): `data` = updated `UserDto`, `"User updated successfully."`

**Failures**: `404 NOT_FOUND`; `400 VALIDATION_ERROR` (including `"At least one allowed IP is required when IP restriction is enabled."` and `"UserIpAllowed must be a comma-separated list of valid IPv4/IPv6 addresses."`); `400` with `responseCode: "FORBIDDEN"` — `"A SuperAdmin account cannot be deactivated."` when setting `isActive: false` on a SuperAdmin, or `"Only a SuperAdmin can change a user's type."` when a non-SuperAdmin caller sends a different `userType` than the user currently has (send the unchanged value to avoid this).

## DELETE /api/users/{id}

**Response** (`200`):
```json
{ "responseCode": "SUCCESS", "responseMessage": "User deleted successfully.", "data": true }
```

Soft delete — the user vanishes from lists and can't log in, but the row survives.

**Failures**: `404 NOT_FOUND`; `400` with `responseCode: "FORBIDDEN"` `"A SuperAdmin account cannot be deleted."`

---

# Roles — `/api/roles`

`RoleDto`:
```json
{ "id": "c2222222-0000-0000-0000-000000000001", "name": "Editor", "description": "Can edit content", "userType": "ADMIN" }
```
`userType` (**2026-08-24**) is `Domain.Constants.MenuAudience` (`ADMIN`/`USER`/`BOTH`), **not**
the `ApplicationUser.UserType` enum — it's a UI filtering hint for which menus make sense to
offer in this role's permission-tree editor (an `ADMIN`-tagged role should only be offered
`ADMIN`/`BOTH` menus; a `USER`-tagged role — a Teacher/Student self-service role — only
`USER`/`BOTH`). It is **not** enforced by `AuthorizedAction` and does not affect what actually
lands in a signed-in user's nav tree (that's still `Menu.MenuFor` + account-linkage audience, see
`GET /api/roles/user-menus` below) — purely a frontend picker filter. Full reference:
`role_menu_claims_sync_implementation_guide.md`.

## POST /api/roles

**Request** (`name` required ≤256; `description` optional ≤500; `userType` optional, one of
`ADMIN`/`USER`/`BOTH`, defaults to `BOTH` when omitted):
```json
{ "name": "Editor", "description": "Can edit content", "userType": "ADMIN" }
```

**Response** (`200`):
```json
{ "responseCode": "SUCCESS", "responseMessage": "Role created successfully.", "data": { "id": "c2222222-...", "name": "Editor", "description": "Can edit content", "userType": "ADMIN" } }
```

**Failures** (`400`): `CONFLICT` `"Role 'Editor' already exists."` or `VALIDATION_ERROR`.

## GET /api/roles/{id}

**Response** (`200`): `data` = `RoleDto`. **Failure**: `404 NOT_FOUND` `"Role with id '...' was not found."`

## GET /api/roles?page=1&pageSize=20

**Response** (`200`): same pagination wrapper as users, `items` = `RoleDto[]`, ordered by name.

## PUT /api/roles/{id}

**Request**: same body as create. Renaming re-checks uniqueness. `userType` omitted/empty leaves
the role's current value unchanged (same "null = unchanged" convention as `UpdateUserCommand.RoleIds`);
supply it explicitly to change it.

**Response** (`200`): `data` = updated `RoleDto`, `"Role updated successfully."` **Failures**: `404 NOT_FOUND`; `400 CONFLICT` on duplicate name; `400 VALIDATION_ERROR`.

## DELETE /api/roles/{id}

**Response** (`200`): `{ ..., "responseMessage": "Role deleted successfully.", "data": true }` — **hard** delete (unlike users). **Failure**: `404 NOT_FOUND`.

## GET /api/roles/user-menus — the current user's permitted menu tree

Identifies the caller from the JWT (no `userId` parameter). Returns every menu granted to any of the caller's roles, plus the parent `SUB_MENU`/`MAIN_MENU` nodes above them, assembled into a tree. This is the endpoint to call after login to render navigation. Any authenticated user may call it (listed in `DefaultEnabledMenu` — no permission row needed); a user whose roles have no grants gets an empty `data` array, not an error.

**2026-08-07: audience filtering, reversed 2026-08-24.** A granted menu only appears in this tree if its `menuFor` (`ADMIN`/`USER`/`BOTH`) matches the caller's own **audience** — `BOTH` always passes. Audience mirrors the account's own `userType` directly: `SuperAdmin`/`Admin` accounts resolve `ADMIN` audience, everything else (including an Employee-linked self-service login, e.g. a Teacher, and every Student-linked login) resolves `USER` audience. **This was originally the opposite** — an Employee-linked account used to resolve `ADMIN` audience — but that was corrected: Teacher and Student self-service logins are both plain `UserType.User` accounts and belong on the same `USER` side of the split. `MY_WORKSPACE` (Employee/Teacher self-service) and `STUDENT_PORTAL` are both tagged `menuFor: USER` to match. This does **not** change API authorization — `AuthorizedAction` is unaffected; this only controls what shows up in the nav tree. Full reference: `role_menu_claims_sync_implementation_guide.md` (Round 2).

**Response** (`200`, array of root menus, not paginated):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": [
    {
      "id": 1,
      "code": "USER_MANAGEMENT",
      "displayName": "User Management",
      "url": null,
      "icon": null,
      "menuType": "MAIN_MENU",
      "parentId": null,
      "order": 1,
      "isHidden": false,
      "hasChildren": true,
      "children": [
        {
          "id": 4,
          "code": "USER_LIST",
          "displayName": "View Users",
          "url": null,
          "icon": null,
          "menuType": "PERMISSION",
          "parentId": 1,
          "order": 1,
          "isHidden": true,
          "hasChildren": false,
          "children": []
        }
      ]
    }
  ]
}
```

`PERMISSION` entries come back with `isHidden: true` — use them for capability checks (show/hide buttons), and render only non-hidden nodes as nav items. Menus are ordered by `order` at every level.

**Failures**: `401 UNAUTHORIZED` (no authenticated user); `404 NOT_FOUND` `"User was not found."`

## POST /api/roles/users — assign a role to a user

**Request**: `{ "userId": "c1111111-0000-0000-0000-000000000002", "roleId": "c2222222-0000-0000-0000-000000000002" }`

**Response** (`200`): `{ ..., "responseMessage": "Role assigned to user successfully.", "data": true }` **Failures**: `404 NOT_FOUND` (user or role missing); `400 CONFLICT` `"This user is already in the role."`; `400 VALIDATION_ERROR`; `400 FORBIDDEN` `"You cannot assign this role because it grants permissions you do not hold yourself: <names>."` — **2026-08-07**, see the privilege-escalation guard note below. SuperAdmin is exempt.

## DELETE /api/roles/users/{userId}/{roleId} — remove a role from a user

**Response** (`200`): `{ ..., "responseMessage": "Role removed from user successfully.", "data": true }` **Failures**: `404 NOT_FOUND` (user or role missing, or `"This user is not in the role."`).

## GET /api/roles/{roleId}/claims — a role's existing menu grants

**Response** (`200`, plain array, not paginated):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": [
    { "id": 12, "roleId": "c2222222-0000-0000-0000-000000000001", "menuId": 5, "menuCode": "USER_LIST", "menuDisplayName": "View Users" }
  ]
}
```

**Failure**: `404 NOT_FOUND` `"Role with id '...' was not found."` Use this to pre-check the checkboxes in the role-permission editor described below.

## POST /api/roles/claims — grant a menu permission to a role

**Request**:
```json
{ "roleId": "c2222222-0000-0000-0000-000000000001", "menuId": 5 }
```

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Menu assigned to role successfully.",
  "data": {
    "id": 12,
    "roleId": "c2222222-0000-0000-0000-000000000001",
    "menuId": 5,
    "menuCode": "USER_LIST",
    "menuDisplayName": "View Users"
  }
}
```

**Failures**: `404 NOT_FOUND` (role or menu missing); `400 CONFLICT` `"This menu is already assigned to the role."`; `400 VALIDATION_ERROR`; `400 FORBIDDEN` `"You cannot grant '<menu display name>' because you do not hold it yourself."` — **2026-08-07**, see the privilege-escalation guard note below. SuperAdmin is exempt.

## DELETE /api/roles/{roleId}/claims/{menuId}

**Response** (`200`): `{ ..., "responseMessage": "Menu removed from role successfully.", "data": true }` **Failure**: `404 NOT_FOUND` `"This menu is not assigned to the role."`

## PUT /api/roles/{roleId}/claims — sync a role's menu claims (2026-08-24)

**Use this for a permission-tree "Save" button, not the two granular endpoints above.** Send the
complete set of currently-checked menu ids on every save — the backend diffs it against what the
role currently has and adds/removes accordingly, in one atomic call. This exists specifically to
fix a reported bug where unchecking a menu and saving never removed its claim, because the
frontend save action only called `PUT /api/roles/{id}` (name/description) and skipped firing the
per-menu `DELETE` calls for unchecked rows.

**Request**:
```json
{ "menuIds": [4, 5, 12, 18] }
```
`[]` clears every claim from the role. Duplicates are de-duplicated automatically.

**Response** (`200`, `data` = the role's full claim list after the sync, same shape as `GET
/api/roles/{roleId}/claims`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Role menu claims synced successfully.",
  "data": [
    { "id": 12, "roleId": "c2222222-0000-0000-0000-000000000001", "menuId": 4, "menuCode": "USER_LIST", "menuDisplayName": "View Users" }
  ]
}
```

**Failures**: `404 NOT_FOUND` (role missing, or one or more `menuIds` don't exist); `400
VALIDATION_ERROR` (`menuIds` null or contains a value `<= 0`); `400 FORBIDDEN`
`"You cannot grant the following menus because you do not hold them yourself: <names>."` — same
privilege-escalation guard as `POST /api/roles/claims`, checked only against newly-added menus.
SuperAdmin is exempt. Full reference: `role_menu_claims_sync_implementation_guide.md`.

## Teacher data scoping — My Students & My Marks Entry (2026-08-07)

New self-service, data-filtered routes so a teacher sees only their own students and only the
subjects they teach: `GET /api/students/me` / `GET /api/students/me/{id}` (scoped to the caller's
own `TeacherAssignment.ClassSectionId` values), `GET /api/exams/me`,
`GET /api/studentexammarks/me/roster`, `GET/POST /api/studentexammarks/me`,
`PUT /api/studentexammarks/me/{id}`, `POST /api/studentexammarks/me/bulk` (all scoped to the
caller's `TeacherAssignment.ClassSubjectId` values, enforced on the write path too — not just
hidden from the read/roster view). `DefaultEnabledMenu`-gated like every other "Me" route, no
permission row. Admin routes are unchanged. Full reference:
`teacher_data_scoping_implementation_guide.md`.

## Per-user menu overrides (2026-08-07)

`GET /api/users/{id}/claims`, `POST /api/users/claims` (`{ userId, menuId }`), `DELETE
/api/users/{id}/claims/{menuId}` — grant/revoke a menu directly to one user, additive on top of
whatever their roles already grant (never a substitute). Same privilege-escalation guard as
`POST /api/roles/claims` (caller can only hand out a menu they hold themselves), same response
shapes as the role-claims endpoints above with `userId` in place of `roleId`. Permissions
`USER_CLAIM_LIST`/`USER_CLAIM_ASSIGN`/`USER_CLAIM_REMOVE`. Full reference:
`role_privilege_escalation_guard_implementation_guide.md` (Part 4).

## Privilege-escalation guard on role/claim assignment (2026-08-07)

`AssignMenuToRoleAsync` and `AssignRoleToUserAsync` now check the **caller's own** effective
granted menus before letting a grant through — a non-SuperAdmin caller can only hand out a
permission (or a role carrying permissions) they already hold themselves. Previously the only
gate was the caller's own `AssignMenuToRole`/`AssignRoleToUser` permission row, which meant an
Admin holding just that one grant could hand any role (or any user) every permission in the
system, including ones they didn't have. Full reference:
`role_privilege_escalation_guard_implementation_guide.md`.

A typical role-permission editor: list all `PERMISSION`-type menus from `/api/menus`, render checkboxes per role, call these two endpoints on toggle.

---

# Menus — `/api/menus`

`MenuDto`:
```json
{
  "id": 5,
  "code": "USER_LIST",
  "displayName": "View Users",
  "url": null,
  "icon": null,
  "menuType": "PERMISSION",
  "controller": "Users",
  "action": "GetUsers",
  "parentId": 1,
  "menuFor": "ADMIN",
  "order": 1,
  "isHidden": true
}
```

- `menuType`: `"MAIN_MENU"` (top level, no parent) → `"SUB_MENU"` (parent must be a MAIN_MENU) → `"PERMISSION"` (leaf carrying the `controller`/`action` the backend authorizes against; parent may be MAIN_MENU **or** SUB_MENU).
- `menuFor`: `"ADMIN"` | `"USER"` | `"BOTH"` — which audience's navigation the item belongs to.
- Navigation tree rendering: take non-hidden `MAIN_MENU`/`SUB_MENU` rows, group by `parentId`, sort by `order`.

## POST /api/menus

**Request** (creating a permission leaf under main menu `1`; for a `MAIN_MENU` send `"parentId": null` and typically `"isHidden": false`):
```json
{
  "code": "USER_EXPORT",
  "displayName": "Export Users",
  "url": null,
  "icon": null,
  "menuType": "PERMISSION",
  "controller": "Users",
  "action": "ExportUsers",
  "parentId": 1,
  "menuFor": "ADMIN",
  "order": 5,
  "isHidden": true
}
```

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Menu created successfully.",
  "data": { "id": 16, "code": "USER_EXPORT", "displayName": "Export Users", "menuType": "PERMISSION", "controller": "Users", "action": "ExportUsers", "parentId": 1, "menuFor": "ADMIN", "order": 5, "isHidden": true, "url": null, "icon": null }
}
```

**Failures** (`400`):
- `CONFLICT` `"Menu code 'USER_EXPORT' is already in use (possibly by a soft-deleted menu)."` — soft-deleted menus keep their code reserved.
- `VALIDATION_ERROR` — `"MenuType must be one of: MAIN_MENU, SUB_MENU, PERMISSION."`, `"MenuFor must be one of: ADMIN, USER, BOTH."`, or hierarchy violations: `"A MAIN_MENU cannot have a parent menu."`, `"A SUB_MENU must have a parent menu."`, `"A SUB_MENU's parent must be a MAIN_MENU."`, `"A PERMISSION's parent must be a MAIN_MENU or SUB_MENU."`, `"Parent menu with id '99' was not found."`

## GET /api/menus/{id}

**Response** (`200`): `data` = `MenuDto`. **Failure**: `404 NOT_FOUND` `"Menu with id '...' was not found."`

## GET /api/menus?page=1&pageSize=20&menuType=MAIN_MENU&menuFor=ADMIN&search=user&parentId=2&isHidden=false

`menuType` and `menuFor` are **optional, independent filters** — omit either (or both) to not filter on it. Allowed values are the same enums as create: `menuType` = `MAIN_MENU` | `SUB_MENU` | `PERMISSION`, `menuFor` = `ADMIN` | `USER` | `BOTH` (exact match — a menu created as `BOTH` is only returned by `menuFor=BOTH`, not by `menuFor=ADMIN`). Results are sorted by `order`. **New (2026-07-15)**: `search` (matches `code`/`displayName`, case-insensitive), `parentId` (exact match, direct children only), `isHidden` (exact match) — see `filters_update.md`.

**Response** (`200`): same pagination wrapper, `items` = `MenuDto[]` (`totalCount` reflects the filtered count). **Failure** (`400 VALIDATION_ERROR`): an unknown filter value — `"MenuType must be one of: MAIN_MENU, SUB_MENU, PERMISSION."` / `"MenuFor must be one of: ADMIN, USER, BOTH."`

## PUT /api/menus/{id}

**Request**: same body as create. The same hierarchy rules are re-validated, plus `"A menu cannot be its own parent."`; renaming `code` re-checks uniqueness.

**Response** (`200`): `data` = updated `MenuDto`, `"Menu updated successfully."` **Failures**: `404 NOT_FOUND`; `400 CONFLICT` on duplicate code; `400 VALIDATION_ERROR` (same messages as create).

## DELETE /api/menus/{id}

Only allowed once the menu has no children — delete bottom-up (permissions before their sub/main menus). **Soft** delete (unlike roles): the row is hidden, not removed, and its code stays reserved — recreating a menu with a deleted menu's `code` will 409.

**Response** (`200`): `{ ..., "responseMessage": "Menu deleted successfully.", "data": true }` **Failures**: `404 NOT_FOUND`; `400 CONFLICT` `"Menu with id '...' still has child menus. Delete its children first."`

---

# Configs (dropdown catalog) — `/api/configs`

**Every dropdown in the UI is populated from these tables** — don't hardcode option lists in the frontend. A `ConfigType` names a dropdown (identified by its numeric `typeCode`); its `Config` rows are the options.

**Use this catalog's dropdown endpoint only to populate a `<select>` in a create/edit form (2026-08-05).** Every read endpoint elsewhere in this API that returns a Config-backed code (`gradeCode`, `sectionCode`, `subjectCode`, `jobPositionCode`, `componentCode`, ...) now also returns that code's resolved label in a sibling `...Label` field on the same response — do not call `GET /api/configs/dropdown/{typeCode}` a second time just to map a code you already have to its label. Full reference, including the complete list of DTOs that gained a label field: `Docs/config_label_resolution_implementation_guide.md`.

`ConfigDto`:
```json
{
  "id": "a1111111-0000-0000-0000-000000000001",
  "typeCode": 100,
  "code": "DRAFT",
  "label": "Draft",
  "order": 1,
  "additionalValue1": null,
  "additionalValue2": null,
  "additionalValue3": null
}
```

## GET /api/configs/dropdown/{typeCode}?parentCode=&search= — options for one dropdown

Available to **any authenticated user** (no permission grant needed). Returns the **common dropdown shape** (`DropdownItemDto`) — every dropdown endpoint in the system uses this same shape, so one frontend select component can bind them all: render `label`, submit `value`.

Two optional query params (2026-07-24), reusable by any catalog: `parentCode` narrows to options
whose `additionalValue1` equals it (cascading dropdowns — e.g. District options for one Province);
`search` is a case-insensitive substring match against `label` (a searchable lookup for a catalog
with too many options for a plain `<select>`). Both independent; omitting both is the plain
unfiltered call.

**Response** (`200`): `data` = `DropdownItemDto[]`, sorted by `order`. An unknown `typeCode` returns an empty array, not an error.
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": [
    { "value": "DRAFT", "label": "Draft", "order": 1, "additionalValue1": null, "additionalValue2": null, "additionalValue3": null }
  ]
}
```

## GET /api/configs/types/dropdown?search= — dropdown of the ConfigType "tables" themselves

Gated by the hidden `CONFIG_TYPE_DROPDOWN` permission (not in `DefaultEnabledMenu` — grant it to
whichever role needs a "pick a catalog" selector, e.g. an admin screen letting a user choose which
`ConfigType` to manage). One level up from the endpoint above, which lists the options *within* one
type — this one lists every type. Same common `DropdownItemDto` shape: `value` = `typeCode` as a
string, `label` = the type's `Name`, `additionalValue1` = its `Description`, sorted by `Name`.
`search` (optional) is a case-insensitive substring match against `Name`.

```json
{
  "responseCode": "SUCCESS",
  "data": [
    { "value": "1002", "label": "Section", "order": 0, "additionalValue1": null, "additionalValue2": null, "additionalValue3": null }
  ]
}
```

## POST /api/configs/types — create a dropdown type (admin)

**Request**: `{ "typeCode": 100, "name": "PostStatus", "description": "Statuses a blog post can be in" }`

**Response** (`200`): `data` = `ConfigTypeDto`, `"Config type created successfully."` **Failures** (`400`): `CONFLICT` `"Config type with type code '100' already exists."`; `VALIDATION_ERROR` (typeCode > 0, name required ≤ 100 chars, description ≤ 500).

## GET /api/configs/types?page=1&pageSize=20

**Response** (`200`): same pagination wrapper, `items` = `ConfigTypeDto[]`.

## GET /api/configs/types/{id}

**Response** (`200`): `data` = `ConfigTypeDto`. **Failure**: `404 NOT_FOUND` `"Config type with id '...' was not found."`

## GET /api/configs/{id}

**Response** (`200`): `data` = `ConfigDto` (the full admin shape shown at the top of this section, not the dropdown shape). **Failure**: `404 NOT_FOUND` `"Config with id '...' was not found."`

## PUT /api/configs/types/{id} — rename a dropdown type (admin)

`typeCode` cannot be changed — it's the key the options and the frontend reference.

**Request**: `{ "name": "PostStatus", "description": "Statuses a blog post can be in" }`

**Response** (`200`): `data` = updated `ConfigTypeDto`, `"Config type updated successfully."` **Failures**: `404 NOT_FOUND` `"Config type with id '...' was not found."`; `400 VALIDATION_ERROR`.

## DELETE /api/configs/types/{id} — delete a dropdown type (admin)

Only allowed once the type has no options left.

**Response** (`200`): `{ ..., "responseMessage": "Config type deleted successfully.", "data": true }` — **hard** delete. **Failures**: `404 NOT_FOUND`; `400 CONFLICT` `"Config type with type code '...' still has configs. Delete its configs first."`

## POST /api/configs — create an option under a type (admin)

**Request**: `{ "typeCode": 100, "code": "DRAFT", "label": "Draft", "order": 1, "additionalValue1": null, "additionalValue2": null, "additionalValue3": null }`

**Response** (`200`): `data` = `ConfigDto`, `"Config created successfully."` **Failures**: `404 NOT_FOUND` `"Config type with type code '...' was not found."`; `400 CONFLICT` `"Config code '...' already exists for type code '...'."`; `400 VALIDATION_ERROR`.

## PUT /api/configs/{id} — update an option (admin)

`typeCode` cannot be changed (move an option to another dropdown by deleting and re-creating it). Renaming `code` re-checks uniqueness within the option's type.

**Request**: `{ "code": "DRAFT", "label": "Draft (unpublished)", "order": 1, "additionalValue1": null, "additionalValue2": null, "additionalValue3": null }`

**Response** (`200`): `data` = updated `ConfigDto`, `"Config updated successfully."` **Failures**: `404 NOT_FOUND` `"Config with id '...' was not found."`; `400 CONFLICT` on duplicate code; `400 VALIDATION_ERROR`.

## DELETE /api/configs/{id} — delete an option (admin)

**Response** (`200`): `{ ..., "responseMessage": "Config deleted successfully.", "data": true }` — **hard** delete. **Failure**: `404 NOT_FOUND` `"Config with id '...' was not found."` **No reference check** — deleting (or renaming the `code` of) an option that some entity's plain-string field already stores does not cascade or block; see `config_catalog_implementation_guide.md` for the full explanation and UI guidance.

---

# App Configs (application settings) — `/api/appconfigs`

Application-wide settings as key/value rows: app name, theme mode, primary/secondary colors, logo URL, and any future setting the UI should change without a redeploy. Full request/response detail, suggested `configParam` conventions, and a frontend bootstrap snippet live in **`app_config_implementation_guide.md`** — summary below.

`AppConfigDto` (admin shape):
```json
{
  "id": "b2222222-0000-0000-0000-000000000001",
  "configParam": "PRIMARY_COLOR",
  "configValue": "#4F46E5",
  "configGroup": "THEME",
  "isEnable": true
}
```

## GET /api/appconfigs/public — *anonymous*

The UI bootstrap call — no token needed, safe to call on the login page. Returns **only `isEnable: true` rows** as a trimmed shape (`configParam`/`configValue`/`configGroup`, no `id`/`isEnable`), sorted by group then param. Never put secrets in an enabled app config — this endpoint is world-readable.

**Response** (`200`): `data` = `[ { "configParam": "APP_NAME", "configValue": "My Blog CMS", "configGroup": "GENERAL" }, ... ]`

## POST /api/appconfigs — create a setting (admin)

**Request**: `{ "configParam": "PRIMARY_COLOR", "configValue": "#4F46E5", "configGroup": "THEME", "isEnable": true }` (`isEnable` defaults to `true`)

**Response** (`200`): `data` = `AppConfigDto`, `"App config created successfully."` **Failures** (`400`): `CONFLICT` `"App config with param '...' already exists."` (`configParam` is globally unique); `VALIDATION_ERROR` (param required ≤ 256, value required ≤ 555, group **optional** ≤ 256 — may be omitted/null; a group-less row simply never appears in any `group/{configGroup}` lookup).

## GET /api/appconfigs?page=1&pageSize=20

**Response** (`200`): pagination wrapper, `items` = `AppConfigDto[]` — enabled and disabled rows (admin view).

## GET /api/appconfigs/{id}

**Response** (`200`): `data` = `AppConfigDto`. **Failure**: `404 NOT_FOUND` `"App config with id '...' was not found."`

## GET /api/appconfigs/group/{configGroup}

All settings in one group (for a tabbed settings screen), not paged, sorted by `configParam`, disabled rows included. Exact case-sensitive group match; unknown group returns an empty array.

**Response** (`200`): `data` = `AppConfigDto[]`.

## PUT /api/appconfigs/{id} — update a setting (admin)

Full replace — send all four fields. Renaming `configParam` re-checks uniqueness.

**Request**: `{ "configParam": "PRIMARY_COLOR", "configValue": "#16A34A", "configGroup": "THEME", "isEnable": true }`

**Response** (`200`): `data` = updated `AppConfigDto`, `"App config updated successfully."` **Failures**: `404 NOT_FOUND`; `400 CONFLICT` on duplicate param; `400 VALIDATION_ERROR`.

## DELETE /api/appconfigs/{id} — delete a setting (admin)

**Response** (`200`): `{ ..., "responseMessage": "App config deleted successfully.", "data": true }` — **hard** delete, the param is immediately reusable. **Failure**: `404 NOT_FOUND`.

---

# Dashboard — `/api/dashboard`

## GET /api/dashboard/summary — headline stat tiles

**Response** (`200`): `data` =
```json
{
  "totalUserCount": 42,
  "activeUserCount": 39,
  "distinctErrorCount": 4,
  "totalErrorCount": 31
}
```

`totalUserCount` excludes soft-deleted users; `activeUserCount` is the subset with `isActive: true`. The error counts are the same numbers as `/error-logs/summary` (distinct error kinds vs. total occurrences).

## GET /api/dashboard/error-logs?page=1&pageSize=20

Server-side error log, deduplicated: the same error occurring repeatedly is **one row with an incrementing `errorCount`**, not many rows. Ordered by `lastOccurredTs` descending.

**Response** (`200`): pagination wrapper, `items` =
```json
{
  "id": 1,
  "exceptionType": "Npgsql.PostgresException",
  "message": "connection refused",
  "path": "/api/users",
  "errorCount": 17,
  "firstOccurredTs": "2026-07-09T08:00:00+00:00",
  "lastOccurredTs": "2026-07-09T11:42:00+00:00"
}
```

## GET /api/dashboard/error-logs/summary

**Response** (`200`): `data` = `{ "distinctErrorCount": 4, "totalErrorCount": 31 }` — distinct error kinds vs. total occurrences across all of them.

## GET /api/dashboard/access-logs?page=1&pageSize=20&userId={guid}

Audit trail of who executed which critical action (only actions listed in the server's `CriticalChanges` configuration are recorded). `userId` is optional — omit it for all users. Ordered newest first.

**Response** (`200`): pagination wrapper, `items` =
```json
{
  "id": 1,
  "userId": "c1111111-0000-0000-0000-000000000001",
  "userName": "superadmin",
  "controller": "Roles",
  "action": "DeleteRole",
  "httpMethod": "DELETE",
  "url": "/api/roles/c2222222-0000-0000-0000-000000000003",
  "ipAddress": "203.0.113.7",
  "accessedTs": "2026-07-09T11:42:00+00:00"
}
```

## GET /api/dashboard/enrollment-stats — student/enrollment widget

**Response** (`200`): `data` =
```json
{
  "totalStudents": 100,
  "totalActiveEnrollments": 96,
  "enrollmentsByStatus": [
    { "status": 1, "count": 96 },
    { "status": 2, "count": 2 },
    { "status": 3, "count": 1 },
    { "status": 4, "count": 1 }
  ],
  "enrollmentsByGrade": [
    { "gradeCode": "NUR", "gradeLabel": "Nursery", "count": 20 },
    { "gradeCode": "LKG", "gradeLabel": "LKG", "count": 18 }
  ]
}
```
`status` is `EnrollmentStatus` (1 Enrolled / 2 Transferred / 3 Withdrawn / 4 Completed). `enrollmentsByGrade` is scoped to the **current** academic year's `Enrolled`-status rows only, one entry per seeded Grade config option ordered by its `order` (zero-count grades still appear). Empty array if no academic year is marked current.

## GET /api/dashboard/teachers?take=5 — teacher list widget

**Response** (`200`): `data` =
```json
{
  "totalTeachers": 20,
  "activeTeachers": 19,
  "recentTeachers": [
    { "id": "...", "employeeCode": "EMP2026020", "firstName": "Anita", "middleName": null, "lastName": "Sharma", "status": 1, "joiningDate": "2026-04-01T00:00:00" }
  ]
}
```
`recentTeachers` is the `take` most recently created teachers (default 5), newest first, joined through the teacher's owning `Employee` row (2026-07-15 Employee split — `employeeCode` was `employeeNo` before the split). `status` is `EmploymentStatus` (1 Active / 2 OnLeave / 3 Suspended / 4 Resigned / 5 Terminated / 6 Retired) — `activeTeachers`/the `TeachersByStatus` bar graph below still treat it as a 2-bucket Active/"anything else" split for display purposes.

## GET /api/dashboard/users?take=5 — user list widget

**Response** (`200`): `data` =
```json
{
  "totalUsers": 42,
  "activeUsers": 39,
  "recentUsers": [
    { "id": "...", "userName": "jdoe", "email": "jdoe@example.com", "firstName": "John", "lastName": "Doe", "userType": 2, "isActive": true, "createdTs": "2026-07-13T09:00:00+00:00" }
  ]
}
```
`recentUsers` is the `take` most recently created users (default 5), newest first, soft-deleted excluded. `userType` is `UserType` (0 SuperAdmin / 1 Admin / 2 User).

## GET /api/dashboard/bar-graph?metric={metric} — chart data

`metric` is required, one of `EnrollmentsByGrade`, `EnrollmentsByMonth`, `StudentsByStatus`, `TeachersByStatus`. Unknown/missing value → `400 VALIDATION_ERROR`. Shape is deliberately generic (`labels` + one or more named `series`) so one chart component on the frontend can render any of them, and new metrics can be added later without a new response shape.

**Response** (`200`): `data` =
```json
{
  "metric": "EnrollmentsByMonth",
  "title": "New Enrollments (Last 6 Months)",
  "labels": ["Feb 2026", "Mar 2026", "Apr 2026", "May 2026", "Jun 2026", "Jul 2026"],
  "series": [
    { "name": "New Enrollments", "data": [3, 5, 2, 8, 6, 4] }
  ]
}
```
- `EnrollmentsByGrade` — active enrollments in the current academic year, one bar per Grade config option (same data as `enrollment-stats.enrollmentsByGrade`, chart-ready).
- `EnrollmentsByMonth` — count of enrollments by `enrollmentDate`, last 6 calendar months including the current one, zero-filled for months with no activity.
- `StudentsByStatus` / `TeachersByStatus` — two bars, `["Active", "Inactive"]` (`TeachersByStatus` buckets the 6-value `EmploymentStatus` on the owning `Employee` into Active vs. anything else).

## GET /api/dashboard/current-academic-year — current year widget

**Response** (`200`): `data` =
```json
{
  "id": "...",
  "code": "2026",
  "name": "Academic Year 2026",
  "startDate": "2026-01-01T00:00:00",
  "endDate": "2026-12-31T00:00:00",
  "totalClasses": 13,
  "totalSections": 26,
  "totalActiveEnrollments": 96
}
```
**Failure**: `404 NOT_FOUND` if no `AcademicYear` currently has `isCurrent: true`.

## GET /api/dashboard/quick-menus?take=8 — quick menu suggestions

Shortcut links to menu catalog `SUB_MENU` rows explicitly curated as quick links
(`Menu.isQuickLink`, 2026-07-28 — an admin toggles this via `POST/PUT /api/menus`) that the
**current logged-in user** actually has permission to open — resolved the same way as
`GET /api/roles/user-menus`, filtered to visible + `isQuickLink` rows. Zero grants returns an
empty list, not a 403. Seeded quick links: Students, Fee Generation, Config Types, Users,
Employees, Academic Years.

**Response** (`200`): `data` =
```json
[
  { "id": 12, "code": "STUDENT_LIST", "displayName": "Students", "url": "/apps/student/list", "icon": "icons.student", "order": 1 },
  { "id": 20, "code": "TEACHER_LIST", "displayName": "Teachers", "url": "/apps/teacher/list", "icon": "icons.teacher", "order": 2 }
]
```
**Failure**: `401 UNAUTHORIZED` if not authenticated; `404 NOT_FOUND` if the caller's user row can't be found.

## GET /api/dashboard/accounts-summary?take=5 / GET /api/dashboard/hr-summary?take=5 (2026-07-28)

Two persona-oriented composite widgets, same one-call shape as `summary` above: **Accounts**
(fee collection totals, outstanding dues, invoice-status breakdown, current payroll run, recent
payments) and **HR** (headcount by status/category, pending leave/loan approval counts, recent
hires, upcoming birthdays/work anniversaries). Permission-gated like every other dashboard
endpoint — grant `DASHBOARD_ACCOUNTS_SUMMARY`/`DASHBOARD_HR_SUMMARY` to whatever role a school
uses for its finance/HR staff (there's no hardcoded "Accounts"/"HR" role). Full field reference:
`Docs/dashboard_updated_file.md`.

**Logs moved out of Dashboard**: System Access Logs and Error Logs (`GET /api/dashboard/error-logs`,
`/error-logs/summary`, `/access-logs` — routes unchanged) now have their own top-level `Logs`
sidebar menu instead of being reachable only from the Dashboard page.

Full request/response reference and design notes for the Dashboard feature: `Docs/dashboard_updated_file.md`.

## GET /api/dashboard/widgets?take=5 — generic dashboard widget registry (2026-08-07)

The role-agnostic alternative to calling `summary`/`accounts-summary`/`hr-summary`/
`employees/me/dashboard` individually: returns exactly the widgets the caller's own role grants,
each resolved from `Menu.isDashboardWidget` rows in their `GET /api/roles/user-menus` tree.
Permission `DASHBOARD_WIDGETS`. Currently registered widget codes: `DASHBOARD_SUMMARY`,
`DASHBOARD_ACCOUNTS_SUMMARY`, `DASHBOARD_HR_SUMMARY`, `MY_DASHBOARD`.

**Response** (`200`): `data` = array of `{ widgetCode, displayName, success, message, data }` —
`data` is whichever DTO that widget's dedicated endpoint already returns (polymorphic per
`widgetCode`; the frontend must know each shape ahead of time). `success: false` means that
widget's underlying call failed for this caller (e.g. `MY_DASHBOARD` for an account with no
linked `Employee` record) — still included in the array, filter on `success` before rendering. A
`Menu` flagged `isDashboardWidget` with no registered provider yet is silently omitted, not an
error. The dedicated per-widget endpoints above are unchanged and still work — this is an
additional surface, not a replacement. `Menu.isDashboardWidget` (settable via `POST`/`PUT
/api/menus`, same "admin curates via a flag" shape as `isQuickLink`) is what makes a menu eligible
in the first place. Full reference: `Docs/role_privilege_escalation_guard_implementation_guide.md`
(Part 3).

---

# Student Management (2026-07-12, restructured 2026-07-13)

Full sub-system for academic years, classes (with sections), subjects, teachers, guardians, students, and enrollments — **complete reference with request/response JSON, invariants, and UI flows in `student_management_implementation_guide.md`**, and the 2026-07-13 breaking changes (class/section split, enrollment-per-year guard, guardian onboarding) detailed in `class_section_structure_implementation_guide.md`; this is the orientation summary:

- **Dropdown catalogs, not tables**: Grade (`typeCode 1001`), Section (`1002`), Subject (`1003`), Guardian Relationship (`1004`), Employee Qualification (`1005`, renamed 2026-07-23 from "Teacher Qualification" — generic to every staff member now, see "Employee Management & Payroll" below) live in the Config catalog and are referenced everywhere by their `code` string. Populate via `GET /api/configs/dropdown/{typeCode}`; relationship + qualification options are seeded, grade/section/subject options must be created by the admin before classes can exist. An unknown code → `400 VALIDATION_ERROR`.
- **Endpoints** (all permission-gated, standard envelope):
  - `/api/academicyears` — CRUD; unique `code`; one `isCurrent` year (setting it demotes the others); code immutable on update. `POST /{id}/clone-structure` copies another year's classes/sections/subject mappings into it (existing grades skipped) — the one-click new-year setup.
  - `/api/academicclasses` — CRUD + `/{id}/sections` (add/list/update/remove sections; capacity lives on the section, `0` = unlimited) + `/{id}/subjects` (assign/list/remove class subjects — **shared by every section of the class**; an *optional* subject may instead be scoped to a single section via `classSectionId`, and `GET …/subjects?classSectionId=…` returns one section's effective list); a class = year+grade (unique pair, immutable after create), with its `sections` nested in every response.
  - **2026-08-06: the standalone `Teacher` entity/`TeachersController` were removed entirely.** A
    teacher is now just an `Employee` categorized by `employeeCategoryCode`/`jobPositionCode` — no
    separate CRUD, no separate profile. Assignments live at `/api/employees/{id}/assignments`
    (link to a classSubject, optionally narrowed to one `classSectionId` — null = all sections;
    `isClassTeacher` requires a section, at most one class teacher per **section**); the employee
    detail response adds `serviceHistory` (assignments with academic years, oldest first) and
    three optional teaching fields (`teachingLicenseNo`/`experienceYears`/`specialization`,
    settable on any employee via the normal `POST`/`PUT /api/employees` calls — no eligibility
    gate, no separate "add teacher profile" step). Qualifications/documents/salary/tax/payslip/
    loans/ID-card-preview all live at `/api/employees/{id}/...` too (they were already Employee
    routes even before this round). Full reference:
    `employee_teaching_profile_and_assignments_implementation_guide.md`. See "Employee Management
    & Payroll" below.
  - `/api/guardians` — CRUD; standalone records shared across students.
  - `/api/students` — CRUD (`admissionNo` optional on create — blank = auto-generated `ADM{year}{seq}`; unique/immutable, `search` filter); `POST` accepts an optional `guardians` array (existing `guardianId` or inline new-guardian fields, `relationshipCode`, at most one `isPrimary`) so onboarding captures guardians in one call; `PUT` takes the same `guardians` list with three-way semantics (null = unchanged, `[]` = unlink all, list = replace-sync); the detail response returns `guardians` inline plus `currentEnrollment` (current year/grade/section/roll + subjects studying, each subject with its `teacherName`) and `enrollmentHistory` (all enrollments, oldest year first — see `profile_history_and_documents_implementation_guide.md`); `/{id}/guardians` still links/unlinks individually (one primary per student, auto-demoted); `/{id}/documents` mirrors employee documents (multipart upload, catalog 1007, list/download/delete).
  - `/api/enrollments` — CRUD against a **section** (`classSectionId`; unique student+section; **one active enrollment per student per academic year**; per-section capacity and roll-number uniqueness; student/section immutable — move = status `2` Transferred + new enrollment) + `/{id}/subjects/{classSubjectId}` electives (non-mandatory subjects of the enrollment's class only). Rows flatten grade/section/year ids and codes.
- **Enums**: `status` on records — `1` Active / `2` Inactive; enrollment `status` — `1` Enrolled / `2` Transferred / `3` Withdrawn / `4` Completed.
- **No logins for students/teachers** — they're records only; account linkage is a later phase.
- Deletes of the main records are **soft** (codes/pairs stay reserved → clean `409` on re-create); child links (class subjects, assignments, guardian links, electives) are hard deletes, refused with `409` while dependents exist.
- **Grading metadata (2026-07-15)**: `ClassSubject` (and `POST`/new `PUT /api/academicclasses/{id}/subjects/{classSubjectId}`) gained optional `creditHours`/`fullMarks`/`passMarks`/`theoryMarks`/`practicalMarks` — see `student_management_implementation_guide.md`.
- **Class display ordering (2026-07-31)**: `AcademicClass`/`AcademicClassDto` gained `order` (int, default `0`, editable on both `POST`/`PUT /api/academicclasses[/{id}]`) — pure UI sort key, no uniqueness enforced; `GET /api/academicclasses` now sorts by `order` then `gradeCode` instead of `gradeCode` alone. `POST /api/academicyears/{id}/clone-structure` carries the source class's `order` onto the clone.

---

# Fee, Discount & Scholarship Management (2026-07-15, redesigned three times same day)

Full reference in `fee_management_implementation_guide.md`; orientation summary: `/api/feestructures` is a **header per class** (`academicClassId` unique) owning a child `items` array — `POST /api/feestructures` creates the header **and every submitted item in one call** (`{ academicClassId, items: [{ feeCategoryCode, amount, frequencyType, isOptional, isRefundable }, ...] }`), fixing the earlier one-call-per-category flow. `feeCategoryCode` must be a known option in the `FeeCategory` Config catalog (`1010`, all 11 "permitted" categories seeded: Tuition/Annual/Admission/Deposit/Examination/Computer/SpecialTraining/Hostel/Meal/Transportation/EducationalTour) — this is the **authoritative whitelist**, not a free-text field (a same-day detour briefly allowed free-text `name` here; reverted the same day). A school needing a category outside the 11 seeded ones adds it via plain `POST /api/configs` first, then references its code. `/api/feestructures/{id}/items` (`POST`/`PUT /{itemId}`/`DELETE /{itemId}`) manages items one at a time after the initial bulk create — `feeCategoryCode` is immutable once set. `/api/enrollments/{id}/fee-selections/{feeStructureItemId}` — how an enrollment opts into an optional item (by its id — a class can only charge a given category once, but the selection still keys off the item's own id). `/api/enrollments/{id}/discounts` and `/{id}/scholarships` — awards (percentage or fixed amount) against a specific enrollment, unaffected by any of the fee-structure redesigns; discount/scholarship "reason" is a Config code (catalog `1008`/`1009`, admin-extensible — this is the configurable eligibility-criteria mechanism, e.g. class topper, exam merit, social category, sibling) that can carry a **global default rate**; omit `valueType`/`value` on the award to use it, or supply both for an individual override. `GET /api/enrollments/{id}/fee-structure` composes all of this into one priced view for the student detail page (fee items + discounts + scholarships + a frequency-grouped summary). `GET /api/enrollments/scholarships/summary` (and the discount equivalent) report student counts per type. Discounts/scholarships are soft-deleted (financial-audit records); fee structure items and fee-selections are hard-deleted, refused with `409` while an enrollment still references an item.

---

# Employee Management & Component-Based Payroll (2026-07-15, redesigned same day from teacher-only)

Full reference in `employee_management_implementation_guide.md`; orientation summary: every staff member (teacher, principal, accountant, receptionist, librarian, IT officer, driver, security guard, office assistant, cleaner, office help) is an `Employee` (`/api/employees` — CRUD, `employeeCode` optional/auto-generated `EMP{year}{seq}`, filters by category/position/status/gender/date-range/search/phone/qualification). `employeeCategoryCode`/`jobPositionCode` are Config codes (catalog `1011`/`1012`); `employmentStatus` is a 6-value enum (Active/OnLeave/Suspended/Resigned/Terminated/Retired). **2026-08-06: the standalone `Teacher` entity/`TeachersController` were removed entirely** — `teachingLicenseNo`/`experienceYears`/`specialization` are now plain optional fields directly on `EmployeeDto`/`CreateEmployeeCommand`/`UpdateEmployeeCommand`, settable on any employee via the normal Create/Update call (no more `POST /api/teachers`, no more `POST /api/employees/{id}/teacher-profile` eligibility-gated promotion step). `EmployeeDto.isTeachingStaff` (read-only, derived from category/position) replaces the old `hasTeacherProfile`. Full reference: `employee_teaching_profile_and_assignments_implementation_guide.md`.

**"Accounts and Codes" (2026-07-23)**: `EmployeeDto`/`CreateEmployeeCommand`/`UpdateEmployeeCommand` gained five optional statutory/scheme identifier fields — `panNumber`, `providentFundNumber`, `ssfNumber`, `citNumber`, `gratuityNumber` (all free-form strings, ≤50 chars, no format enforced — the numbering schemes aren't standardized enough across employers to validate a shape). Distinct from the existing `bankName`/`bankAccountNumber` (payment routing, not a statutory identifier).

**Qualifications and Documents are generic to every employee, not teacher-specific (2026-07-23)** — moved off `Teacher` entirely: `POST/DELETE/GET /api/employees/{id}/qualifications` (catalog `1005`, renamed "Employee Qualification") and `POST/GET /api/employees/{id}/documents` + `GET .../documents/{documentId}/download` + `DELETE .../documents/{documentId}` (multipart upload, PDF/JPG/PNG ≤10 MB, catalog `1006`, optional `validUntil` expiry). Every consumer (teacher or otherwise) uses this same Employees route — no teacher-specific alias ever existed for these two. Full reference: `employee_documents_and_qualifications_implementation_guide.md` (supersedes the retired `teacher_documents_implementation_guide.md`).

**Self-service upload + HR verification (2026-08-07)**: both `EmployeeDocumentDto`/`EmployeeQualificationDto` gained `verificationStatus` (`1` Pending/`2` Approved/`3` Rejected)/`verificationRemarks`/`verifiedTs`/`verifiedBy`. New no-permission "Me" routes — `POST/GET /api/employees/me/qualifications`, `POST/GET /api/employees/me/documents`, `GET /api/employees/me/documents/{documentId}/download` — let an employee submit their own record, which starts `Pending`; a record entered through the existing admin `{id}`-scoped route is still auto-`Approved` (an already-authorized staff action doesn't need re-review). `POST /api/employees/{id}/documents/{documentId}/verify|reject` and the `qualifications` equivalent (permission `EMPLOYEE_DOCUMENT_VERIFY`/`REJECT`, `EMPLOYEE_QUALIFICATION_VERIFY`/`REJECT` — grant to whichever role a school treats as "HR", no hardcoded role) decide a `Pending` record, one-shot (a second decision 409s), optional `remarks`, and raise a `Notification` to the employee either way. Full reference: `employee_documents_and_qualifications_implementation_guide.md`'s "Self-service upload + HR verification" section.

**Compensation plan** — `/api/employees/{id}/salaries`: one row per salary revision, each holding named **components** (income — `componentCode` from catalog `1013`, e.g. `BASIC`/`SSF_CONTRIBUTION`/allowances; `valueType` Fixed or Percentage-of-`BASIC`; `frequencyType` Monthly/Annual/OneTime; `isTaxable`/`isRetirementContribution` flags), **deductions** (catalog `1014`, same shape minus `isTaxable`), and **insurance premiums** (catalog `1015`: Life/Health/Housing, each with a configured tax-deduction cap). The plain `GET .../salaries/tax-calculation` computation described below was removed entirely on 2026-07-23 (2026-08-06 update: the `GET /api/teachers/{id}/salaries/tax-calculation` alias that used to survive this removal is also gone now, along with the rest of `TeachersController`) — get the same `taxCalculation` block from `GET .../salaries/tax-planning?fiscalYearId=` instead (see the Investment & Tax Planning entry below). The computation itself: gross annual taxable income → Nepal's retirement-fund "least of three" exemption (actual contributions vs. ⅓ of gross vs. the fiscal year's configured cap) → capped insurance deduction → the existing progressive `TaxSlab` bracket walker → annual/monthly tax and net pay.

`/api/fiscalyears` — a payroll-specific year concept (separate from `AcademicYear`, since Nepal's government fiscal year doesn't align with the school's academic calendar), with nested `/{id}/taxslabs` (progressive Individual/Couple tax brackets, `taxRate` as a fraction) and a `retirementExemptionCapAmount` field (the configurable "C" in the exemption rule). Full fiscal-year/tax-slab reference stays in `payroll_implementation_guide.md`.

**The seeded `FY-SAMPLE` fiscal year/slabs/retirement cap and the seeded insurance-type caps are illustrative placeholders** — verify against the current government of Nepal budget before relying on them for real payroll.

---

# Document Preview (payslip / fee receipt / ID cards) (2026-07-15)

Full reference in `document_preview_implementation_guide.md`; orientation summary: an admin edits an HTML template per document type via `/api/documenttemplates` (`templateType` unique: `1` Payslip / `2` FeeReceipt / `3` StudentIdCard / `4` TeacherIdCard) containing `{{Token}}` placeholders; `GET /api/documenttemplates/placeholders/{templateType}` returns the backend-authoritative list of tokens each type supports. Four preview endpoints compute the real data and return the fully-substituted HTML string, ready to display/print — no PDF generation, the frontend prints via the browser:

- `GET /api/employees/{id}/salaries/payslip-preview?fiscalYearId=` — always the employee's **latest** salary revision.
- `GET /api/enrollments/{id}/fee-receipt-preview` — same composed data as `GET /api/enrollments/{id}/fee-structure`.
- `GET /api/students/{id}/id-card-preview` and `GET /api/employees/{id}/id-card-preview`.

All five return `CommonResponse<{ templateType, html }>`; `404` if no template is configured for that type yet (a default is seeded on first boot for every type). No photo/image support today — `StudentDto`/`TeacherDto` have no photo field.

---

# Pay & Taxes (2026-07-15)

Full reference in `pay_and_taxes_implementation_guide.md`; orientation summary: three additions on top of the existing compensation-plan endpoints above, all on `Employees` (no `Teachers` alias exists anymore, see `employee_teaching_profile_and_assignments_implementation_guide.md`). `GET /api/employees/{id}/salaries/tax-calculation/monthly?fiscalYearId=` returns the same annual `taxCalculation` plus `months`: 12 fiscal-month rows (`{ monthIndex, monthName, periodStartDate, periodEndDate, monthDays, incomeLines, deductionLines, monthGrossIncome, monthTax, monthNet }`) — fiscal-month boundaries are an **approximation** (`FiscalYear.startDate`..`endDate` split into 12 equal Gregorian segments, labeled Shrawan..Ashad) and `monthTax` is a **flat** `annualTax / 12` every row, not a cumulative rest-of-year re-projection. `GET /api/employees/{id}/payslips?fiscalYearId=` lists only fiscal months whose pay period has already started (`PayslipSummaryDto[]`, `payDays`/`upl` simplified — no attendance module exists); `GET /api/employees/{id}/payslips/{fiscalYearId}/{monthIndex}` returns the structured line-item detail behind it (`PayslipDetailDto`) — a separate, non-HTML path from the existing `.../payslip-preview`. `POST /api/employees/{id}/loans` / `GET .../loans` / `POST .../loans/{loanId}/approve|reject|cancel` manage a request → approve/reject/cancel workflow (`EmployeeLoanDto`, `LoanStatus` 1–5); repayment progress (`amountRepaid`/`remainingBalance`/`isFullyRepaid`) is computed from `startDate`/`emiAmount`/`principalAmount` against today, not stored, and an `Approved` loan's EMI is automatically folded into the Payslip/Tax-Details deduction lines for any month on/after `startDate` — no separate "activate deduction" step. **`dbo.employee_loans` needs a migration that doesn't exist yet** — every `/loans` endpoint 500s until it's applied.

---

# Setup module, Fee Generation & Payroll Runs (2026-07-16)

Full reference in `setup_fee_payroll_redesign_implementation_guide.md` (and the architecture blueprint in `setup_fee_payroll_redesign_implementation_plan.md`); orientation summary:

- **Navigation**: new `SETUP` main menu now parents Academic Years, Classes, Fee Structures, Fiscal Years, and the new Fee Rules list; `ACADEMIC_MANAGEMENT` and `TEACHER_MANAGEMENT` mains are retired (teacher permissions/aliases live on under `EMPLOYEE_LIST`; teacher UI folds into the Employee profile, Compensation Plan tab always last). `FEE_MANAGEMENT`/`PAYROLL_MANAGEMENT` keep only the transactional pages below. The nav tree from `GET /api/roles/user-menus` re-shapes automatically; role grants survive.
- **Fee rules** (`/api/feerules`, Setup): configurable payment-time discounts — advance-months ("pay X months together") and early-payment ("pay N days before due date"), percentage or fixed, class/category-scoped, priority + combinability.
- **Fee generation** (`/api/feeinvoices`): `POST /generate` creates one Draft invoice per Enrolled enrollment per billing month — scoped by `academicClassId` (one grade, all sections) and/or `classSectionId` (2026-07-17; always send the class when the picker has one). Annual fees charge in full on the enrollment's first invoice unless the fee-structure item sets `installmentCount` (admin-configured split, no more automatic "1/5" installments); one-time charges on the first invoice only; discounts/scholarships/monthly adjustments folded in as lines. Admin edits Draft lines, then `POST /finalize` locks them. Statuses Draft→Generated→Pending(overdue)→PartiallyPaid→Paid / Cancelled. `GET /statement/{enrollmentId}` is the parent-facing dues view; `GET /account-statement/{enrollmentId}` is the ledger-style Statement of Account (invoice debits, payment credits, running balance, `closingBalance` = live pending amount); `GET /students?search=` finds a student (name/admission no/email) and returns their `enrollmentId` + outstanding balance; the invoice list also takes `search`. Pre-generation per-month overrides via `/api/feeinvoices/adjustments` (catalog 1017, one student/one month, optional category-scoped `feeCategoryCode`) or `POST /adjustments/bulk` (2026-07-17, same shape stamped onto every Enrolled enrollment in a year/class/section scope in one call — the "Education Tour Fee for all of Grade 9" case). The Fee Generation page now also hosts a **Fee Payments tab** (2026-07-17) — `FEE_PAYMENT_LIST` is no longer a separate sidebar menu, same API, folded navigation. **Annual fee "pay in full" (2026-07-17)**: `POST .../lines/{lineId}/settle-annual-in-full` on a Draft invoice's Annual-installment line bills the item's true remaining balance in one shot; the installment engine is remaining-balance-driven (reads actual earlier-invoice line amounts, not a schedule index), so later months automatically stop billing an item once it's fully settled — no separate flag. Full fixes reference: `fee_module_fixes_implementation_guide.md` (round 1), `fee_advance_payment_and_ux_implementation_guide.md` (round 2), `fee_advance_billing_and_annual_settlement_implementation_guide.md` (round 3).
- **Fee payments** (`/api/feepayments`): preview-then-confirm — `POST /preview` returns the FIFO allocation plan (oldest month first) plus earned rule discounts and the exact collectable amount; `POST /` records the payment (receipt `RCP{year}{seq}`); `POST /{id}/void` reverses; `GET /{id}/receipt` renders the printable PaymentReceipt document template (redesigned 2026-07-17 with a school-header band from `AppConfig` `APP_NAME`/`SCHOOL_ADDRESS`/`SCHOOL_PHONE`) with every allocated invoice's Sr.No-led line details; the list takes `search` (name/admission no/email/receipt no). **Advance payment (2026-07-17)**: when the tendered `amount` exceeds what's currently outstanding, the service bills ahead — creates and finalizes the next consecutive months' invoices (same line composition as generation, up to a 12-month cap) so a Fee Rule like "pay 3 months together for Rs 5000 off" (`AdvanceMonthsDiscount`) has real fully-settled invoices to evaluate against; set `allowAdvanceBilling: false` to keep the old strict behavior. Response gains `monthsBilledInAdvance` and each allocation gets `isNewlyGenerated`. Regular generation automatically skips any month a payment already billed ahead. **`GET /advance-quote?enrollmentId=&monthsToPay=`** (2026-07-17, read-only) answers "how much for X months" so the Collect Payment form can auto-fill Amount before the cashier hits Preview/Confirm.
- **Optional fees, editable anytime**: `POST /api/enrollments` (onboarding) and `PUT /api/enrollments/{id}` (2026-07-17, the student-profile edit) both take `optionalFeeStructureItemIds` — checkboxes (transportation, hostel, …) rendered from the class fee structure's `isOptional` items, three-way semantics on update (null=unchanged/[]=clear/list=replace-sync, same as `UpdateUserCommand.RoleIds`). Selections also directly editable via `/api/enrollments/{id}/fee-selections`.
- **Fee summary fix (2026-07-17)**: `GET /api/enrollments/{id}/fee-structure`'s `summary.monthlyRecurringTotal` now folds in the per-month share of any Annual item with `installmentCount >= 2` (new `summary.annualInstallmentMonthlyShare` breaks it out) — previously it silently excluded a genuinely-monthly-billed Annual Fee's share, understating the true monthly cost. Discounts/scholarships now reduce against this combined total, matching what a real invoice actually discounts. Statement of Account is also reachable from Student Management now, not just the Fee Generation page.
- **Salary adjustments** (`/api/employees/{id}/adjustments`, catalog 1016): pre-run monthly overrides — unpaid-leave day counts, late fines, bonuses, incentives; Pending until a payroll run consumes them.
- **Payroll runs** (`/api/payrollruns`): `POST` snapshots every payable employee's effective compensation plan + TDS + due loan EMIs + Pending adjustments into Draft slips; review/edit Draft slip lines; `approve` (locks, records approver) → `mark-paid`. One live run per fiscal month; cancel re-pends adjustments. **`POST /{id}/refresh`** (2026-07-18) rebuilds a Draft run's slips in place from the *current* configuration (plan edits, slab fixes, new adjustments/loans) — Manual lines and individually-cancelled slips are preserved; see the Payroll fixes section below.
- **Payslip endpoints** (`/api/employees/{id}/payslips*` + teacher aliases) gained `isProjection`: `false` = served from a persisted run slip (real payDays/UPL). **2026-07-21: `isProjection: true` no longer occurs on these two endpoints** — a month without an Approved/Paid `SalarySlip` (no run, or still Draft) returns nothing (list) / `404` (detail) instead of a live projection; the field stays on the DTO for shape stability. Use the Tax Details tab (`.../salaries/tax-calculation/monthly`) for a forward-looking estimate instead. Same date: every payroll/tax amount (`MonthlyTax`, `GrossMonthly`/`NetMonthly`, annual-frequency monthly splits, percentage-of-Basic resolutions, retirement exemption, insurance-premium deduction) is now rounded to 2 decimal places at the point it's calculated, fixing long repeating-decimal values (e.g. `39633.334166666666666666666667`) that could previously reach the API response.
- **Common Salary Components (2026-07-21)**: the Compensation Plan's earnings dropdown (Config catalog 1013) gained `HOUSE_RENT_ALLOWANCE`/`MEDICAL_ALLOWANCE`/`OVERTIME`/`BONUS` — the standard Nepali payslip component set (`BASIC`, `DEARNESS_ALLOWANCE`, `HOUSE_RENT_ALLOWANCE`, `TRAVEL_ALLOWANCE`, `MEDICAL_ALLOWANCE`, `FESTIVAL_BONUS`, `OVERTIME`, `BONUS`) is now fully seeded, so admins no longer need to freehand these under `OTHER_ALLOWANCE`. The deductions dropdown (catalog 1014: `SSF_DEDUCTION`, `CIT_DEDUCTION`, `LOAN`, `ADVANCE`, `OTHER`) already covered the requested set — no change there. `Gross − SSF/PF − TDS − other deductions = Net` was already exactly how the payslip/payroll-run math worked; nothing changed there.
- **`GET /api/employees/{id}/salary-forecast?fiscalYearId=`** (+ `Teachers` alias, 2026-07-21, permission `EMPLOYEE_SALARY_FORECAST`/`TEACHER_SALARY_FORECAST`): a forward-looking "next month's estimated pay" built off the employee's current compensation plan — unlike the Payslip endpoints, it needs no Approved/Paid payroll run to exist. Returns income/deduction lines (including that month's TDS share and any due loan EMI), `grossSalary`, `totalDeductions`, `netSalary`. `404` if today is in the fiscal year's last month (pass the next fiscal year's id). Full shape in `pay_and_taxes_implementation_guide.md`.
- **`GET /api/employees/{id}/salaries/tax-planning?fiscalYearId=`** (+ `Teachers` alias, 2026-07-21, permission `EMPLOYEE_TAX_PLANNING`/`TEACHER_TAX_PLANNING`): the single composite response for the Investment & Tax Planning tab — income lines (every earning component, taxable or not, with `valueType`/`isTaxable`), `totalAnnualIncome`, the retirement-fund `a`/`b`/`c`/`exemptionApplied` breakdown, insurance premium lines + the capped total, `assessmentType`, and the full `taxCalculation` (monthly/annual tax, slab breakdown). One call instead of assembling it from the tax-calculation endpoint, salary history, and the fiscal year record. Full field reference, failure table, and a complete worked JSON example (matching real screenshot numbers) in `Docs/investment_and_tax_planning_implementation_guide.md`. Note `valueType` serializes as the raw `AwardValueType` int (`1` = Percentage, `2` = FixedAmount) — no string enum converter is registered anywhere in this API.
- **SSF Social Security Tax waiver + corrected "contribute more" figure (2026-07-22, applies everywhere `TaxCalculator` runs — tax-planning, tax-calculation, monthly breakdown, payslips, payroll runs, salary calculator)**: an employee with an active `SSF_CONTRIBUTION` component or `SSF_DEDUCTION` deduction now has the **first tax slab's rate waived entirely** (0 tax on that bracket, not just a reduced taxable base) — `taxCalculation.isSsfExemptionApplied` (bool) and `taxCalculation.breakdown[].isSsfExempted` (per-row) surface this. Previously every SSF-contributing employee's tax was overstated by the full first-bracket amount. Also new: `retirementFund.additionalContributionAvailable` is the correct "contribute NPR X more to save more tax" figure (`max(0, min(b, c) - a)`) — **bind this instead of computing `c - a` client-side**, which ignores the `b` (1/3-of-taxable-income) cap and overstates how much more actually helps.
- **Children's Education deduction + generalized per-line deduction breakdown (2026-07-22, same `TaxCalculator` reach as above)**: new `EDUCATION` insurance-type option (add via the existing `POST .../insurance-premiums` with `annualPremiumAmount` = the actual annual education expense) — only 25% of it counts before its NPR 25,000 cap, per Nepal's "25% of annual education expenses, max NPR 25,000" rule. `taxCalculation.insuranceDeductionLines[]` (and the tax-planning tab's `insuranceLines[]`, same shape) now carry the full per-type breakdown: `eligiblePercentage`, `capAmount`, `deductedAmount`, and `additionalAmountAvailable` (the "spend NPR X more on this to save more tax" figure, correctly divided back through the eligible percentage — **bind it instead of computing `cap - actual` client-side**, which is wrong for Education). Housing insurance's cap also corrected `25,000 → 10,000` to match the official FY 2083/84 rate; Life (40,000) and Health/Medical (20,000) were already correct.
- **Compensation basis (Net/Gross/CTC) on the Employee "Add Revision" flow (2026-07-22, `Docs/compensation_basis_ui_implementation_guide.md`)**: no new endpoint — the existing `POST /api/salarycalculator` (preview) + `POST /api/salarycalculator/assign` (commit, same permission/date-conflict rules as the manual `POST /api/employees/{id}/salaries`) should become the **default** way to add a compensation revision from the Employee/Teacher profile, not just the standalone Salary Calculator page. Root cause of "Gross Monthly > CTC"-style confusion: the raw manual entry form has no basis concept, so nothing guarantees `ctc >= grossPayment` the way the calculator does by construction (`ctc = grossPayment + ssfEmployerContribution`). Separately, `grossMonthly` on the Tax Planning/Tax Calculation tabs is a **different concept** from the calculator's `grossPayment` — it's the *taxable* annual gross ÷ 12 (includes the employer SSF contribution and smears in one-time bonuses), much closer numerically to `ctc` than to `grossPayment`. Don't conflate the two fields; see the guide's terminology table.
- **`SSF_CONTRIBUTION` data-entry correction + tax-slab boundary fix (2026-07-22, verified against a real HRMS payslip)**: `SSF_CONTRIBUTION` (the employer's SSF share, a salary *component*) must be entered with `isTaxable: true` — it's real assessable income under Nepal law, and the tax relief for it comes entirely through the retirement-fund exemption, not through excluding it from gross. Marking it non-taxable (a data mistake, not a code default — the salary calculator's suggestions already set this correctly) silently understates tax; check this first if a payslip's tax looks too low relative to gross. Separately, `PayrollSeeder`'s `2084/85` fiscal year's tax-slab boundaries were fixed from a `+1`-gap convention (`1 / 1,000,001 / ...`) to contiguous boundaries (`0 / 1,000,000 / ...`) matching `FY-SAMPLE`; the old convention silently dropped 1 rupee of taxable income per bracket transition.
- **`NetMonthly` bug fixed (2026-07-22, real `TaxCalculator` bug, not a data/UI issue)**: `GET .../salaries/tax-calculation` and `.../salaries/tax-planning` previously computed `netMonthly` as `grossMonthly - monthlyTax` only — it never subtracted the employee's own salary deductions (`SSF_DEDUCTION`, `CIT_DEDUCTION`, loans), so it overstated take-home pay by exactly (annual deductions ÷ 12) whenever a salary had any deduction line. New fields on `taxCalculation`: `cashDeductionsAnnual` (every real deduction, annualized, plus every retirement-flagged *component*'s annualized amount — the employer SSF share, which is taxable income but never reaches the employee) and `netAnnualIncome` (`grossAnnualIncome - cashDeductionsAnnual - annualTax`). `netMonthly` is now `netAnnualIncome / 12`. If your numbers previously "matched" using the old formula, re-verify — a plan with any deduction line will now show a lower (correct) `netMonthly`.
- **"SSF Contribution" no longer looks duplicated on a Payroll Run's Slip Lines (2026-07-22)**: the employer-share offsetting deduction line (see the employer-share payslip offset rule) now renders as `"SSF Contribution (Employer Share - Fund Remittance)"` instead of reusing the exact same label as its paired earning line — same amounts, same `componentCode`, just distinguishable text. Also worth knowing: a Payroll Run slip's `NetPay` for the specific month containing a `FESTIVAL_BONUS`'s `EffectiveFromDate` will be **much higher** than `EmployeeTaxCalculationDto.NetMonthly` (the smoothed annual average) — the bonus lands in full, in cash, in that one real month, while the "monthly" figure on Payslip/Tax Planning spreads it evenly across all 12. Both are correct; don't treat the gap as a bug when a sizeable one-time bonus is in play.
- **Catalog-locked percentage components/deductions (2026-07-22)**: a `SalaryComponentType`/`DeductionType` (catalog 1013/1014) option can now be locked to a fixed percentage-of-another-component via `additionalValue1: "PERCENTAGE"` (`additionalValue2` = the rate, `additionalValue3` = the base component's code, e.g. `"BASIC"`) — `SSF_CONTRIBUTION` (20% of Basic) and `SSF_DEDUCTION` (11% of Basic) are seeded this way. `POST /api/employees/{id}/salaries` and `POST .../salaries/{salaryId}/components|deductions` now **reject** a submission for a locked code unless it's `valueType: Percentage` with the exact locked rate (`400 VALIDATION_ERROR`, message names the required rate/base) — this is what prevents an admin from ever hand-entering SSF at the wrong rate (e.g. a combined 31% instead of the employer's 20%) again. Every other component/deduction code is unaffected (unlocked = free-form, same as always). **No "update line" endpoint exists** (add + remove only) — an already-saved wrong line has to be removed and re-added, it isn't auto-corrected. **A pre-existing catalog database needs a manual `PUT /api/configs/{id}`** on its `SSF_CONTRIBUTION`/`SSF_DEDUCTION` rows to pick up the lock (the seeder is create-if-missing, so it won't retrofit an already-seeded row). Full UI-entry guidance (dropdown-driven form behavior, the "+ Add SSF" quick-add button, the "Live Gross" preview algorithm) is in the new `Docs/salary_structure_and_slip_adjustments_implementation_guide.md`.
- **Payroll Run Slip Lines now recalculate tax and sync into the compensation plan (2026-07-22)**: adding/editing/removing a manual Earning/Deduction line on a Draft slip (`POST`/`PUT`/`DELETE .../slips/{slipId}/lines...`) re-runs the tax calculation and rewrites the slip's `TDS` line (previously frozen at generation time, so `netPay` silently ignored the change — this is the actual fix). A manual line that names a real catalog code also upserts into the employee's real `EmployeeSalaryComponent`/`EmployeeSalaryDeduction` rows (add to an existing Fixed-amount line, or insert a new `Fixed`/`OneTime` one) — the "Code" field in the add-row stops being cosmetic: picking a code now makes the addition permanent on the compensation plan, not just this slip. See the same new guide above for the full behavior, including the best-effort (not exact) reversal on edit/remove and why a brand-new insert defaults to `OneTime` frequency.
- **"Refresh not working" root-caused and fixed, same day**: a synced manual line (above) was staying labeled `Manual` even after being written into the real structure, so a subsequent Refresh regenerated a second, duplicate `SalaryStructure` line for the same code — doubling the amount. Fixed: a successfully-synced line is now labeled `SalaryStructure` immediately, so Refresh correctly replaces it with the one true regenerated line. If your run's numbers looked doubled after adding a manual line and refreshing, re-test — should now match.
- **Individual (per-slip) Approve and Regenerate (2026-07-22, new)**: `POST .../slips/{slipId}/approve` locks one slip independently of the whole run (finer-grained alongside the existing bulk "Approve Run"); `POST .../slips/{slipId}/regenerate` fully rebuilds one slip from the current configuration and flips it back to Draft — works on a Cancelled slip (un-cancel and rebuild) or an already-Draft one (refresh just this employee). Deliberately **not** automatic during a whole-run Refresh (which still leaves an individually-cancelled slip cancelled, since it may be a real leaver) — Regenerate is the explicit, admin-opted-into way to revive one specific slip. Add per-row "Approve"/"Regenerate" buttons to the Salary Slips table alongside the existing per-row Cancel and view icon. New permissions: `SALARY_SLIP_APPROVE`, `SALARY_SLIP_REGENERATE`.
- **`GET /api/employees/{id}/salaries/annual-forecast?fiscalYearId=`** (+ `Teachers` alias, new, `Docs/salary_annual_forecast_implementation_guide.md`): "salary receipt history" — a 12-month grid (rows = income + retirement-fund line items, columns = fiscal months) flagging each month `Actual` (a real Approved/Paid payslip exists — real figures from that month's own snapshotted salary) or `Forecast` (projected from the current plan). Retirement-fund a/b/c/min rows are populated only for Actual months, matching the reference HRMS screenshot exactly. New permissions: `EMPLOYEE_SALARY_ANNUAL_FORECAST`, `TEACHER_SALARY_ANNUAL_FORECAST`. Fresh databases get the fix automatically; an already-seeded fiscal year needs its tax slabs corrected via `PUT /api/fiscalyears/{id}/taxslabs` (each slab's `minAmount` should equal the previous slab's `maxAmount`, first slab `minAmount = 0`).
- **Composite catalog format + unified Add Salary Line endpoint (2026-07-23)**: `additionalValue1` on catalogs 1013/1014/1016 is now `"CALCULATE_TYPE|TYPE|FREQUENCY"` (e.g. `"ADDITION|PERCENTAGE|MONTHLY"`) — replaces the separate `"PERCENTAGE"`/`"FIXED"` (1013/1014) and `"EARNING"`/`"DEDUCTION"` (1016) single-value conventions in one move; a blank/unparseable value still means "no rule, free-form entry." New: a locked `FREQUENCY` segment is enforced the same way the percentage lock already was (submit a mismatched `frequencyType`, get `400 VALIDATION_ERROR`); on catalog 1016, `CALCULATE_TYPE` is enforced against the adjustment's `direction` (Increase↔ADDITION, Decrease↔DEDUCTION) on create/update/bulk-create. New code-driven endpoint `POST /api/employees/{id}/salaries/{salaryId}/lines` / `DELETE .../lines/{lineId}` (body just `{ code, valueType, value, frequencyType, isTaxable, isRetirementContribution }`, no component/deduction split — resolves the code against 1013 then 1014 and returns the unified `SalaryLineDto`) alongside the existing `/components`/`/deductions` endpoints, which are unchanged. Full contract in `Docs/salary_structure_and_slip_adjustments_implementation_guide.md` §4. **A pre-existing catalog database keeps the old single-value format until each 1013/1014/1016 row is updated via `PUT /api/configs/{id}`** (create-if-missing seeder, same caveat as every prior catalog change) — the new guards simply don't fire until then.
- **fee_frequency**: a FeeCategory (catalog 1010) option's `additionalValue1` must now be `MONTHLY`/`ANNUAL`/`ONE_TIME` — it drives generation defaults and is validated on create/update.
- **Needs a migration that doesn't exist yet**: 10 new tables — every endpoint in this section 500s until it's applied. The 2026-07-17 fee-module fixes additionally need `fee_structure_items.installment_count` (nullable int).

---

# Payroll fixes & Salary Calculator (2026-07-18)

Full reference in `payroll_fixes_implementation_guide.md`; orientation summary (no new migration needed):

- **Run list totals fixed**: `GET /api/payrollruns` now returns real `slipCount`/`totalGrossEarnings`/`totalNetPay` (they were always 0). Cancelled slips are excluded from these aggregates everywhere — they still appear in the detail's `slips` array (`status: 4`), so render them greyed out.
- **`POST /api/payrollruns/{id}/refresh`** (`PAYROLL_RUN_REFRESH`): Draft-only in-place regeneration — picks up compensation-plan edits, tax-slab changes, newly Pending adjustments and newly approved loans made after generation. Preserves Manual slip lines and individually-cancelled slips; adds slips for newly eligible employees; cancels slips of no-longer-payable ones; response = same `{ run, skipped[] }` shape as create. Add a Refresh button on the Run Detail page while `status` is Draft. This is also the remedy when a run seems to use stale tax slabs: runs are immutable snapshots — fix the fiscal year's slabs, then Refresh.
- **SSF rates catalog 1018** (`GET /api/configs/dropdown/1018`, no grant needed): `EMPLOYEE_SHARE` (11) / `EMPLOYER_SHARE` (20), the percentage in `additionalValue1`, admin-editable. Prefill SSF lines in the Add Salary Revision form from it instead of hardcoding. Correct plan shape: `SSF_CONTRIBUTION` component = employer 20% of Basic (taxable, retirement-flagged); `SSF_DEDUCTION` deduction = employee 11% of Basic (retirement-flagged).
- **Employer-share payslip offset (round 2)**: a retirement-flagged *component* (the employer SSF/EPF share) now automatically produces an equal deduction line with the same code on payslips/slips/monthly breakdowns — it's fund money, not cash, so it stays in gross but no longer inflates net pay. Render the pair under earnings and deductions as-is.
- **`POST /api/salarycalculator`** (`SALARY_CALCULATOR`, sub-menu under Accounts — originally "Payroll Management," merged 2026-08-03 — url `/apps/payroll/salary-calculator`): HR structuring tool — fix one monthly figure (`basis`: 1 NetPayment / 2 GrossPayment / 3 Ctc) plus `amount`, optional `fiscalYearId`/`assessmentType`/`includeSsf`, Basic pinned exactly (`basicSalaryAmount`) or as a percent (`basicPercentOfGross`, default 60), plus round-2 knobs `annualBonusAmount` (Dashain/festival bonus — taxed annually, excluded from monthly cash), `monthlyCitAmount` (CIT savings, retirement-flagged), `annualLifeInsurancePremium`/`annualHealthInsurancePremium` (capped per catalog 1015). Returns the solved structure (Basic, Other Allowance, both SSF shares, CIT, monthly TDS via the year's real slabs, net, CTC, annuals incl. bonus, full `taxCalculation` breakdown) plus `suggestedComponents`/`suggestedDeductions`/`suggestedInsurancePremiums` shaped exactly like `POST /api/employees/{id}/salaries` line inputs.
- **`POST /api/salarycalculator/assign`** (`SALARY_CALCULATOR_ASSIGN`, round 2): same body + `employeeId` + `effectiveFromDate` — recomputes server-side and persists the structure as a real salary revision (same conflict/validation path as the manual form). Response: `{ calculation, salary }`.
- **`POST /api/employees/adjustments/bulk`** (`EMPLOYEE_SALARY_ADJUSTMENT_BULK`, round 2): one Pending salary adjustment per in-scope employee in one call (Dashain allowance/bonus/leave-encashment/deduction for everyone) — scope = explicit `employeeIds`, or all payroll-eligible optionally narrowed by `employeeCategoryCode`. Response: `{ createdCount, adjustments, skipped }`. Same past-Draft `409` guard as the single endpoint; a Draft run picks them up on Refresh.
- **Verified, not a bug**: identical TDS across FY-SAMPLE and 2084/85 is correct for the current dev data — the employee's taxable income (Rs. 440,000) sits inside the first bracket of both years and both first brackets are 1%; they only diverge above Rs. 500,000. Also both fiscal years currently have `retirementExemptionCapAmount = 0`, which zeroes the retirement exemption — set a real cap (e.g. 500,000) via `PUT /api/fiscalyears/{id}`. Deduction Type catalog 1014 gained `CIT_DEDUCTION`.

---

# Fee Generation Run & Carry-Forward (2026-07-18)

Full reference in `fee_generation_run_and_carry_forward_implementation_guide.md`; orientation
summary (needs a migration — see below):

- **Fee Generation Runs** (`GET /api/feegenerationruns` / `GET /api/feegenerationruns/{id}` /
  `GET /api/feegenerationruns/{id}/classes/{academicClassId}`): the fee-side counterpart of
  Payroll Runs — a period-keyed master row (one per `academicYearId`/`billingYear`/
  `billingMonth`) found-or-created automatically by `POST /api/feeinvoices/generate`, so it
  accumulates across however many scoped generate calls hit the same month (one class at a time,
  or "all classes"). List rows carry `invoiceCount`/`classCount`/`studentCount`/
  `totalNetAmount`/`totalPaidAmount`/`totalOutstandingAmount`. **2026-07-21**: the detail endpoint
  was split in two so it's never bulky — `GET .../{id}` returns only per-class rollups
  (`classes[{ gradeCode, invoiceCount, studentCount, draftInvoiceCount, totalNetAmount,
  totalPaidAmount, totalOutstandingAmount }]`, no students/invoices), and expanding a class in the
  UI fires the new `GET .../{id}/classes/{academicClassId}` for that one class's
  `students[{ ..., invoices[] }]` tree — build the Fee Generation page's master table (collapsed
  class rows, expand-to-fetch student/invoice detail) around this pair. **Also 2026-07-21**:
  `POST .../{id}/refresh` and `POST .../{id}/classes/{academicClassId}/refresh` (no body) re-run
  generation with `regenerateDrafts: true` for the run's own period — the fix for "I added a Bulk
  Adjustment but the run detail still shows the old amounts" (an adjustment only folds into an
  invoice at generation time; refresh is the discoverable way to re-trigger that from this page
  instead of the separate Generate Invoices dialog). Only Draft invoices are touched; both return
  the same `FeeGenerationResultDto` the generate endpoint does — refresh **never** changes an
  invoice's status (only `POST /api/feeinvoices/finalize` does that); if a class shows mostly
  `Generated` right after a refresh, that status predates the refresh click. **`POST
  /api/feeinvoices/{id}/unfinalize`** (2026-07-21) is the new way back to Draft for one already-
  finalized invoice — the only path for a locked invoice to become eligible for refresh again —
  refused once it has a payment against it (void that first), same guard `cancel` uses.
- **Automatic carry-forward voids the old invoice**: generation now auto-creates a Pending
  `CARRY_CORRECTION` adjustment (catalog 1017) whenever an enrollment has an outstanding balance
  from a strictly earlier billing period, folding it into the new invoice as a normal adjustment
  line (its description reads "Carried forward from INV..." for self-explanatory traceability).
  Every contributing older invoice is voided in the same stroke — `status` flips to `6 Cancelled`
  (a system transition; the user-facing `POST /{id}/cancel` still refuses invoices with payments)
  — and stamped `carriedForwardAmount` (display only) + `carriedForwardToInvoiceId` (the new
  invoice's id — the reference, readable from either invoice). Being genuinely Cancelled means
  the voided invoice is automatically excluded from every outstanding-balance total (statement,
  student search, payment allocation) with no separate exclusion logic — it just disappears from
  the account-statement ledger's running balance (still readable directly via
  `GET /api/feeinvoices/{id}` or by filtering the list for `status=6`). Regenerating a Draft that
  already carried a balance forward doesn't recompute the amount — it just repoints the voided
  invoice(s)' reference to the replacement invoice.
- **Fee adjustments carry student/class context now**: `GET /api/feeinvoices/adjustments`
  rows gained `studentName`/`admissionNo`/`gradeCode`/`sectionCode` — a broad query like
  `?billingYear=2026` used to be unreadable without a second lookup per row.
  `PUT`/`DELETE .../adjustments/{id}` (edit/cancel) already existed, Pending-only.
- **Student search default view reworked**: `GET /api/feeinvoices/students` default page size is
  now 20; with no `search` text, results sort by `outstandingAmount` descending instead of
  alphabetically. An active search still returns matches regardless of balance, sorted
  alphabetically as before. **2026-07-21**: a no-search call now returns every enrolled student in
  scope, not just those who owe something — it previously hid zero-balance students entirely,
  which broke a plain `?academicYearId=` roster browse.
- **`GET /api/feeinvoices` now accepts multiple `status` values** — repeat the key
  (`status=4&status=6`) or comma-separate (`status=4,6`); a single value still works as before.
- **Two generation bugs fixed**: regenerating a Draft invoice no longer silently drops an
  adjustment that was already applied to it; `previousDueAmount` and the Annual-installment
  "already billed" math now only consider invoices strictly earlier than the period being
  generated (previously an out-of-order invoice, e.g. one created ahead by advance payment,
  could skew both).
- **Full-name search fixed (2026-07-21)**: the shared student search backing
  `GET /api/feeinvoices/students` (and enrollment search generally) now splits multi-word queries
  and requires every word to match somewhere across first/middle/last name, admission no, or
  email — so `"Sandhya Adhikari"` now finds the student, not just `"Sandhya"` or `"Adhikari"`
  alone.
- **Needs a migration that doesn't exist yet**: new table `dbo.fee_generation_runs`; new columns
  `dbo.fee_invoices.carried_forward_amount numeric NOT NULL DEFAULT 0` and
  `dbo.fee_invoices.carried_forward_to_invoice_id uuid NULL`.

---

# Audit fields + simplified generate response (2026-07-21)

Full reference in `audit_fields_and_generation_response_implementation_guide.md`; no migration
(pure DTO/mapper exposure of columns that already existed). Summary:

- **`POST /api/feeinvoices/generate` (and its refresh wrappers) response simplified**: `skipped[]`
  (one row per skipped enrollment — up to hundreds on a regenerate call) is replaced by
  `skippedCount` (int, unchanged meaning) + `skippedSummary[{ reason, count }]` grouped by a fixed
  set of short reason strings (`"Invoice Already Generated"`, `"Draft Invoice Already Exists"`,
  `"No Fee Structure Configured"`, `"Fee Structure Not Active"`). **Breaking change** — the old
  `skipped[]` field is gone, not deprecated-alongside. `POST /api/feeinvoices/adjustments/bulk`'s
  `skipped[{ enrollmentId, studentName, reason }]` is unrelated and unchanged.
- **`createdBy`/`createdTs` added to list rows, `updatedBy`/`updatedTs` also added to detail**,
  for: Fee Generation Runs (`GET /api/feegenerationruns` / `GET .../{id}`), Students, Employees,
  Users, Payroll Runs. All four entities already had these columns via `IAuditableEntity` — this
  only exposes them on the DTOs; no new data. Students/Employees/Users/Payroll Runs share one DTO
  for list and detail, so all four fields are always present (list rows just carry
  `updatedBy`/`updatedTs` too, harmless); Fee Generation Runs has a real List/Detail DTO split, so
  the split follows that: base = `createdBy`/`createdTs`, detail subclass adds
  `updatedBy`/`updatedTs`.
- Scoped to the five named areas + their GetById endpoints — the same recipe applies identically
  to any other entity in the system on request (see the guide's §3).

---

# Fee Payment UX, Config Labels & Fee-Rule Fix (2026-07-19)

Full reference in `fee_payment_ux_and_labels_implementation_guide.md`; orientation summary
(no migration needed — code/seed-only):

- **Payment preview shows what is collected**: `POST /api/feepayments/preview` allocations
  now carry a `lines[]` array (the allocated invoice's full line breakdown — `source`,
  `feeCategoryCode`, `feeCategoryLabel`, `description`, `amount`), including advance-billed
  months that don't exist yet. Preview-only; the confirm response leaves it empty (use the
  receipt endpoint after confirming).
- **Fee rules actually apply now**: the payment planner previously required the *pre-discount*
  total to be tendered for a rule to match, then rejected that very amount as an over-payment —
  so no rule ever landed on a confirmed payment. Earned discounts now count toward settlement:
  collecting the quoted `netAmountToCollect` (advance-quote) or the preview's recommended
  amount earns the discount and settles the months in full. Reminder: a rule scoped to a class
  (e.g. Nursery) never fires for another class's enrollment — clear the class scope for a
  school-wide rule.
- **Labels alongside codes, everywhere**: every DTO that stores a Config option code now also
  returns the catalog Label (`feeCategoryLabel`, `adjustmentTypeLabel`, `discountTypeLabel`,
  `scholarshipTypeLabel`, `componentLabel`, `deductionLabel`, `insuranceTypeLabel`,
  `loanTypeLabel`, `label` on payslip/tax-detail lines) — bind display columns to these; they
  fall back to the code, never blank. Newly generated fee-invoice lines and salary-slip lines
  also write label-based `description`s; existing rows keep their old code-based descriptions
  (refresh a Draft payroll run / regenerate a Draft invoice to pick labels up).
- **Statement of Account moved**: the Student Management sidebar entry is retired (menu
  seeder retire pass); build it as a tab on the student profile instead, calling the existing
  `GET /api/feeinvoices/account-statement/{enrollmentId}` with `currentEnrollment.id`.
- **Statement of Account now shows discounts as their own line (2026-07-20 fix)**: an invoice's
  `debit` is now `grossAmount` (pre-discount) rather than `netAmount`; any discount/scholarship/
  rule-discount/negative-adjustment on that invoice posts a same-date `credit` entry right after
  it (`entryType` `"Discount"`/`"Scholarship"`/`"Rule Discount"`/`"Adjustment"`, `description`
  taken from the invoice line, e.g. `"Discount - Sibling Discount"`) instead of being silently
  netted into the invoice row with no visible trace. `closingBalance`/running `balance` math is
  unchanged — treat any `entryType` beyond `"Invoice"`/`"Payment"` as a generic credit row rather
  than hardcoding just those two. See `Docs/fee_module_fixes_implementation_guide.md` §3.
- **Monthly Adjustments clarified**: rows typed `CARRY_CORRECTION` ("Opening Balance / Carry
  Correction") are system-generated carry-forwards — render them read-only. Manual
  adjustments are editable while Pending via `PUT /api/feeinvoices/adjustments/{id}` and
  cancellable via `DELETE /api/feeinvoices/adjustments/{id}`; both 409 once Applied
  (regenerate/cancel the invoice to re-pend them first).

---

# Dual Calendar (AD / Bikram Sambat) & Meetings (2026-07-16)

Full reference in `dual_calendar_implementation_guide.md`; orientation summary: everything is stored AD-canonical with the BS date denormalized alongside, so every payload carries both calendars and the UI never converts anything itself. BS month lengths are DB-driven (`bs_month_lengths`, seeded BS 2000–2090, admin-extends yearly via `POST /api/calendar-configuration/bs-month-lengths`; anchor BS 2000-01-01 = AD **1943-04-14** — the design doc's 04-13 was off by one, verified against known reference dates). `GET /api/calendar-configuration/localization-data` returns the 12 BS month names + 7 weekday names (EN/NP, `isWeeklyHoliday` — Saturday seeded true); `PUT …/weekdays/{index}` edits them. `GET /api/calendar/month-view?year=&month=&mode=BS|AD` is the one calendar-page call — one row per day with both dates, localized day names, weekly-holiday/`isToday` flags (Nepal-time today), and that day's events, festivals, and meetings pre-joined. `GET /api/calendar/today` and `GET /api/calendar/convert/ad-to-bs|bs-to-ad` power dual date pickers (all three return the same `DualDateDto`; open to any authenticated user). `CalendarEvent` CRUD lives at `/api/calendar/events` (types: 0 Note, 1 PublicHoliday, 2 InternalEvent; date enterable on either calendar via `isBsDate`); BS-anchored shifting festivals (Dashain/Tihar) at `/api/calendar/festivals` (one row per festival per BS year, entered in BS, AD range computed; 409 on duplicate name+year). Meetings at `/api/meetings` — `POST /schedule` (either-calendar date via `isBsScheduled`, host defaults to the caller, attendee emails start Pending, **409 when the host is double-booked** on an overlapping time block), paged list, update (attendee replace-sync, host immutable), soft-delete cancel, and `POST /respond` (`{ meetingId, email, status }`, open to all authenticated users). **The 7 new tables need a migration that doesn't exist yet** — every endpoint in this section 500s until it's applied, and `CalendarSeeder` (month/weekday names + the month-length table) skips until then.

---

# Leave Management, Notifications & Employee Profile (2026-07-23)

> **2026-08-06: self-service arrived.** Every route below (leave balances/requests, profile) is
> now also reachable without an `{id}` — see "Employee Self-Service" further down — for any login
> whose `ApplicationUser` is linked to this `Employee` row, resolved from the JWT rather than a
> route parameter. The admin `{id}`-scoped routes described here are unchanged.

Full reference in `leave_management_and_employee_profile_implementation_guide.md`; orientation summary: built from a raw schema sketch and an Employee Profile page mockup, everything here is admin-facing at `/api/employees/{id}/...`. `Employee` gained org fields — `branchCode`/`provinceCode`/`levelCode` (Config catalogs `1019`/`1020`/`1021`), a self-referencing `managerId`, and a single `photoPath` (`POST/GET-download/DELETE /api/employees/{id}/photo`, jpg/jpeg/png only). `LeaveType` (`/api/leavetypes`, real entity — `daysPerYear`/`carryForward`/`isPaid`, not a Config catalog) is seeded with Annual/Sick/Casual (18/12/12, illustrative). `EmployeeLeaveBalance` (`GET/POST /api/employees/{id}/leavebalances`) is scoped to the **current fiscal year only** — `balance = allocated - used - pending`. `LeaveRequest` (`POST /api/employees/{id}/leaverequests` with optional multipart attachment, plus list/detail/manager-approve/manager-reject/hr-approve/hr-reject/cancel, and substitute add/remove) has **two independent one-shot approval fields**, `managerStatus`/`hrStatus` — **`hrStatus` is authoritative and is never gated on `managerStatus`**, so HR can approve or reject a request regardless of what the manager has or hasn't done (an explicit "emergency override" requirement) — only an HR decision actually moves days between `pending`/`used` on the balance. `effectiveStatus` in the response is computed, not stored (HR's decision if made; else `Rejected` if the manager rejected; else `Pending`). `Notification` (`GET /api/employees/{id}/notifications`, mark-read/mark-all-read) is raised automatically on submit and on each manager/HR decision — nothing else creates one yet (birthday/anniversary reminder types are reserved for a future job, not wired up). `GET /api/employees/{id}/profile` is the one composite call behind the whole profile page: personal/employment info, current-fiscal-year leave summary, live-computed upcoming birthday/work-anniversary, and pending leave requests — there's no Attendance module, so the mockup's "View Attendance" quick action has nothing to link to. Holidays reuse the existing Dual Calendar module rather than a new table: `CalendarEvent` with `eventType = PublicHoliday` gained optional `provinceCode`/`branchCode` scoping, and two new event types, `StudentBirthday`/`EmployeeBirthday`, let an admin pin a specific person's birthday onto the shared calendar (`studentId`/`employeeId` fields, exactly one required matching the type). **New tables (`leave_types`, `employee_leave_balances`, `leave_requests`, `leave_substitutes`, `notifications`) and new columns on `employees`/`calendar_events` need a migration that doesn't exist yet** — every endpoint in this section 500s until it's applied.

---

# Employee Self-Service (2026-08-06, Dashboard added 2026-08-07)

Full reference in `employee_self_service_implementation_guide.md`; orientation summary: 12 new
`/api/employees/me/...` routes (no `{id}`) mirroring the admin routes above byte-for-byte in
request/response shape — `me/profile`, `me/leavebalances`, `me/leaverequests` (POST/GET),
`me/leaverequests/{requestId}` (GET), `me/leaverequests/{requestId}/cancel` (POST),
`me/payslips` (list + `{fiscalYearId}/{monthIndex}` detail), `me/salaries/payslip-preview`,
`me/salaries/tax-planning`, `me/salaries/tax-details`, `me/assignments`. Each resolves the
caller's own `Employee` from the JWT (`Employee.UserId`) rather than a route parameter — a Student
or an unlinked Admin account gets a clean `404` ("not linked to an employee record"), not a 500.
Deliberately **not** teacher-specific — any Employee-linked login gets this regardless of role
(Teacher, Accountant, HR, Principal, ...), and manager/HR approval actions, salary editing, and
loans stay admin-only. **Access itself needs no permission grant** (all "me" routes are in
`appsettings.json`'s `DefaultEnabledMenu`) — a `MY_WORKSPACE` menu (5 sub-menus: My Dashboard, My
Profile, Leave & Balance, Payslip & Taxes, My Classes) exists only so a role's sidebar can show nav
links for them, via the normal one-time `POST /api/roles/claims` grant; the API call already works
without it. `GET /api/calendar/month-view`/`events`/`festivals` were also added to
`DefaultEnabledMenu` (read-only, school-wide — not employee-scoped, so no "me" wrapper needed).

**`GET /api/employees/me/dashboard`** (+ admin `GET /api/employees/{id}/dashboard`, permission
`EMPLOYEE_DASHBOARD_VIEW`), added 2026-08-07: one composite call for the Employee Dashboard
landing page — `leaveSummary`/`pendingLeaveRequests` (same figures as the Profile page),
`classRoutine` (`TeacherAssignmentDto[]`, the employee's whole assigned-period routine ordered by
period start time — this codebase has no day-of-week timetable, so a period recurs every school
day, not just "today"), `nextClass` (the first routine entry whose period hasn't started yet
today in Nepal time, or `null`), and `upcomingEvents` (Birthday/WorkAnniversary merged with
`PublicHoliday`/`InternalEvent` calendar rows and BS festivals over the next 30 days, capped at
10, Province/Branch-scoped like the calendar itself). No migration needed — reuses
`Employee.UserId`, `CalendarEvent`, and `FestivalOccurrence`, all pre-existing.

**Calendar Year View + Teacher/Student access (2026-08-24)**: added
`GET /api/calendar/year-view?year=&mode=BS|AD` — all 12 months of the requested year, each shaped
exactly like a `month-view` entry (full reference in `dual_calendar_implementation_guide.md`
§2). Added to `DefaultEnabledMenu` alongside `GetMonthView` (no permission grant needed to call
it). The calendar page's nav entries, `CALENDAR_MANAGEMENT`/`CALENDAR_VIEW`, changed from
Admin-only to `menuFor: BOTH` so a Teacher (Employee-linked login) or Student self-service login
can see "Calendar" in their sidebar once granted — both those account kinds resolve
`MenuAudience.User` (`RoleService.ResolveMenuAudience`), same as the `USER_PORTAL`/My Workspace
tree, and an Admin-only menu never shows up for them regardless of role claims. A new hidden leaf,
`CALENDAR_YEAR_VIEW`, gates the frontend's Month/Year toggle button (the API itself doesn't check
it). **No claims are auto-granted** — same as everywhere except SuperAdmin's full auto-grant, an
admin grants `CALENDAR_MANAGEMENT` → `CALENDAR_VIEW` → `CALENDAR_YEAR_VIEW` to the Student role
and to whichever role covers Teacher/Employee self-service logins, via Role Menu Access, to turn
this on. Event/festival CRUD permissions nested under `CALENDAR_VIEW` are unaffected — grant those
separately if a role should also manage them.

---

# Leave Configurability (2026-07-24, revised same day)

Full reference in `leave_configurability_implementation_guide.md`; orientation summary: `LeaveType`
gained two optional policy fields — `maxConsecutiveDays` (cap per single request) and
`maxDaysPerWeek`/`maxDaysPerMonth` (cap on Pending+Approved days of that type within the calendar
week/month containing the request's `fromDate`, clipped per request so a boundary-spanning request
only counts its portion). `LeaveRequest` gained `isEmergency` (bool, requester's own claim, sent
alongside the existing Apply Leave multipart fields) — **unconditionally** bypasses all three caps
for any leave type (no per-type toggle), and is **unrelated** to the pre-existing HR "emergency
override" of the manager/HR approval workflow; don't conflate the two. An earlier revision also
added a per-type mandatory-document rule and a per-type emergency-override toggle — both were
dropped the same day: whether a supporting document backs a request is left to the requester/HR
conversation, not a system-enforced gate, so `LeaveRequest.AttachmentPath` stays optional for every
leave type exactly as before. Two new seeded leave types: Bereavement Leave and Marriage Leave
(both capped at their own yearly allocation as a single-request maximum). All policy fields are
optional and independent — an unconfigured leave type behaves exactly as it did before this
feature. **Needs a migration** — three new columns on `leave_types`, one new column
(`is_emergency`) on `leave_requests`; every leave-type create/update and leave-request create call
fails until applied.

---

# Employee Address — Province/District/Local Level/Ward (2026-07-24)

Full reference in `employee_address_implementation_guide.md`; orientation summary: extends the
existing Employee `provinceCode` org field into a full Nepal address chain — `districtCode`
(Config catalog `1022`) and `localLevelCode` (`1023`, Nepal's municipality/rural-municipality/
metropolitan/sub-metropolitan-city catalog) plus a plain `wardNo` (1–99, shape-only). Both new
catalogs reuse the existing `ConfigType`/`Config` tables (no new tables). Seed data: all 77
districts (full list, stable since the 2017 restructuring); **80 of 753 local levels** — the 6
metro + 11 sub-metro cities plus one notable municipality per remaining district, so every
district has at least one usable option — the remaining ~670 are deliberately not seeded (avoiding
baking possibly-wrong government records from memory) and should be added via `POST /api/configs`
from an official source before relying on this for real addresses. `GET /api/configs/dropdown/
{typeCode}` gained generalized `parentCode` (cascading, filters on `AdditionalValue1`) and `search`
(Label substring match) query params, reusable by any hierarchical catalog. `EmployeeService`
auto-derives `districtCode`/`provinceCode` from a supplied `localLevelCode` (or `provinceCode` from
`districtCode`) when the coarser fields are left blank — this is the actual "reverse map
automatically" behavior; anything supplied at more than one level is cross-checked, never silently
overridden. **Needs a migration** — three new nullable columns on `employees`
(`district_code`/`local_level_code`/`ward_no`); every address read/write 500s until applied.

---

# Portal Account Provisioning for Employees & Students (2026-07-27)

Full reference in `portal_account_provisioning_implementation_guide.md`; orientation summary: an
Employee or Student can optionally get a real login, on request — `registerUserAccount: true` on
`POST /api/employees`/`POST /api/students` (Employees also need `roleIds`, admin-picked; Students
always get the fixed new `Student` role, no picker), or retrofitted afterward via
`POST /api/employees/{id}/register-account` / `POST /api/students/{id}/register-account`.
`EmployeeDto.userId`/`StudentDto.userId` are non-null once provisioned. The account is created
with **no password** (same shape as a Google-signed-in account) and an email is sent with a
password-reset link to activate it — reuses the existing `POST /api/auth/reset-password`
endpoint, no new activation mechanism. `Conflict` if the email is already registered to another
account or the record already has one; `ValidationError` for a missing email/role. **Needs a
migration** — one new nullable column `students.user_id` + its unique partial index (mirrors the
existing `employees.user_id`); every student `register-account` call and any create with
`registerUserAccount: true` 500s until applied.

## Exam Management — assessment configuration, exams, marks, results & promotion (2026-07-28, redesigned 2026-07-29 per `Docs/Exam_Module_Design_Revised.md`, room/seat-arrangement subsystem and invigilator removed 2026-07-30)

The full Exam/Result/Promotion design (`Docs/Student_Management_System_Exam_Result_Promotion_Design.md`)
was implemented, then **redesigned once** (merging "Exam" + "ExamSchedule" into one resource), then
**redesigned again** the next day from `Docs/Exam_Module_Design_Revised.md`: an `Exam` no longer
pins a `ClassSection`, `Name`, `WeightagePercent`, or `IsFinalExam` at all — it's keyed by
`(examTermId, classSubjectId)` only, always covers the whole grade, and "who sits it" is resolved
dynamically from the subject's own mandatory/elective/section-scoping rules. That same redesign
added an optional examination hall/seat-allocation engine, which was then **removed entirely the
following day** (2026-07-30, per instruction) — the module only needs simple subject/date/time
scheduling. Full reference, two guides: `exam_management_implementation_guide.md` (the module) and
`exam_routine_and_marks_configuration_implementation_guide.md` (the 2026-07-30 whole-class routine
endpoint + marks-configuration defaulting). Orientation summary:

- `ClassSubject`'s existing grading fields (create/update via
  `POST`/`PUT /api/academicclasses/{id}/subjects[/{classSubjectId}]`) are the **only** place
  grading is configured for the whole module — **`fullMarks`/`passMarks` are READ-ONLY, computed
  server-side** (2026-07-30, redesigned same day from an earlier version that accepted them as
  independent inputs) as `theoryMarks + practicalMarks` / `theoryPassMarks + practicalPassMarks`
  — never send them on assign/update. `hasTheory`/`hasPractical` pick whole-mode (theory only,
  the default) vs. divided mode (both required together when both flags are on); the disabled
  component in whole mode is forced to `0` server-side (the "theory-only fallback" rule), and
  leaving the enabled component(s) unset keeps the computed totals `null` ("not graded yet") rather
  than `0`. A student must clear each enabled component's own pass mark individually — passing the
  combined total alone is not enough if either component fails (unaffected by this round). Full
  detail: `exam_routine_and_marks_configuration_implementation_guide.md` section 2.
- `POST/GET/PUT/DELETE /api/examterms` (a macro period like "First Terminal", soft-deleted, unique
  `code`) → `/api/exams` (**one subject's single sitting per term, no section, no name/weightage/
  isFinal, no room, no invigilator** — just date/time/remarks, hard-deleted, `409` on a duplicate
  `(examTerm, classSubject)` pair, plus `POST /api/exams/{id}/lock`/`unlock` to close/reopen its
  marks-entry window; there is no `for-class` endpoint anymore — nothing left for it to do once an
  exam already always covers the whole grade). **`PUT /api/exams/routine`** is the batch-scheduling
  workflow — submits a whole class's exam term timetable in one grid and idempotently syncs it
  (create/update/remove) against whatever's already scheduled, validated as one atomic transaction
  (term-boundary + time-overlap checks; a removal blocked by recorded marks fails the whole save).
  **Redesigned 2026-07-30 same day** from an earlier create-only/skip-list `POST` cut — see the
  companion guide for the full contract. **Creating/updating/deleting an exam auto-manages a linked
  `CalendarEvent`** (`eventType: 5`). `ExamDto` exposes `fullMarks`/`passMarks`/etc. read-only,
  sourced from the linked subject. **2026-07-30, class period timing (superseded 2026-08-03, see
  below)**: `startTime`/`endTime` are now `TimeSpan?` on create/update, and each exam (or routine
  item) can instead send `timePeriodId` (a real `TimePeriod` id — see the dedicated round entry
  below) to have the times resolved server-side from the picked period — exactly one of the two
  paths is required; a `Break`-kind period is rejected, and the period must be mapped to the
  exam's own class.
- `/api/gradescales` (percentage-band grading schema, A+/A/B+/.../F with grade points, soft-deleted,
  unique `grade`), `/api/studentexammarks` (+ `POST /api/studentexammarks/bulk` for a whole
  roster's marks in one call, + `GET .../roster` for a searchable single-student entry worklist —
  both `GET .../roster` and the plain `GET /api/studentexammarks` list gained a `classSectionId`
  filter 2026-07-30, since the same subject can be taught by different teachers in different
  sections and an `Exam` has no section of its own; + new `GET .../student/{enrollmentId}`,
  admin's "one student, every subject" entry screen), `/api/examresults`
  (`generate`/`publish/{examTermId}`/`{id}/withhold`/`{id}/lift-withhold` plus the paged
  list/detail — **every exam in the term now contributes, there is no "final exam only" filter
  anymore**), and `/api/studentpromotions` (+ `POST /api/studentpromotions/bulk-process` for the
  pass→promote/fail→retain whole-section workflow) round out the module.
- **`/api/examrooms` and `/api/examhallarrangements` no longer exist** (removed 2026-07-30, along
  with `ExamRoom`/`ExamHallArrangement`/`ExamHallArrangementClass`/`ExamSeatAllocation` and
  `Exam.RoomId`/`Room`) — drop any frontend calls to either controller or any `roomId` field on an
  exam create/update. **`Exam.InvigilatorEmployeeId`/`InvigilatorEmployee` are also gone** (same
  day, follow-up) — drop `invigilatorEmployeeId` from any exam create/update body too.
- **Migration exists for the room removal, not for the invigilator removal**:
  `20260730061558_changes in exam module3 update.cs` drops the four room/seat-arrangement
  tables plus `exams.room_id` — apply it via `dotnet ef database update` if it hasn't run yet.
  Dropping `dbo.exams.invigilator_employee_id` (and its FK) still needs a further migration —
  harmless today (EF just never touches that column). See the migration-status callout at the
  top of `exam_management_implementation_guide.md` for the full chain of migrations this module
  has been through, including the class-period-timing churn covered in the next two round entries.

**Round (2026-08-03): teacher-assignment period timing + bulk multi-section assign, first cut —
Config-based, superseded the same day by the next round entry.** `TeacherAssignmentDto`/
`AssignTeacherCommand` gained a `periodCode` field (a Config catalog code) and
`POST /api/employees/{id}/assignments/bulk` was added (`AssignTeacherBulkCommand`:
`classSubjectId`, `classSectionIds` (non-empty list), `isClassTeacher`, plus the period field) —
assigns the same subject/period to a teacher across several sections in one call instead of
repeating `POST /api/employees/{id}/assignments` once per section. Skip-list style, like every
other bulk endpoint in this codebase: `{ created: [...], skipped: [{ classSectionId, reason }] }`,
never an all-or-nothing reject. `isClassTeacher: true` is only valid with exactly one
`classSectionId` (a class teacher belongs to one section) — that specific combination 400s the
whole request rather than landing in `skipped`, since it's a malformed request, not a per-item
business conflict. Nothing stops a teacher from being the class teacher of one section while also
teaching a subject (via this bulk call or the single endpoint) in a different section — that's
just two ordinary `TeacherAssignment` rows. **The bulk endpoint's shape is unchanged by the next
round** — only the period field's type changed.

**Round (2026-08-03, same day): class period timing redesigned onto a real `TimePeriod` table —
this is the version that ships.** The Config-catalog cut above (and its exam-side twin) turned out
to be the wrong model: "certain classes run different period structures" is a relationship, which
a flat Config option list can't express. Replaced with `Domain/Entities/TimePeriod` (a real
table: `Id`, `Name`, `StartTime`, `EndTime`, `Kind` — `Period`/`Break`, `Order`) and
`ClassTimePeriod` (the class↔period mapping, bulk-created via `POST /api/timeperiods/map`, taking
`{ academicClassIds: [...], timePeriodIds: [...] }` and creating the cross-product, skip-list
style). `Exam.PeriodCode`/`TeacherAssignment.PeriodCode` (both string, Config-code) became
`Exam.TimePeriodId`/`TeacherAssignment.TimePeriodId` (both `Guid?`, real FKs) — a picked period
must now (a) not be a `Break`-kind row and (b) be mapped to the exam/assignment's own class via
`ClassTimePeriod`, a check the Config-based cut had no way to perform at all. `ExamDto`/
`TeacherAssignmentDto` both expose `timePeriodId` + a server-resolved `timePeriodName`. New
`TimePeriodsController` (`/api/timeperiods`, standard CRUD) plus the mapping endpoints
(`POST /map`, `GET /map/{academicClassId}`, `DELETE /map/{academicClassId}/{timePeriodId}`); new
`TIME_PERIOD_LIST` sub-menu under `SETUP`. `TimePeriodSeeder` seeds one illustrative full day
(8 periods + a short break + lunch break) but deliberately maps none of it to any class — that's
an admin decision made via the bulk-map endpoint. Full reference:
`Docs/time_period_and_class_routine_implementation_guide.md`.
- **Needs a migration** — supersedes, not adds to, the two `period_code` column migrations from
  the earlier rounds (`teacher_assignments.period_code` from
  `20260803083606_update in setup for class assignment.cs`, and `exams.period_code` from
  `20260730093356_changes in in exams for period code.cs`): new tables `dbo.time_periods` /
  `dbo.class_time_periods`; on both `dbo.teacher_assignments` and `dbo.exams`, drop `period_code`
  and add `time_period_id uuid NULL` (FK → `time_periods.id`, Restrict). Until applied, every
  `TimePeriod`/`ClassTimePeriod` call 500s, and every `Exam`/`TeacherAssignment` create/update
  500s. Exact statements in the guide's own migration section.

**Round (2026-08-03, same day): Subject catalog gained a `GRADE_CODE` field.** `Config.AdditionalValue2`
on a `Subject` (TypeCode `1003`) option now names the grades that subject is actually offered to —
`"ALL"` when every grade offers it, or a comma-separated `Domain/Constants` `GradeCodes` list (e.g.
`"NINE,TEN,ELEVEN,TWELVE"`) otherwise. `SampleDataSeeder` computes this from the same
mandatory/optional grade→subject mapping it uses to seed real `ClassSubject` rows, so the two can't
drift apart. **Informational only** — `AcademicClassService.AssignSubjectAsync` still validates a
`SubjectCode` against the `Subject` catalog by `Code` alone; it does not cross-check `GRADE_CODE`
against the class being assigned to, so assigning e.g. `PHYSICS` to a Grade Three class is not
blocked by this field. A subject created via `POST /api/configs` leaves `additionalValue2` blank
until set manually. No migration needed — this reuses the existing `Config.AdditionalValue2`
column; an already-seeded database's existing `Subject` rows keep it blank until re-seeded fresh or
edited via `PUT /api/configs/{id}`.

**Round (2026-08-04): general-purpose bulk entry for teacher assignments.**
`POST /api/employees/{id}/assignments/bulk-entry` (`AssignTeacherBulkEntryCommand`: `items[]`, each
with its own `classSubjectId`/`classSectionId`/`isClassTeacher`/`timePeriodId`) is a new sibling to
the existing `POST /api/employees/{id}/assignments/bulk` — that one fixes one `classSubjectId`/
`timePeriodId` per call and only varies the section list; this one lets every row be a completely
different class/subject/section/period, so a teacher's whole routine can be entered in one submit
instead of one call per row. Still scoped to one teacher (the route id) — no multi-teacher grid,
same "no logged-in-teacher resolution anywhere in this codebase" reasoning as every other
teacher-assignment endpoint. Skip-list style response: `{ created: [...], skipped: [{ itemIndex,
classSubjectId, classSectionId, reason }] }` — a bad row never fails the rest of the batch; two new
in-request-only guards (on top of every check the single-assignment endpoint already does) catch a
duplicate `(classSubjectId, section)` pair or two rows both claiming class-teacher of the same
section **within the same submission**, since those can't be caught by the usual database-existence
checks before anything's been saved. New permission `TEACHER_ASSIGNMENT_BULK_ENTRY_ADD`. No
migration needed — creates ordinary `TeacherAssignment` rows through the same repository method the
other assignment endpoints already use. Full reference:
`Docs/teacher_assignment_bulk_entry_implementation_guide.md`.

**Round (2026-08-04, same day): class-scoped sibling — bulk entry from the Academic Class side.**
`POST /api/academicclasses/{id}/teacher-assignments/bulk-entry`
(`AssignClassTeachersBulkEntryCommand`: `items[]`, each with `teacherId`/`classSubjectId`/
`classSectionId`/`isClassTeacher`/`timePeriodId`) is the third assignment-bulk endpoint: scoped to
one **academic class** (the route id) rather than one teacher, and each row names its own teacher
— built for "who teaches this class," mapping several different teachers across a class's
subjects/sections/periods in one submission from the class's own page, instead of visiting every
teacher's profile in turn. Every row is validated the same way the other two assignment endpoints
validate theirs — `Application/Teachers/TeacherAssignmentBuilder.BuildAsync` was extracted out of
`TeacherService`'s formerly-private `BuildAssignmentAsync` into a shared static helper specifically
so `AcademicClassService` could reuse the identical (teacher, subject, section, period) rule set
rather than duplicating it — plus one check unique to this entry point (a row's `classSubjectId`
must belong to the route's own academic class) and an in-request duplicate check keyed by
`(teacherId, classSubjectId, section)` — note `teacherId` is part of that key here, since (unlike
the teacher-scoped endpoint) the same subject/section pair can validly be taught by two different
teachers in the same batch; only the same teacher assigned to it twice is a duplicate. New
permission `CLASS_TEACHER_ASSIGNMENT_BULK_ENTRY_ADD` under `CLASS_LIST`. No migration needed — same
`AddAssignmentAsync` repository method as the other two. Full reference:
`Docs/class_teacher_assignment_bulk_entry_implementation_guide.md`.

**Round (2026-08-04, same day): three validation tightenings across every teacher-assignment
endpoint.** `Application/Teachers/TeacherAssignmentBuilder.BuildAsync` — the one shared place all
four assignment endpoints' per-row validation goes through (single, both bulk-sections/bulk-entry
teacher-scoped endpoints, and the class-scoped bulk-entry endpoint) — gained two new rules, plus a
simplification of a third that was already correct:

1. **`classSectionId` is now required** for a class-wide subject — a single assignment can no
   longer cover "every section of the class" (previously `null` meant that). A section-scoped
   subject still derives its section automatically and doesn't need it repeated. `ValidationError`:
   `"ClassSectionId is required -- a teacher must be assigned to one specific section, not every
   section of the class at once."` Rows created before this date may still read back with a null
   `classSectionId`/`sectionCode` (`Scope: ClassWide`) from the old behavior — that's a legacy read
   shape now, not a creatable one.
2. **Time-period double-booking is now blocked** — `ITeacherRepository.TeacherHasTimePeriodConflictAsync`
   (new) checks whether the teacher already has *any other* assignment (any class/subject/section)
   sharing the same `timePeriodId`; if so, the row is rejected: `"This teacher is already assigned
   to another class/section during '<period name>'."` Every bulk endpoint also gained an in-request
   staged-period guard for the same reason the existing duplicate/class-teacher guards exist
   (nothing is saved until the batch's `SaveChangesAsync`, so two colliding rows in one request
   wouldn't otherwise see each other) — keyed by `(teacherId, timePeriodId)` on the class-scoped
   endpoint (two *different* teachers can share a period; only the same teacher twice conflicts) and
   effectively just `timePeriodId` on the two teacher-scoped endpoints (teacher is constant for the
   whole request there). One side effect: `POST /api/employees/{id}/assignments/bulk`'s shared
   `TimePeriodId` now only ever succeeds for the *first* section in the list when set — assigning a
   teacher to several different sections during the identical period is exactly the scenario this
   rule exists to block, so use `.../assignments/bulk-entry` (each row gets its own period) instead
   when periods need to differ per section.
3. **"One class teacher per section" is unchanged in substance** (`ClassTeacherExistsForSectionAsync`,
   unmodified) but its code got simpler — since `classSectionId` is now always present, the old
   `"ClassSectionId is required when IsClassTeacher is true"` guard is unreachable and was removed.

No migration — no schema change, this is application-layer validation only (`TeacherAssignment.ClassSectionId`
stays a nullable DB column so legacy class-wide rows keep displaying correctly). Both
`teacher_assignment_bulk_entry_implementation_guide.md` and
`class_teacher_assignment_bulk_entry_implementation_guide.md` updated with the new field
requirements and failure-reason tables.

**Round (2026-08-04, same day): the missing GET — list teachers by class.** Every earlier round
this same day added a way to *create* `TeacherAssignment` rows in bulk, but there was still no way
to read them back scoped by class — only `GET /api/employees/{id}/assignments` (one teacher at a
time) existed. New **`GET /api/academicclasses/{id}/teacher-assignments`**
(optional `?classSectionId=` query param to narrow to one section) returns
`ClassTeacherAssignmentDto[]` — the same core fields as `Application.Teachers.Dtos.TeacherAssignmentDto`
plus `teacherName`/`employeeCode` (a class-scoped listing needs the teacher's name up front, unlike
a teacher's own profile page). Backed by new `ITeacherRepository.GetAssignmentsByAcademicClassAsync`
(`TeacherRepository` — filters on `TeacherAssignment.ClassSubject.AcademicClassId`, `Include`s
`Teacher.Employee`/`ClassSubject`/`ClassSection`/`TimePeriod`) and
`AcademicClassMapper.ToTeacherAssignmentDto` (new, own `BuildFullName` helper — mappers stay
self-contained per this codebase's convention). Unpaged, sorted by subject code then teacher first
name — a class routine is a small, bounded dataset. New permission `CLASS_TEACHER_ASSIGNMENT_LIST`
under `CLASS_LIST`. This is the data source the "Class Routine" grid (from the bulk-entry round
above) loads on page load and re-loads after every submit; removing a row still goes through the
existing `DELETE /api/employees/{teacherId}/assignments/{assignmentId}` (no new delete endpoint
needed — it only needs the assignment's own `id`, which this list returns). No migration —
read-only over existing data. Full reference:
`Docs/class_teacher_assignments_list_implementation_guide.md`.

**Round (2026-08-05): student timetable + profile-response optimization.** Three related changes,
all in `Docs/student_timetable_and_profile_optimization_implementation_guide.md`:

1. **New `GET /api/students/{id}/timetable`** — a student-facing "who teaches my classes, and
   when" view (`StudentTimetableDto`: header + `entries[]` of subject/teacher/period, sorted by
   period start time). Reuses `ITeacherRepository.GetAssignmentsByAcademicClassAsync` (the same
   repository method the "who teaches this class" admin endpoint uses), scoped to the student's own
   section and filtered down to the subjects they actually study (mandatory + their own chosen
   electives). New permission `STUDENT_TIMETABLE`.
2. **`GET /api/students/{id}` slimmed to a profile-header shape** — `guardians[]` is now always
   empty on this endpoint (the "Guardians" tab already had its own `GET .../guardians`, this was
   duplicate data; create/update responses still populate it), `currentEnrollment.subjects[]` is
   gone (replaced by the new timetable endpoint), and `enrollmentHistory[]` is removed entirely —
   replaced by new **`GET /api/students/{id}/enrollment-history`** (new permission
   `STUDENT_ENROLLMENT_HISTORY`). This is a **breaking DTO shape change** for any UI already reading
   those three things off the main GET — see the guide's before/after table. One internal fix
   alongside it: `GetIdCardPreviewAsync` used to read guardians off the (now-empty) profile DTO —
   fixed to query the repository directly instead of silently printing blank guardian fields on ID
   cards.
3. **Server-resolved Config labels** — `StudentGuardianDto.RelationshipLabel`,
   `StudentCurrentEnrollmentDto`/`StudentEnrollmentHistoryDto`/`StudentTimetableDto`'s
   `GradeLabel`/`SectionLabel`, and `StudentTimetableEntryDto.SubjectLabel`, all resolved
   server-side via the existing `ConfigLabelHelper` (first use in the Student feature; already used
   by Fee/Payroll). Removes the need for a UI to call the dropdown endpoints just to turn
   `"FATHER"` into `"Father"`.

The guide also writes up the general "one API call per tab, not one call for the whole page"
convention this round is the reference implementation of — apply it (and the `ConfigLabelHelper`
label-resolution pattern) to other screens as they're next touched, rather than assuming every
other feature was swept in this same round (it wasn't — see the guide's own "Scope of this round"
section). No migration — DTO/service-layer only.

**Round (2026-08-05, same day): Global Search (navbar "Ctrl+K").** New
**`GET /api/dashboard/global-search?query=&limit=`** — the cross-menu search a navbar search box
needs (distinct from the Students/Employees list pages' own scoped `search` params): matches
Students by `FirstName`/`LastName`/`AdmissionNo` and Employees (including teaching staff — see
below) by `FirstName`/`LastName`/`EmployeeCode`, plus an exact match on either record's own `Id`
when `query` parses as a GUID (the "Student ID"/"Employee ID" half of the ask). Response is
grouped, not one flat list: `{ query, students: [...], employees: [...] }`, each group
independently capped at `limit` (default 5, max 20) and sorted by name.
`GlobalSearchEmployeeResultDto.IsTeacher` (`Employee.Teacher != null`) replaces a separate
"Teachers" group — Teacher isn't a standalone root aggregate in this codebase (shared-PK with
Employee), so a third group would either duplicate the Employee rows or arbitrarily exclude
teaching staff from `employees[]`; `isTeacher` tells the UI which detail route to use instead.
`JobPositionLabel` is resolved server-side via the existing `ConfigLabelHelper`. Lives on the
existing `IDashboardService`/`DashboardService` (not a new feature folder) — same "cross-cutting,
multi-aggregate, UI-navigation concern" shape as `GetQuickMenusAsync`/`GetAccountsSummaryAsync`/
`GetHrSummaryAsync`, querying `ApplicationDbContext.Students`/`.Employees` directly rather than
adding search methods to `IStudentRepository`/`IEmployeeRepository`. New permission
`DASHBOARD_GLOBAL_SEARCH` — gated like every other Dashboard widget (**not** in
`DefaultEnabledMenu`), a deliberate choice since this can surface Student/Employee records across
the whole org independent of the caller's `STUDENT_LIST`/`EMPLOYEE_LIST` grants. Deliberately a
typeahead, not a paged search — no `page`/total-count, no fuzzy matching, no relevance ranking
beyond alphabetical; point a "see all results" action at the existing list endpoints' own `search`
param instead. No migration — read-only over existing data. Full reference:
`Docs/global_search_implementation_guide.md`.

## Teacher & Student Portal — separate self-service navigation shells (2026-08-21)

A distinct, non-admin-feeling shell for Teacher and Student logins. The Teacher side reuses the
existing Employee self-service ("Me") endpoints unchanged. The Student side is new: four
`me/...` routes on `StudentsController` (`GetMyProfile`, `GetMyTimetable`,
`GetMyEnrollmentHistory`, `GetMyDashboard`), resolved from the JWT via a new
`IStudentRepository.GetByUserIdAsync`, `DefaultEnabledMenu`-gated like every other "Me" route.
`GetMyDashboard` returns a new composite `StudentDashboardDto` (current class, fee-due summary,
recent exam results, upcoming events) and is also wired into the `GET /api/dashboard/widgets`
registry as `STUDENT_MY_DASHBOARD`. New menu catalog: `STUDENT_PORTAL` main menu (My Dashboard/My
Profile/My Timetable/My Enrollment History), the **first real `MenuFor = USER` audience** rows in
this codebase — every menu before this round was hardcoded `MenuFor = ADMIN`
(`MenuSeeder.MainMenu`/`SubMenu` now accept an optional `menuFor`, defaulting to Admin so nothing
existing changes). No migration — `Menu.MenuFor` already existed as a column. Full reference:
`Docs/teacher_student_portal_implementation_guide.md`.

---

# Seeded data (first run against a migrated DB)

- Roles `SuperAdmin` / `Admin` / `User`, one account per role (credentials from the `Seed` config section). A fourth role, `Student`, is also seeded (2026-07-27) with no seeded account and zero permissions — it's assigned automatically to every student portal account provisioned via `registerUserAccount`/`register-account`.
- Main menus `DASHBOARD` / `USER_MANAGEMENT` / `CONFIG_MANAGEMENT` / `SETUP` / `STUDENT_MANAGEMENT` / `ACCOUNTS` (2026-08-03 — merges the former `FEE_MANAGEMENT`/`PAYROLL_MANAGEMENT` mains; holds Fee Generation, Salary Generation, Salary Calculator) / `EMPLOYEE_MANAGEMENT` / `CALENDAR_MANAGEMENT` / `LEAVE_MANAGEMENT` / `LOGS` (2026-07-28 — System Access Logs/Error Logs, moved out of `DASHBOARD`) / `MY_WORKSPACE` (2026-08-06, Employee/Teacher self-service) / `STUDENT_PORTAL` (2026-08-21, Student self-service — the first `MenuFor = USER` audience menu tree) / `EXAM_MANAGEMENT` (2026-07-28 — Exam Terms/Exams/Grade Scales/Marks Entry/Exam Results/Student Promotions; gained Exam Rooms/Hall Arrangements 2026-07-29) with permission leaves covering every protected endpoint (`ACADEMIC_MANAGEMENT`/`TEACHER_MANAGEMENT` retired 2026-07-16 — their contents live under `SETUP`/`EMPLOYEE_LIST`; `FEE_MANAGEMENT`/`PAYROLL_MANAGEMENT` retired 2026-08-03 — their contents live under `ACCOUNTS`); **all permissions granted to the SuperAdmin role** — and SuperAdmin-typed accounts additionally bypass the permission check entirely, so the seeded superadmin works everywhere immediately.
- Config catalogs for student management (`typeCode` 1001–1007) plus discount/scholarship/fee-category types (`1008`/`1009`/`1010`, fee categories carrying their normative `fee_frequency`) plus employee-category/job-position/salary-component/deduction/insurance-type (`1011`–`1015`) plus salary/fee adjustment types (`1016`/`1017`) plus SSF rates (`1018`, employee/employer share percentages in `additionalValue1`) plus branch/province/employee-level (`1019`–`1021`, province seeded with Nepal's 7 federal provinces) plus district/local-level (`1022`/`1023`, all 77 districts and 80 of 753 local levels — see `employee_address_implementation_guide.md` before relying on the local-level list for real addresses); default guardian-relationship, teacher-qualification, document-type (teacher + student), discount/scholarship-type (with default rates), all 11 fee-category options, and all employee-side options (categories, positions, salary components, deductions, insurance types with tax-deduction caps); a baseline of app-config settings (`GENERAL`/`THEME`/`ANNOUNCEMENT`, including `FEE_DUE_DAY_OF_MONTH`); one placeholder `FY-SAMPLE` fiscal year with illustrative Individual/Couple tax slabs and retirement-exemption cap (verify before real payroll use); one default `DocumentTemplate` HTML row per type (Payslip/FeeReceipt/StudentIdCard/TeacherIdCard) so the preview endpoints work out of the box; BS calendar reference data (12 month names + 7 weekday names EN/NP with Saturday as the weekly holiday, and the BS 2000–2090 month-length table) so the dual-calendar endpoints work out of the box; baseline `LeaveType` rows (Annual/Sick/Casual, 18/12/12 days, illustrative — verify against actual policy); one illustrative full school day of `TimePeriod` rows (8 periods + Short Break + Lunch Break, 2026-08-03), **not** mapped to any class — that's an admin decision via `POST /api/timeperiods/map`, see `Docs/time_period_and_class_routine_implementation_guide.md`.
- `Admin`/`User`/`Student` roles start with **zero** permissions; grant via `POST /api/roles/claims` while signed in as superadmin.

# Error-handling checklist for the UI

1. Read `responseCode` from the envelope on every response, including non-2xx ones.
2. `VALIDATION_ERROR` → show `responseMessage` (all failures pre-joined).
3. 401 anywhere except login → one refresh attempt, one retry; on refresh failure clear tokens, go to login.
4. 403 → access-denied screen; don't retry, don't refresh.
5. Store the token pair deliberately (mobile secure storage; on web prefer memory + HttpOnly-cookie BFF, or accept the XSS tradeoff of localStorage knowingly).
6. After `change-password` / `reset-password` / `logout`, force fresh login — the server already revoked every session.
