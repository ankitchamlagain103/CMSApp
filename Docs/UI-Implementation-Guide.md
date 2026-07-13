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
{ "id": "c2222222-0000-0000-0000-000000000001", "name": "Editor", "description": "Can edit content" }
```

## POST /api/roles

**Request** (`name` required ≤256; `description` optional ≤500):
```json
{ "name": "Editor", "description": "Can edit content" }
```

**Response** (`200`):
```json
{ "responseCode": "SUCCESS", "responseMessage": "Role created successfully.", "data": { "id": "c2222222-...", "name": "Editor", "description": "Can edit content" } }
```

**Failures** (`400`): `CONFLICT` `"Role 'Editor' already exists."` or `VALIDATION_ERROR`.

## GET /api/roles/{id}

**Response** (`200`): `data` = `RoleDto`. **Failure**: `404 NOT_FOUND` `"Role with id '...' was not found."`

## GET /api/roles?page=1&pageSize=20

**Response** (`200`): same pagination wrapper as users, `items` = `RoleDto[]`, ordered by name.

## PUT /api/roles/{id}

**Request**: same body as create. Renaming re-checks uniqueness.

**Response** (`200`): `data` = updated `RoleDto`, `"Role updated successfully."` **Failures**: `404 NOT_FOUND`; `400 CONFLICT` on duplicate name; `400 VALIDATION_ERROR`.

## DELETE /api/roles/{id}

**Response** (`200`): `{ ..., "responseMessage": "Role deleted successfully.", "data": true }` — **hard** delete (unlike users). **Failure**: `404 NOT_FOUND`.

## GET /api/roles/user-menus — the current user's permitted menu tree

Identifies the caller from the JWT (no `userId` parameter). Returns every menu granted to any of the caller's roles, plus the parent `SUB_MENU`/`MAIN_MENU` nodes above them, assembled into a tree. This is the endpoint to call after login to render navigation. Any authenticated user may call it (listed in `DefaultEnabledMenu` — no permission row needed); a user whose roles have no grants gets an empty `data` array, not an error.

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

**Response** (`200`): `{ ..., "responseMessage": "Role assigned to user successfully.", "data": true }` **Failures**: `404 NOT_FOUND` (user or role missing); `400 CONFLICT` `"This user is already in the role."`; `400 VALIDATION_ERROR`.

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

**Failures**: `404 NOT_FOUND` (role or menu missing); `400 CONFLICT` `"This menu is already assigned to the role."`; `400 VALIDATION_ERROR`.

## DELETE /api/roles/{roleId}/claims/{menuId}

**Response** (`200`): `{ ..., "responseMessage": "Menu removed from role successfully.", "data": true }` **Failure**: `404 NOT_FOUND` `"This menu is not assigned to the role."`

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

## GET /api/menus?page=1&pageSize=20&menuType=MAIN_MENU&menuFor=ADMIN

`menuType` and `menuFor` are **optional, independent filters** — omit either (or both) to not filter on it. Allowed values are the same enums as create: `menuType` = `MAIN_MENU` | `SUB_MENU` | `PERMISSION`, `menuFor` = `ADMIN` | `USER` | `BOTH` (exact match — a menu created as `BOTH` is only returned by `menuFor=BOTH`, not by `menuFor=ADMIN`). Results are sorted by `order`.

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

## GET /api/configs/dropdown/{typeCode} — options for one dropdown

Available to **any authenticated user** (no permission grant needed). Returns the **common dropdown shape** (`DropdownItemDto`) — every dropdown endpoint in the system uses this same shape, so one frontend select component can bind them all: render `label`, submit `value`.

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

**Response** (`200`): `{ ..., "responseMessage": "Config deleted successfully.", "data": true }` — **hard** delete. **Failure**: `404 NOT_FOUND` `"Config with id '...' was not found."`

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

---

# Student Management (2026-07-12, restructured 2026-07-13)

Full sub-system for academic years, classes (with sections), subjects, teachers, guardians, students, and enrollments — **complete reference with request/response JSON, invariants, and UI flows in `student_management_implementation_guide.md`**, and the 2026-07-13 breaking changes (class/section split, enrollment-per-year guard, guardian onboarding) detailed in `class_section_structure_implementation_guide.md`; this is the orientation summary:

- **Dropdown catalogs, not tables**: Grade (`typeCode 1001`), Section (`1002`), Subject (`1003`), Guardian Relationship (`1004`), Teacher Qualification (`1005`) live in the Config catalog and are referenced everywhere by their `code` string. Populate via `GET /api/configs/dropdown/{typeCode}`; relationship + qualification options are seeded, grade/section/subject options must be created by the admin before classes can exist. An unknown code → `400 VALIDATION_ERROR`.
- **Endpoints** (all permission-gated, standard envelope):
  - `/api/academicyears` — CRUD; unique `code`; one `isCurrent` year (setting it demotes the others); code immutable on update. `POST /{id}/clone-structure` copies another year's classes/sections/subject mappings into it (existing grades skipped) — the one-click new-year setup.
  - `/api/academicclasses` — CRUD + `/{id}/sections` (add/list/update/remove sections; capacity lives on the section, `0` = unlimited) + `/{id}/subjects` (assign/list/remove class subjects — **shared by every section of the class**; an *optional* subject may instead be scoped to a single section via `classSectionId`, and `GET …/subjects?classSectionId=…` returns one section's effective list); a class = year+grade (unique pair, immutable after create), with its `sections` nested in every response.
  - `/api/teachers` — CRUD (`employeeNo` unique/immutable, `search` filter) + `/{id}/qualifications` (qualification records: code from catalog 1005 + course/institution/year/score) + `/{id}/assignments` (link to a classSubject, optionally narrowed to one `classSectionId` — null = all sections; `isClassTeacher` requires a section, at most one class teacher per **section**) + `/{id}/documents` (multipart upload of PDF/JPG/PNG ≤10 MB, type from catalog 1006, optional `validUntil` expiry; list/download/delete — see `teacher_documents_implementation_guide.md`).
  - `/api/guardians` — CRUD; standalone records shared across students.
  - `/api/students` — CRUD (`admissionNo` unique/immutable, `search` filter); `POST` accepts an optional `guardians` array (existing `guardianId` or inline new-guardian fields, `relationshipCode`, at most one `isPrimary`) so onboarding captures guardians in one call; `PUT` takes the same `guardians` list with three-way semantics (null = unchanged, `[]` = unlink all, list = replace-sync); the detail response returns `guardians` inline plus `currentEnrollment` (current year/grade/section/roll + subjects studying — see `student_profile_enhancements_implementation_guide.md`); `/{id}/guardians` still links/unlinks individually (one primary per student, auto-demoted).
  - `/api/enrollments` — CRUD against a **section** (`classSectionId`; unique student+section; **one active enrollment per student per academic year**; per-section capacity and roll-number uniqueness; student/section immutable — move = status `2` Transferred + new enrollment) + `/{id}/subjects/{classSubjectId}` electives (non-mandatory subjects of the enrollment's class only). Rows flatten grade/section/year ids and codes.
- **Enums**: `status` on records — `1` Active / `2` Inactive; enrollment `status` — `1` Enrolled / `2` Transferred / `3` Withdrawn / `4` Completed.
- **No logins for students/teachers** — they're records only; account linkage is a later phase.
- Deletes of the main records are **soft** (codes/pairs stay reserved → clean `409` on re-create); child links (class subjects, assignments, guardian links, electives) are hard deletes, refused with `409` while dependents exist.

---

# Seeded data (first run against a migrated DB)

- Roles `SuperAdmin` / `Admin` / `User`, one account per role (credentials from the `Seed` config section).
- Main menus `USER_MANAGEMENT` / `ROLE_MANAGEMENT` / `MENU_MANAGEMENT` / `CONFIG_MANAGEMENT` / `DASHBOARD` / `ACADEMIC_MANAGEMENT` / `TEACHER_MANAGEMENT` / `STUDENT_MANAGEMENT` with permission leaves covering every protected endpoint; **all permissions granted to the SuperAdmin role** — and SuperAdmin-typed accounts additionally bypass the permission check entirely, so the seeded superadmin works everywhere immediately.
- Config catalogs for student management (`typeCode` 1001–1006) plus default guardian-relationship, teacher-qualification, and document-type options; a baseline of app-config settings (`GENERAL`/`THEME`/`ANNOUNCEMENT`).
- `Admin`/`User` roles start with **zero** permissions; grant via `POST /api/roles/claims` while signed in as superadmin.

# Error-handling checklist for the UI

1. Read `responseCode` from the envelope on every response, including non-2xx ones.
2. `VALIDATION_ERROR` → show `responseMessage` (all failures pre-joined).
3. 401 anywhere except login → one refresh attempt, one retry; on refresh failure clear tokens, go to login.
4. 403 → access-denied screen; don't retry, don't refresh.
5. Store the token pair deliberately (mobile secure storage; on web prefer memory + HttpOnly-cookie BFF, or accept the XSS tradeoff of localStorage knowingly).
6. After `change-password` / `reset-password` / `logout`, force fresh login — the server already revoked every session.
