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

Shortcut links to the feature list pages (menu catalog `SUB_MENU` rows) the **current logged-in user** actually has permission to open — resolved the same way as `GET /api/roles/user-menus`, filtered to visible rows that carry a `url`. Zero grants returns an empty list, not a 403.

**Response** (`200`): `data` =
```json
[
  { "id": 12, "code": "STUDENT_LIST", "displayName": "Students", "url": "/apps/student/list", "icon": "icons.student", "order": 1 },
  { "id": 20, "code": "TEACHER_LIST", "displayName": "Teachers", "url": "/apps/teacher/list", "icon": "icons.teacher", "order": 2 }
]
```
**Failure**: `401 UNAUTHORIZED` if not authenticated; `404 NOT_FOUND` if the caller's user row can't be found.

Full request/response reference and design notes for these six endpoints: `Docs/dashboard_updated_file.md`.

---

# Student Management (2026-07-12, restructured 2026-07-13)

Full sub-system for academic years, classes (with sections), subjects, teachers, guardians, students, and enrollments — **complete reference with request/response JSON, invariants, and UI flows in `student_management_implementation_guide.md`**, and the 2026-07-13 breaking changes (class/section split, enrollment-per-year guard, guardian onboarding) detailed in `class_section_structure_implementation_guide.md`; this is the orientation summary:

