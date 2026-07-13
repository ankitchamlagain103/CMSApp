# CMSApp — Patch: `roleIds` on Create/Update User (UI)

**What changed**: `POST /api/users` (register/create) and `PUT /api/users/{id}` (update) now accept an optional `roleIds` array — a list of role **guids** — directly in the request body. Roles are assigned/synced from it in the same call; the separate `POST /api/roles/users` calls are no longer needed for these two flows. Everything else about both endpoints is unchanged.

Get the available role ids from `GET /api/roles` (each item's `id`).

**Update (2026-07-10)**: `UserDto` now also has a **`roleIds` response field** — every user endpoint (`GET /api/users/{id}`, `GET /api/users`, and the create/update responses) returns the user's current role guids as `"roleIds": ["...", ...]` (empty array = no roles). Use it to pre-select the roles multi-select on the edit form directly from `GET /api/users/{id}`; no separate role lookup per user is needed.

## POST /api/users — create

New optional field on the existing body:

```json
{
  "userName": "jdoe",
  "email": "jdoe@example.com",
  "password": "Str0ng!Pass",
  "firstName": "John",
  "lastName": "Doe",
  "isTosAgreed": true,
  "roleIds": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"]
}
```

Behavior:

| `roleIds` sent as | Result |
|---|---|
| omitted or `[]` | Previous behavior — the default `User` role is assigned |
| `["...guid...", ...]` | The user is created with **exactly those roles** (default `User` role is *not* added on top — include its id if you want it) |

Duplicated ids in the list are ignored. All ids are checked **before** the user is created — one unknown id fails the whole request and no user is saved.

## PUT /api/users/{id} — update

Same field, with three-way semantics — **omitting the field and sending an empty array are different**:

| `roleIds` sent as | Result |
|---|---|
| omitted (field absent) | Roles are **left unchanged** — pure profile update, same as before this patch |
| `[]` (empty array) | **Every role is removed** from the user |
| `["...guid...", ...]` | Roles are **replaced** to exactly this set (missing ones added, extra ones removed) |

So a UI form that doesn't manage roles should simply not send the field. All ids are checked before any profile field is written — an unknown id fails the whole request with no partial update.

## Failures (both endpoints)

| HTTP | `responseCode` | When / message |
|---|---|---|
| 404 | `NOT_FOUND` | `"Role with id '...' was not found."` (also unknown user id on update) |
| 400 | `VALIDATION_ERROR` | `"RoleIds must not contain an empty guid."` (all-zero guid in the list), plus all pre-existing field validations |
| 400 | `CONFLICT` | Unchanged — duplicate username/email on create |

## Security note for the team — resolved 2026-07-12

~~`POST /api/users` is **anonymous** (it's the public registration endpoint), so anyone could self-register with any existing role.~~ **This hole is closed: there is no public self-registration anymore.** `POST /api/users` now requires a valid JWT plus the `USER_CREATE` permission (or SuperAdmin), the same as every other user-management endpoint. `roleIds` on create is therefore admin-gated exactly like it already was on update (`USER_UPDATE`). A role-selection control on the (admin) create-user form is now fine to expose.

Consequences for the UI:

- There must be **no public "register" page** — users are created by admins from the user-management screen.
- An unauthenticated `POST /api/users` gets `403 FORBIDDEN` (standard envelope), and the endpoint is rate-limited like the rest of the API surface.
- The create response is unchanged; the new user still receives a verification email and must verify before logging in.
