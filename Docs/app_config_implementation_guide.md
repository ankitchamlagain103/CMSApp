# CMSApp — App Config Implementation Guide (UI)

App Configs are the **application-wide settings store**: a flat key/value table (`app_configs`) that drives things like the app name, theme selection, primary/secondary brand colors, logo URL, footer text — anything the UI should be able to change **without a redeploy**. An admin edits the values through the CRUD endpoints below; every visitor's UI reads them through one public bootstrap endpoint.

This guide follows the same conventions as `UI-Implementation-Guide.md` (read its "Conventions" section first): every response is wrapped in the common envelope, JSON is camelCase, and protected endpoints need `Authorization: Bearer {accessToken}`.

---

## The data model

One row per setting:

| Field | Type | Notes |
|---|---|---|
| `id` | guid | Row id — used for update/delete |
| `configParam` | string ≤ 256 | The **key**, unique across the whole table (e.g. `APP_NAME`) |
| `configValue` | string ≤ 555 | The value, always a string — the UI parses it (`"#4F46E5"`, `"true"`, `"DARK"`) |
| `configGroup` | string ≤ 256, **optional** | Free-form grouping bucket for the admin screen and for fetching related settings together (e.g. `GENERAL`, `THEME`). May be omitted/null — an ungrouped row is fine, it just never shows up in any `group/{configGroup}` lookup |
| `isEnable` | bool | Master switch: disabled rows are kept but **excluded from the public endpoint** |

`AppConfigDto` (the admin CRUD shape):

```json
{
  "id": "b2222222-0000-0000-0000-000000000001",
  "configParam": "PRIMARY_COLOR",
  "configValue": "#4F46E5",
  "configGroup": "THEME",
  "isEnable": true
}
```

`PublicAppConfigDto` (the anonymous bootstrap shape — no `id`, no `isEnable`):

```json
{
  "configParam": "PRIMARY_COLOR",
  "configValue": "#4F46E5",
  "configGroup": "THEME"
}
```

### Seeded baseline params (2026-07-12)

`AppConfigSeeder` now seeds these 17 rows at startup (idempotent, **create-if-missing only** — values an admin edits are never overwritten by a restart). The backend doesn't care what keys exist beyond these; use `SCREAMING_SNAKE_CASE` params and keep related settings in one `configGroup` when adding more:

| `configGroup` | `configParam` | Seeded `configValue` | UI use |
|---|---|---|---|
| `GENERAL` | `APP_NAME` | `CMSApp` | Title bar, login page, `document.title` |
| `GENERAL` | `APP_TAGLINE` | `Content Management System` | Login page subtitle |
| `GENERAL` | `FOOTER_TEXT` | `© 2026 CMSApp` | Footer |
| `GENERAL` | `SUPPORT_EMAIL` | `support@cmsapp.local` | "Contact support" links |
| `THEME` | `THEME_MODE` | `SYSTEM` | `LIGHT` / `DARK` / `SYSTEM` default |
| `THEME` | `PRIMARY_COLOR` | `#4F46E5` | CSS `--color-primary` |
| `THEME` | `SECONDARY_COLOR` | `#EC4899` | CSS `--color-secondary` |
| `THEME` | `ACCENT_COLOR` | `#0EA5E9` | CSS `--color-accent` |
| `THEME` | `SUCCESS_COLOR` | `#16A34A` | Toasts/badges |
| `THEME` | `WARNING_COLOR` | `#D97706` | Toasts/badges |
| `THEME` | `ERROR_COLOR` | `#DC2626` | Toasts/badges |
| `THEME` | `FONT_FAMILY` | `Inter` | Typography |
| `THEME` | `BORDER_RADIUS` | `8px` | Component roundness |
| `ANNOUNCEMENT` | `ANNOUNCEMENT_ENABLED` | `false` | Show/hide the app-wide banner |
| `ANNOUNCEMENT` | `ANNOUNCEMENT_TEXT` | `Welcome to CMSApp` | Banner content |
| `ANNOUNCEMENT` | `ANNOUNCEMENT_TYPE` | `INFO` | `INFO` / `WARNING` / `CRITICAL` banner color |
| `ANNOUNCEMENT` | `MAINTENANCE_MODE` | `false` | Full-page "be right back" screen |

