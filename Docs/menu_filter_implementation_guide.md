# CMSApp — Menu List Filter Implementation Guide (UI)

The menu list endpoint now supports filtering by menu type and audience. Same conventions as `UI-Implementation-Guide.md`: common response envelope, camelCase JSON, `Authorization: Bearer {accessToken}` required.

## Endpoint

### GET /api/menus?page=1&pageSize=20&menuType=MAIN_MENU&menuFor=ADMIN

Covered by the existing `MENU_LIST` permission — no new grant needed if the role can already list menus.

| Query param | Required | Allowed values | Notes |
|---|---|---|---|
| `page` | no (default `1`) | ≥ 1 | |
| `pageSize` | no (default `20`) | ≥ 1 | |
| `menuType` | no | `MAIN_MENU`, `SUB_MENU`, `PERMISSION` | Omit to not filter by type |
| `menuFor` | no | `ADMIN`, `USER`, `BOTH` | Omit to not filter by audience |

The two filters are independent — send either, both, or neither. Omitting both gives the same unfiltered list as before. Results are sorted by `order`.

**Exact-match caveat**: `menuFor=ADMIN` returns only rows saved as `ADMIN` — it does **not** include `BOTH` rows. If the UI wants "everything visible to admins", request `ADMIN` and `BOTH` separately (or fetch unfiltered and filter client-side).

## Example

`GET /api/menus?page=1&pageSize=20&menuType=MAIN_MENU&menuFor=ADMIN`

**Response** (`200`) — standard pagination wrapper, `items` = `MenuDto[]`, `totalCount` reflects the **filtered** count:

```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "items": [
      {
        "id": 1,
        "code": "USER_MANAGEMENT",
        "displayName": "User Management",
        "url": null,
        "icon": "users",
        "menuType": "MAIN_MENU",
        "controller": null,
        "action": null,
        "parentId": null,
        "menuFor": "ADMIN",
        "order": 1,
        "isHidden": false
      }
    ],
    "page": 1,
    "pageSize": 20,
    "totalCount": 5,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  }
}
```

## Failures

| HTTP | `responseCode` | When / message |
|---|---|---|
| 400 | `VALIDATION_ERROR` | `"MenuType must be one of: MAIN_MENU, SUB_MENU, PERMISSION."` or `"MenuFor must be one of: ADMIN, USER, BOTH."` |
| 403 | `FORBIDDEN` | No token, or the caller's roles lack the `MENU_LIST` grant |

## Typical UI uses

- Parent-menu dropdown when creating a `SUB_MENU`: `?menuType=MAIN_MENU`
- Parent dropdown when creating a `PERMISSION`: request `MAIN_MENU` and `SUB_MENU` (two calls, or unfiltered + client-side filter)
- Permission-assignment screen listing only leaves: `?menuType=PERMISSION`
- Admin vs. user nav management tabs: `?menuFor=ADMIN` / `?menuFor=USER` (plus `BOTH` — see caveat above)
