# Config Catalog (ConfigType / Config) CRUD — implementation guide

This is the admin surface behind **every dropdown in the system** — `ConfigType` names a dropdown
(e.g. "District", "Fee Category", "Employee Level") and owns a unique `typeCode`; `Config` rows are
that dropdown's options (`code`/`label`/`order` plus three free-form `additionalValue1..3` slots
used differently per catalog — see each feature's own guide for what its catalog's slots mean).
This guide focuses on the **edit and delete** paths specifically, since create/list are already
straightforward and well-covered elsewhere; it's the single reference for how updating or removing
a `ConfigType`/`Config` behaves.

## Endpoints

| Method | Route | Purpose |
|---|---|---|
| `POST` | `/api/configs/types` | Create a `ConfigType`. |
| `GET` | `/api/configs/types?page=&pageSize=` | Paged list of `ConfigType`s. |
| `GET` | `/api/configs/types/{id}` | Single `ConfigType`. |
| `PUT` | `/api/configs/types/{id}` | **Edit** a `ConfigType` — `name`/`description` only. |
| `DELETE` | `/api/configs/types/{id}` | **Delete** a `ConfigType`. |
| `POST` | `/api/configs` | Create a `Config` option. |
| `GET` | `/api/configs/{id}` | Single `Config` option. |
| `PUT` | `/api/configs/{id}` | **Edit** a `Config` option — `code`/`label`/`order`/`additionalValue1..3`. |
| `DELETE` | `/api/configs/{id}` | **Delete** a `Config` option. |
| `GET` | `/api/configs/dropdown/{typeCode}?parentCode=&search=` | The read-only dropdown feed every form binds to — see "Dropdown endpoint" below. |

All ten routes go through the global `AuthorizedAction` filter like every other controller in this
system — `GetConfigsByTypeCode` (the dropdown feed) is listed in `DefaultEnabledMenu` so any
authenticated user can populate a dropdown with no permission grant; the other nine need the
`CONFIG_TYPE_*`/`CONFIG_*` (or per-feature equivalent) permission rows granted to the caller's
role, same as any other admin CRUD screen.

## Editing a `ConfigType` (`PUT /api/configs/types/{id}`)

```json
{ "name": "District", "description": "Nepal district catalog (employee address)" }
```

- **`typeCode` is immutable** — it isn't even part of the update body. It's the identity every
  `Config` option's `AdditionalValue1`/hierarchy and every feature's hardcoded `ConfigTypeCodes.*`
  constant is built against; changing it after the fact would silently break every reference.
  Renaming/editing the description is safe and has no side effects beyond display.
- 404 if the id doesn't exist.
- No delete-style reference guard on update — you can always rename/re-describe a type freely.

## Deleting a `ConfigType` (`DELETE /api/configs/types/{id}`)

- **Refused with `Conflict` while any `Config` row still references its `typeCode`**
  (`IConfigRepository.AnyByTypeCodeAsync`) — the FK from `Config.TypeCode` to
  `ConfigType.TypeCode` is `Restrict`, so this pre-check turns what would otherwise be a raw
  database constraint violation (a 500) into a clean, actionable `Conflict` response: *"Config
  type with type code 'X' still has configs. Delete its configs first."*
- Delete every option under the type first (see below), then the type itself.
- 404 if the id doesn't exist.

## Editing a `Config` option (`PUT /api/configs/{id}`)

```json
{
  "code": "KATHMANDU",
  "label": "Kathmandu",
  "order": 1,
  "additionalValue1": null,
  "additionalValue2": null,
  "additionalValue3": null
}
```

- **`typeCode` is immutable on this endpoint too** — not part of the body, can't be moved to a
  different catalog. Deleting and re-creating under a different type is the only way to "move" an
  option, and that changes its `Id`.
- **`code` uniqueness is only re-checked when `code` actually changes** — editing just the label/
  order/additional values on an unchanged code does no uniqueness lookup at all (cheap, and there's
  nothing to conflict with). Changing `code` re-runs the same `(TypeCode, Code)` uniqueness check
  `POST` uses, `Conflict` on a clash.
- **One catalog has an extra rule enforced here**: if the option's `TypeCode` is `FeeCategory`
  (`1010`), `additionalValue1` must be one of the `FeeFrequencyCodes` (`MONTHLY`/`ANNUAL`/
  `ONE_TIME`) — a `ValidationError` otherwise. Every other catalog's `additionalValue1..3` stay
  completely free-form; this is the one place a catalog-specific business rule leaks into the
  otherwise-generic `Config` update path. If you add a similarly-constrained catalog in the
  future, this is the method (`ConfigService.ValidateFeeCategoryFrequency`) to extend, following
  the same `typeCode`-switch pattern.
- 404 if the id doesn't exist.