All seeded rows are `isEnable: true` (the announcement banner is toggled by the `ANNOUNCEMENT_ENABLED` *value*, so the toggle itself is always visible to the bootstrap call). Other suggested params to standardize on when needed: `LOGO_URL`/`LOGO_DARK_URL`/`FAVICON_URL` (not seeded — no real asset URLs exist yet), a `LAYOUT` group (`DEFAULT_PAGE_SIZE`, `PAGE_SIZE_OPTIONS`, `DATE_FORMAT`, `TIME_FORMAT`), and an `AUTH_UX` group (`GOOGLE_LOGIN_ENABLED`, `IDLE_LOGOUT_MINUTES`) — remember those toggles are cosmetic (hiding the Google button doesn't disable the endpoint). Adding any of them is a data change (`POST /api/appconfigs`), not a code change.

---

## The one endpoint every client uses

### GET /api/appconfigs/public — *anonymous*

The UI bootstrap call. **No token needed** — call it before login (the login page itself needs the app name and colors). Returns **only rows with `isEnable: true`**, as the trimmed public shape, sorted by `configGroup` then `configParam`.

**Response** (`200`): `data` = `PublicAppConfigDto[]`

```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": [
    { "configParam": "APP_NAME", "configValue": "My Blog CMS", "configGroup": "GENERAL" },
    { "configParam": "PRIMARY_COLOR", "configValue": "#4F46E5", "configGroup": "THEME" },
    { "configParam": "SECONDARY_COLOR", "configValue": "#EC4899", "configGroup": "THEME" },
    { "configParam": "THEME_MODE", "configValue": "DARK", "configGroup": "THEME" }
  ]
}
```

Recommended frontend pattern: fetch once at app start, reduce to a `{ [configParam]: configValue }` map, fall back to a hardcoded default for any missing key (the table may be empty on a fresh install), and apply theme values as CSS variables:

```js
const res = await fetch("/api/appconfigs/public").then(r => r.json());
const settings = Object.fromEntries(res.data.map(c => [c.configParam, c.configValue]));

document.title = settings.APP_NAME ?? "CMSApp";
document.documentElement.style.setProperty("--color-primary", settings.PRIMARY_COLOR ?? "#4F46E5");
document.documentElement.style.setProperty("--color-secondary", settings.SECONDARY_COLOR ?? "#EC4899");
```

**Do not put secrets in app configs.** Anything with `isEnable: true` is readable by the whole internet through this endpoint. To hide a setting from the public without deleting it, set `isEnable: false` — admins still see it through the CRUD endpoints below.

---

## Admin CRUD endpoints — `/api/appconfigs`

All of these require a valid JWT **and** the matching permission grant (seeded to SuperAdmin; grant to other roles via `POST /api/roles/claims`). Seeded permission codes: `APP_CONFIG_CREATE`, `APP_CONFIG_LIST`, `APP_CONFIG_DETAIL`, `APP_CONFIG_GROUP`, `APP_CONFIG_UPDATE`, `APP_CONFIG_DELETE` (under the `CONFIG_MANAGEMENT` main menu).

### POST /api/appconfigs — create a setting

**Request**:

```json
{
  "configParam": "PRIMARY_COLOR",
  "configValue": "#4F46E5",
  "configGroup": "THEME",
  "isEnable": true
}
```

`isEnable` defaults to `true` if omitted.

**Response** (`200`): `data` = the created `AppConfigDto`, `"App config created successfully."`

**Failures** (`400`):
- `CONFLICT` — `"App config with param 'PRIMARY_COLOR' already exists."` (`configParam` is globally unique)
- `VALIDATION_ERROR` — `configParam` required ≤ 256 chars; `configValue` required ≤ 555; `configGroup` optional ≤ 256.

### GET /api/appconfigs?page=1&pageSize=20 — paged list

**Response** (`200`): the standard pagination wrapper, `items` = `AppConfigDto[]` (enabled **and** disabled rows — this is the admin view).

```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "items": [ { "id": "...", "configParam": "APP_NAME", "configValue": "My Blog CMS", "configGroup": "GENERAL", "isEnable": true } ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 8,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

### GET /api/appconfigs/{id} — single setting

**Response** (`200`): `data` = `AppConfigDto`. **Failure**: `404 NOT_FOUND` `"App config with id '...' was not found."`

### GET /api/appconfigs/group/{configGroup} — all settings in one group

For a tabbed admin settings screen ("General" tab → `GET /api/appconfigs/group/GENERAL`). Not paged; sorted by `configParam`; includes disabled rows. The group match is exact and case-sensitive (`THEME`, not `theme`).

**Response** (`200`): `data` = `AppConfigDto[]`. An unknown group returns an empty array, not an error.

### PUT /api/appconfigs/{id} — update a setting

Full replace of the four fields — send all of them, not just the changed one. Renaming `configParam` re-checks uniqueness.

**Request**:

```json
{
  "configParam": "PRIMARY_COLOR",
  "configValue": "#16A34A",
  "configGroup": "THEME",
  "isEnable": true
}
```

**Response** (`200`): `data` = updated `AppConfigDto`, `"App config updated successfully."`

**Failures**: `404 NOT_FOUND` `"App config with id '...' was not found."`; `400 CONFLICT` on a duplicate `configParam`; `400 VALIDATION_ERROR`.

### DELETE /api/appconfigs/{id} — delete a setting

**Hard** delete (like roles/configs, unlike users/menus) — the row is gone and the `configParam` is immediately reusable.

**Response** (`200`): `{ ..., "responseMessage": "App config deleted successfully.", "data": true }` **Failure**: `404 NOT_FOUND`.

---

## Error-handling summary

| HTTP | `responseCode` | When |
|---|---|---|
| 200 | `SUCCESS` | Everything worked — `data` is populated |
| 400 | `VALIDATION_ERROR` | Missing/too-long field; message lists every failure |
| 400 | `CONFLICT` | Duplicate `configParam` on create, or on update when renaming |
| 404 | `NOT_FOUND` | Unknown `id` on get/update/delete |
| 403 | `FORBIDDEN` | No token, or the caller's roles lack the endpoint's permission grant (public endpoint excepted) |
| 500 | `SERVER_ERROR` | Unhandled server fault — show a generic error, it's already logged server-side |

## Backend notes (for whoever runs the API)

- The `dbo.app_configs` table is **new** and needs an EF Core migration before any of this works at runtime (same user-owned migration flow as the other post-initial tables).
- `configParam` has a unique index (`ix_app_configs_config_param`); the service also pre-checks and returns a clean `409 CONFLICT` before the DB would.
- Rows are audit-stamped automatically (`created_by`/`created_ts`/`updated_by`/`updated_ts`) like every other entity.
- `AppConfigSeeder` (2026-07-12) seeds the 17-row baseline above at startup — idempotent by `configParam`, create-if-missing only (admin edits are never overwritten). Additional rows are created via `POST /api/appconfigs` as usual.