- **Dropdown catalogs, not tables**: Grade (`typeCode 1001`), Section (`1002`), Subject (`1003`), Guardian Relationship (`1004`), Teacher Qualification (`1005`) live in the Config catalog and are referenced everywhere by their `code` string. Populate via `GET /api/configs/dropdown/{typeCode}`; relationship + qualification options are seeded, grade/section/subject options must be created by the admin before classes can exist. An unknown code → `400 VALIDATION_ERROR`.
- **Endpoints** (all permission-gated, standard envelope):
  - `/api/academicyears` — CRUD; unique `code`; one `isCurrent` year (setting it demotes the others); code immutable on update. `POST /{id}/clone-structure` copies another year's classes/sections/subject mappings into it (existing grades skipped) — the one-click new-year setup.
  - `/api/academicclasses` — CRUD + `/{id}/sections` (add/list/update/remove sections; capacity lives on the section, `0` = unlimited) + `/{id}/subjects` (assign/list/remove class subjects — **shared by every section of the class**; an *optional* subject may instead be scoped to a single section via `classSectionId`, and `GET …/subjects?classSectionId=…` returns one section's effective list); a class = year+grade (unique pair, immutable after create), with its `sections` nested in every response.
  - `/api/teachers` — CRUD (`employeeCode` optional on create — blank = auto-generated `EMP{year}{seq}`; unique/immutable, `search` filter) + `/{id}/qualifications` (qualification records: code from catalog 1005 + course/institution/year/score) + `/{id}/assignments` (link to a classSubject, optionally narrowed to one `classSectionId` — null = all sections; `isClassTeacher` requires a section, at most one class teacher per **section**) + `/{id}/documents` (multipart upload of PDF/JPG/PNG ≤10 MB, type from catalog 1006, optional `validUntil` expiry; list/download/delete — see `teacher_documents_implementation_guide.md`). The teacher detail response adds `serviceHistory` (assignments with academic years, oldest first). **2026-07-15**: a `Teacher` is now a thin teaching-specific profile (`teachingLicenseNo`/`experienceYears`/`specialization`) sharing its id with an `Employee` row that owns identity/HR/bank fields — `POST /api/teachers` creates both together, and `TeacherDto` still returns one flattened object so this is not a breaking response shape, just a larger one (adds `gender`, `dateOfBirth`, `jobPositionCode`, `employmentStatus`, `bankName`, `bankAccountNumber`, `paymentMode`). See "Employee Management & Payroll" below.
  - `/api/guardians` — CRUD; standalone records shared across students.
  - `/api/students` — CRUD (`admissionNo` optional on create — blank = auto-generated `ADM{year}{seq}`; unique/immutable, `search` filter); `POST` accepts an optional `guardians` array (existing `guardianId` or inline new-guardian fields, `relationshipCode`, at most one `isPrimary`) so onboarding captures guardians in one call; `PUT` takes the same `guardians` list with three-way semantics (null = unchanged, `[]` = unlink all, list = replace-sync); the detail response returns `guardians` inline plus `currentEnrollment` (current year/grade/section/roll + subjects studying, each subject with its `teacherName`) and `enrollmentHistory` (all enrollments, oldest year first — see `profile_history_and_documents_implementation_guide.md`); `/{id}/guardians` still links/unlinks individually (one primary per student, auto-demoted); `/{id}/documents` mirrors teacher documents (multipart upload, catalog 1007, list/download/delete).
  - `/api/enrollments` — CRUD against a **section** (`classSectionId`; unique student+section; **one active enrollment per student per academic year**; per-section capacity and roll-number uniqueness; student/section immutable — move = status `2` Transferred + new enrollment) + `/{id}/subjects/{classSubjectId}` electives (non-mandatory subjects of the enrollment's class only). Rows flatten grade/section/year ids and codes.
- **Enums**: `status` on records — `1` Active / `2` Inactive; enrollment `status` — `1` Enrolled / `2` Transferred / `3` Withdrawn / `4` Completed.
- **No logins for students/teachers** — they're records only; account linkage is a later phase.
- Deletes of the main records are **soft** (codes/pairs stay reserved → clean `409` on re-create); child links (class subjects, assignments, guardian links, electives) are hard deletes, refused with `409` while dependents exist.
- **Grading metadata (2026-07-15)**: `ClassSubject` (and `POST`/new `PUT /api/academicclasses/{id}/subjects/{classSubjectId}`) gained optional `creditHours`/`fullMarks`/`passMarks`/`theoryMarks`/`practicalMarks` — see `student_management_implementation_guide.md`.

---

# Fee, Discount & Scholarship Management (2026-07-15, redesigned three times same day)

Full reference in `fee_management_implementation_guide.md`; orientation summary: `/api/feestructures` is a **header per class** (`academicClassId` unique) owning a child `items` array — `POST /api/feestructures` creates the header **and every submitted item in one call** (`{ academicClassId, items: [{ feeCategoryCode, amount, frequencyType, isOptional, isRefundable }, ...] }`), fixing the earlier one-call-per-category flow. `feeCategoryCode` must be a known option in the `FeeCategory` Config catalog (`1010`, all 11 "permitted" categories seeded: Tuition/Annual/Admission/Deposit/Examination/Computer/SpecialTraining/Hostel/Meal/Transportation/EducationalTour) — this is the **authoritative whitelist**, not a free-text field (a same-day detour briefly allowed free-text `name` here; reverted the same day). A school needing a category outside the 11 seeded ones adds it via plain `POST /api/configs` first, then references its code. `/api/feestructures/{id}/items` (`POST`/`PUT /{itemId}`/`DELETE /{itemId}`) manages items one at a time after the initial bulk create — `feeCategoryCode` is immutable once set. `/api/enrollments/{id}/fee-selections/{feeStructureItemId}` — how an enrollment opts into an optional item (by its id — a class can only charge a given category once, but the selection still keys off the item's own id). `/api/enrollments/{id}/discounts` and `/{id}/scholarships` — awards (percentage or fixed amount) against a specific enrollment, unaffected by any of the fee-structure redesigns; discount/scholarship "reason" is a Config code (catalog `1008`/`1009`, admin-extensible — this is the configurable eligibility-criteria mechanism, e.g. class topper, exam merit, social category, sibling) that can carry a **global default rate**; omit `valueType`/`value` on the award to use it, or supply both for an individual override. `GET /api/enrollments/{id}/fee-structure` composes all of this into one priced view for the student detail page (fee items + discounts + scholarships + a frequency-grouped summary). `GET /api/enrollments/scholarships/summary` (and the discount equivalent) report student counts per type. Discounts/scholarships are soft-deleted (financial-audit records); fee structure items and fee-selections are hard-deleted, refused with `409` while an enrollment still references an item.

---

# Employee Management & Component-Based Payroll (2026-07-15, redesigned same day from teacher-only)

Full reference in `employee_management_implementation_guide.md`; orientation summary: every staff member (teacher, principal, accountant, receptionist, librarian, IT officer, driver, security guard, office assistant, cleaner, office help) is now an `Employee` (`/api/employees` — CRUD, `employeeCode` optional/auto-generated `EMP{year}{seq}`, filters by category/position/status/gender/date-range/search/phone). `employeeCategoryCode`/`jobPositionCode` are Config codes (catalog `1011`/`1012`); `employmentStatus` is a 6-value enum (Active/OnLeave/Suspended/Resigned/Terminated/Retired). `Teacher` (`/api/teachers`) is a thin teaching-profile sharing its id with an `Employee` row — `POST /api/teachers` creates both together; `POST /api/employees/{id}/teacher-profile` promotes an existing employee instead (must be Academic category + Teacher/Principal/Vice Principal position).

**Compensation plan** — `/api/employees/{id}/salaries` (also reachable via the unchanged `/api/teachers/{id}/salaries` alias): one row per salary revision, each holding named **components** (income — `componentCode` from catalog `1013`, e.g. `BASIC`/`SSF_CONTRIBUTION`/allowances; `valueType` Fixed or Percentage-of-`BASIC`; `frequencyType` Monthly/Annual/OneTime; `isTaxable`/`isRetirementContribution` flags), **deductions** (catalog `1014`, same shape minus `isTaxable`), and **insurance premiums** (catalog `1015`: Life/Health/Housing, each with a configured tax-deduction cap). `GET .../salaries/tax-calculation?fiscalYearId=` runs the full computation: gross annual taxable income → Nepal's retirement-fund "least of three" exemption (actual contributions vs. ⅓ of gross vs. the fiscal year's configured cap) → capped insurance deduction → the existing progressive `TaxSlab` bracket walker → annual/monthly tax and net pay.

`/api/fiscalyears` — a payroll-specific year concept (separate from `AcademicYear`, since Nepal's government fiscal year doesn't align with the school's academic calendar), with nested `/{id}/taxslabs` (progressive Individual/Couple tax brackets, `taxRate` as a fraction) and a `retirementExemptionCapAmount` field (the configurable "C" in the exemption rule). Full fiscal-year/tax-slab reference stays in `payroll_implementation_guide.md`.

**The seeded `FY-SAMPLE` fiscal year/slabs/retirement cap and the seeded insurance-type caps are illustrative placeholders** — verify against the current government of Nepal budget before relying on them for real payroll.

---

# Document Preview (payslip / fee receipt / ID cards) (2026-07-15)

Full reference in `document_preview_implementation_guide.md`; orientation summary: an admin edits an HTML template per document type via `/api/documenttemplates` (`templateType` unique: `1` Payslip / `2` FeeReceipt / `3` StudentIdCard / `4` TeacherIdCard) containing `{{Token}}` placeholders; `GET /api/documenttemplates/placeholders/{templateType}` returns the backend-authoritative list of tokens each type supports. Four preview endpoints compute the real data and return the fully-substituted HTML string, ready to display/print — no PDF generation, the frontend prints via the browser:

- `GET /api/employees/{id}/salaries/payslip-preview?fiscalYearId=` (and the `/api/teachers/{id}/salaries/payslip-preview` alias) — always the employee's **latest** salary revision.
- `GET /api/enrollments/{id}/fee-receipt-preview` — same composed data as `GET /api/enrollments/{id}/fee-structure`.
- `GET /api/students/{id}/id-card-preview` and `GET /api/teachers/{id}/id-card-preview`.

All five return `CommonResponse<{ templateType, html }>`; `404` if no template is configured for that type yet (a default is seeded on first boot for every type). No photo/image support today — `StudentDto`/`TeacherDto` have no photo field.

---

# Pay & Taxes (2026-07-15)

Full reference in `pay_and_taxes_implementation_guide.md`; orientation summary: three additions on top of the existing compensation-plan endpoints above, all `Employees`/`Teachers`-aliased as usual. `GET /api/employees/{id}/salaries/tax-calculation/monthly?fiscalYearId=` returns the same annual `taxCalculation` plus `months`: 12 fiscal-month rows (`{ monthIndex, monthName, periodStartDate, periodEndDate, monthDays, incomeLines, deductionLines, monthGrossIncome, monthTax, monthNet }`) — fiscal-month boundaries are an **approximation** (`FiscalYear.startDate`..`endDate` split into 12 equal Gregorian segments, labeled Shrawan..Ashad) and `monthTax` is a **flat** `annualTax / 12` every row, not a cumulative rest-of-year re-projection. `GET /api/employees/{id}/payslips?fiscalYearId=` lists only fiscal months whose pay period has already started (`PayslipSummaryDto[]`, `payDays`/`upl` simplified — no attendance module exists); `GET /api/employees/{id}/payslips/{fiscalYearId}/{monthIndex}` returns the structured line-item detail behind it (`PayslipDetailDto`) — a separate, non-HTML path from the existing `.../payslip-preview`. `POST /api/employees/{id}/loans` / `GET .../loans` / `POST .../loans/{loanId}/approve|reject|cancel` manage a request → approve/reject/cancel workflow (`EmployeeLoanDto`, `LoanStatus` 1–5); repayment progress (`amountRepaid`/`remainingBalance`/`isFullyRepaid`) is computed from `startDate`/`emiAmount`/`principalAmount` against today, not stored, and an `Approved` loan's EMI is automatically folded into the Payslip/Tax-Details deduction lines for any month on/after `startDate` — no separate "activate deduction" step. **`dbo.employee_loans` needs a migration that doesn't exist yet** — every `/loans` endpoint 500s until it's applied.

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
- **fee_frequency**: a FeeCategory (catalog 1010) option's `additionalValue1` must now be `MONTHLY`/`ANNUAL`/`ONE_TIME` — it drives generation defaults and is validated on create/update.
- **Needs a migration that doesn't exist yet**: 10 new tables — every endpoint in this section 500s until it's applied. The 2026-07-17 fee-module fixes additionally need `fee_structure_items.installment_count` (nullable int).

---

# Payroll fixes & Salary Calculator (2026-07-18)

Full reference in `payroll_fixes_implementation_guide.md`; orientation summary (no new migration needed):

- **Run list totals fixed**: `GET /api/payrollruns` now returns real `slipCount`/`totalGrossEarnings`/`totalNetPay` (they were always 0). Cancelled slips are excluded from these aggregates everywhere — they still appear in the detail's `slips` array (`status: 4`), so render them greyed out.
- **`POST /api/payrollruns/{id}/refresh`** (`PAYROLL_RUN_REFRESH`): Draft-only in-place regeneration — picks up compensation-plan edits, tax-slab changes, newly Pending adjustments and newly approved loans made after generation. Preserves Manual slip lines and individually-cancelled slips; adds slips for newly eligible employees; cancels slips of no-longer-payable ones; response = same `{ run, skipped[] }` shape as create. Add a Refresh button on the Run Detail page while `status` is Draft. This is also the remedy when a run seems to use stale tax slabs: runs are immutable snapshots — fix the fiscal year's slabs, then Refresh.
- **SSF rates catalog 1018** (`GET /api/configs/dropdown/1018`, no grant needed): `EMPLOYEE_SHARE` (11) / `EMPLOYER_SHARE` (20), the percentage in `additionalValue1`, admin-editable. Prefill SSF lines in the Add Salary Revision form from it instead of hardcoding. Correct plan shape: `SSF_CONTRIBUTION` component = employer 20% of Basic (taxable, retirement-flagged); `SSF_DEDUCTION` deduction = employee 11% of Basic (retirement-flagged).
- **Employer-share payslip offset (round 2)**: a retirement-flagged *component* (the employer SSF/EPF share) now automatically produces an equal deduction line with the same code on payslips/slips/monthly breakdowns — it's fund money, not cash, so it stays in gross but no longer inflates net pay. Render the pair under earnings and deductions as-is.
- **`POST /api/salarycalculator`** (`SALARY_CALCULATOR`, new sub-menu under Payroll Management, url `/apps/payroll/salary-calculator`): HR structuring tool — fix one monthly figure (`basis`: 1 NetPayment / 2 GrossPayment / 3 Ctc) plus `amount`, optional `fiscalYearId`/`assessmentType`/`includeSsf`, Basic pinned exactly (`basicSalaryAmount`) or as a percent (`basicPercentOfGross`, default 60), plus round-2 knobs `annualBonusAmount` (Dashain/festival bonus — taxed annually, excluded from monthly cash), `monthlyCitAmount` (CIT savings, retirement-flagged), `annualLifeInsurancePremium`/`annualHealthInsurancePremium` (capped per catalog 1015). Returns the solved structure (Basic, Other Allowance, both SSF shares, CIT, monthly TDS via the year's real slabs, net, CTC, annuals incl. bonus, full `taxCalculation` breakdown) plus `suggestedComponents`/`suggestedDeductions`/`suggestedInsurancePremiums` shaped exactly like `POST /api/employees/{id}/salaries` line inputs.
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

# Seeded data (first run against a migrated DB)

- Roles `SuperAdmin` / `Admin` / `User`, one account per role (credentials from the `Seed` config section).
- Main menus `DASHBOARD` / `USER_MANAGEMENT` / `CONFIG_MANAGEMENT` / `SETUP` / `STUDENT_MANAGEMENT` / `FEE_MANAGEMENT` / `PAYROLL_MANAGEMENT` / `EMPLOYEE_MANAGEMENT` / `CALENDAR_MANAGEMENT` with permission leaves covering every protected endpoint (`ACADEMIC_MANAGEMENT`/`TEACHER_MANAGEMENT` retired 2026-07-16 — their contents live under `SETUP`/`EMPLOYEE_LIST`); **all permissions granted to the SuperAdmin role** — and SuperAdmin-typed accounts additionally bypass the permission check entirely, so the seeded superadmin works everywhere immediately.
- Config catalogs for student management (`typeCode` 1001–1007) plus discount/scholarship/fee-category types (`1008`/`1009`/`1010`, fee categories carrying their normative `fee_frequency`) plus employee-category/job-position/salary-component/deduction/insurance-type (`1011`–`1015`) plus salary/fee adjustment types (`1016`/`1017`) plus SSF rates (`1018`, employee/employer share percentages in `additionalValue1`); default guardian-relationship, teacher-qualification, document-type (teacher + student), discount/scholarship-type (with default rates), all 11 fee-category options, and all employee-side options (categories, positions, salary components, deductions, insurance types with tax-deduction caps); a baseline of app-config settings (`GENERAL`/`THEME`/`ANNOUNCEMENT`, including `FEE_DUE_DAY_OF_MONTH`); one placeholder `FY-SAMPLE` fiscal year with illustrative Individual/Couple tax slabs and retirement-exemption cap (verify before real payroll use); one default `DocumentTemplate` HTML row per type (Payslip/FeeReceipt/StudentIdCard/TeacherIdCard) so the preview endpoints work out of the box; BS calendar reference data (12 month names + 7 weekday names EN/NP with Saturday as the weekly holiday, and the BS 2000–2090 month-length table) so the dual-calendar endpoints work out of the box.
- `Admin`/`User` roles start with **zero** permissions; grant via `POST /api/roles/claims` while signed in as superadmin.

# Error-handling checklist for the UI

1. Read `responseCode` from the envelope on every response, including non-2xx ones.
2. `VALIDATION_ERROR` → show `responseMessage` (all failures pre-joined).
3. 401 anywhere except login → one refresh attempt, one retry; on refresh failure clear tokens, go to login.
4. 403 → access-denied screen; don't retry, don't refresh.
5. Store the token pair deliberately (mobile secure storage; on web prefer memory + HttpOnly-cookie BFF, or accept the XSS tradeoff of localStorage knowingly).
6. After `change-password` / `reset-password` / `logout`, force fresh login — the server already revoked every session.