**⚠️ Known gap — editing (or deleting, below) a `Config` option's `code` is not reference-checked
against the rest of the application.** Every feature that "validates against a Config catalog"
does so by storing the option's plain `Code` **string** on its own entity — `Employee.DistrictCode`,
`FeeStructureItem.FeeCategoryCode`, `ClassSubject.SubjectCode`, and so on — deliberately **not** as
a database foreign key (see each feature's own guide for why: `Config.Code` is only unique per
`(TypeCode, Code)`, not globally, so a real FK isn't possible without the composite key). This
means:

- Renaming a `Code` (e.g. `KATHMANDU` → `KTM`) silently orphans every existing row that stored the
  old code — they keep the old string forever, and it stops resolving to anything in the dropdown.
- The **`Label`, `Order`, and `AdditionalValue1..3` are always safe to edit** — nothing else in the
  system depends on those staying fixed, only on `Code` staying stable once real data references
  it.

**Practical guidance for the UI**: treat `Code` as effectively immutable once any real record might
reference it (anything past initial setup) — offer editing `Label`/`Order`/`AdditionalValue1..3`
freely, but warn (or outright disable) changing `Code` on an option, and prefer "add a new option +
retire the old one" over renaming a `Code` that's already in use.

## Deleting a `Config` option (`DELETE /api/configs/{id}`)

- **Hard delete, no reference guard at all** — unlike deleting a `ConfigType` (blocked while it
  still has options) or deleting most feature entities in this codebase (blocked while children
  exist), deleting a single `Config` **option** has no analogous check. This is a direct
  consequence of the same fact above: since nothing is a real FK to `Config.Id`/`Code`, there is no
  child table for the delete path to even check against.
- Same orphaning risk as renaming a `Code` — any employee/student/fee-item/etc. row that already
  stored this option's `Code` keeps it, and it simply stops appearing in the dropdown or resolving
  to a label. It does **not** cascade-null anything, and it does **not** retroactively invalidate
  those rows (they don't get re-validated on their own next update, since the Config-code checks
  in every feature service only run when that specific field is being set/changed).
- 404 if the id doesn't exist.

**Practical guidance for the UI**: before offering a delete button on a `Config` option, consider
whether the feature it backs has any way to check "is this code currently in use" (most don't,
today) — absent that, treat delete as a genuinely destructive action for options that might already
be referenced, and prefer disabling an option (if the feature has such a concept) or simply leaving
unused seed options in place over deleting them.

## Dropdown endpoint (`GET /api/configs/dropdown/{typeCode}`)

The read path every form actually binds to — returns `DropdownItemDto[]`
(`value`/`label`/`order`/`additionalValue1..3`), ordered by `order`. Two optional query params
(added 2026-07-24, reusable by any catalog):

| Param | Behavior |
|---|---|
| `parentCode` | Narrows to options whose `additionalValue1` equals it — the cascading-dropdown case (e.g. District options for one Province, Local Level options for one District). |
| `search` | Case-insensitive substring match against `label` — the searchable-lookup case for a catalog with too many options for a plain `<select>` (e.g. Local Level's 753 possible rows). |

Both are optional and independent; omitting both is the exact same unfiltered call every existing
dropdown caller already makes, so adding these params never breaks an existing integration.

## Failure cases (edit/delete specifically)

| Action | Condition | Response |
|---|---|---|
| `PUT /configs/types/{id}` | id not found | `NOT_FOUND` |
| `DELETE /configs/types/{id}` | id not found | `NOT_FOUND` |
| `DELETE /configs/types/{id}` | type still has `Config` options | `CONFLICT` — "still has configs. Delete its configs first." |
| `PUT /configs/{id}` | id not found | `NOT_FOUND` |
| `PUT /configs/{id}` | new `code` already used by another option of the same `typeCode` | `CONFLICT` |
| `PUT /configs/{id}` | `typeCode == FeeCategory` and `additionalValue1` isn't a valid fee frequency | `VALIDATION_ERROR` |
| `DELETE /configs/{id}` | id not found | `NOT_FOUND` |
| `DELETE /configs/{id}` | option is still referenced elsewhere by `Code` | **No error today** — see the "known gap" callout above |

## Typical UI flow

A generic "Manage Config Types" admin page: a `ConfigType` list (paged), each row expanding (or
linking) to its own "Manage Options" sub-page — the same `Config` CRUD table reused for every
catalog by just varying `typeCode`. Suggested affordances on the options table:

- Inline edit for `Label`/`Order`/`AdditionalValue1..3` — safe, no warnings needed.
- A confirmation dialog specifically for editing `Code` ("Renaming this code will not update any
  existing records that reference it — they will need to be corrected manually or via
  `PUT` on each affected record") and for delete ("Deleting this option does not check whether any
  record currently uses it").
- Sort by `Order` by default, matching what every dropdown consumer sees.
