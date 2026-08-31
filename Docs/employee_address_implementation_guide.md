# Employee Address (Province / District / Local Level / Ward) — implementation guide

**2026-07-24.** Extends the Employee "org" `provinceCode` field (added 2026-07-23, see
`leave_management_and_employee_profile_implementation_guide.md`) into a full Nepal address:
**Province → District → Local Level (municipality / rural municipality / metropolitan /
sub-metropolitan city) → Ward No.** Two new Config catalogs back the new levels; no new database
tables are needed for the catalogs themselves (`ConfigType`/`Config` are already generic), only
three new nullable columns on `dbo.employees`.

## New fields

`EmployeeDto` / `CreateEmployeeCommand` / `UpdateEmployeeCommand` gained:

| Field | Type | Notes |
|---|---|---|
| `provinceCode` | string, optional | Unchanged field, Config catalog `1020` — now the anchor of the address chain. |
| `districtCode` | string, optional | New. Config catalog `1022`. |
| `localLevelCode` | string, optional | New. Config catalog `1023`. |
| `wardNo` | int, optional | New. Shape-only validated (`1`–`99`) — actual ward counts vary per local level (5 to 33+) and aren't tracked as catalog metadata, same "shape-only" reasoning as `CountryIso3` on user registration. |

All four are independently optional — an employee can have no address, a bare province, or the
full chain.

## The two new catalogs

| TypeCode | Name | `AdditionalValue1` | `AdditionalValue2` | `AdditionalValue3` |
|---|---|---|---|---|
| `1022` District | its `ProvinceCode` | — | — |
| `1023` Local Level | its `DistrictCode` | its `ProvinceCode` (denormalized) | its type — one of `METROPOLITAN_CITY` / `SUB_METROPOLITAN_CITY` / `MUNICIPALITY` / `RURAL_MUNICIPALITY` (`Domain/Constants/LocalLevelTypeCodes`) |

A Local Level option denormalizes its province code directly (not just its district) so a UI can
reverse-map **both** coarser levels from a single dropdown/search response, without a second
lookup.

## Seed data coverage — read this before relying on it for real addresses

`ConfigCatalogSeeder` seeds:

- **All 77 districts** — the full, stable list (Nepal's 2017 federal-restructuring district count
  hasn't changed since). High confidence.
- **80 of Nepal's 753 local levels** — the 6 Metropolitan Cities, the 11 Sub-Metropolitan Cities,
  and one notable municipality/rural municipality per remaining district, so **every one of the 77
  districts has at least one immediately usable, searchable Local Level option**. This is a
  deliberately partial set, not the full 753.

**Why not all 753:** hand-typing the full official list from model training data risks baking
wrong government records into the database with no authoritative source to cross-check against —
unlike this codebase's other "illustrative" placeholder seed data (e.g. `PayrollSeeder`'s sample
tax slabs), a wrong municipality name or district mapping here is the kind of error that's unlikely
to be noticed until an actual employee address is wrong. The 80 seeded rows above (district
capitals, all metro/sub-metro cities) are the well-known, high-confidence subset; the remaining
~670 (mostly smaller rural municipalities) were deliberately left out rather than risk shipping
fabricated entries under the guise of authoritative data.

**Before going live with real employee addresses**, add the remaining local levels for whichever
districts your staff actually live in, using the official MoFAGA (Ministry of Federal Affairs and
General Administration) or Election Commission of Nepal local-level list:

```
POST /api/configs
{
  "typeCode": 1023,
  "code": "<UNIQUE_CODE>",
  "label": "<Display Name>",
  "order": <int>,
  "additionalValue1": "<DistrictCode>",
  "additionalValue2": "<ProvinceCode>",
  "additionalValue3": "MUNICIPALITY" | "RURAL_MUNICIPALITY" | "METROPOLITAN_CITY" | "SUB_METROPOLITAN_CITY"
}
```

One row per local level, same single-item `POST /api/configs` every other catalog in this system
already uses to add options (no bulk-import endpoint exists — this is a deliberate scope decision,
not an oversight, since a bulk endpoint doesn't remove the need for accurate source data). Existing
seeded rows are safe to edit in place via `PUT /api/configs/{id}` if you spot a wrong name/mapping
— `ConfigCatalogSeeder` is create-if-missing, so admin corrections survive restarts.

## Cascading and searchable dropdowns (generalized, not address-specific)

`GET /api/configs/dropdown/{typeCode}` gained two optional query parameters, reusable by **any**
hierarchical Config catalog, not just this feature:

| Param | Behavior |
|---|---|
| `parentCode` | Narrows to options whose `AdditionalValue1` equals it. |
| `search` | Case-insensitive substring match against `Label`. |

Both are optional and independent; omitting both behaves exactly like the endpoint always has.
This endpoint is listed in `DefaultEnabledMenu` (`"Configs": "GetConfigsByTypeCode"`), so any
authenticated user can call it with no permission grant, same as every other dropdown.

**Two UI patterns are both supported by the same endpoint — pick whichever fits:**

1. **Classic cascading selects.** Province select (fetch all 7 upfront) → on change, fetch
   `GET /api/configs/dropdown/1022?parentCode={provinceCode}` for the District select → on change,
   fetch `GET /api/configs/dropdown/1023?parentCode={districtCode}` for the Local Level select.
   Good when a district only has a handful of local levels loaded.
2. **Searchable Local Level box with automatic reverse-map** (what the user asked for). Skip the
   cascade — give the user one autocomplete/search box against
   `GET /api/configs/dropdown/1023?search={typed text}`. When they pick a result, its
   `additionalValue1` is the DistrictCode and `additionalValue2` is the ProvinceCode — look those
   two codes up in the Province/District dropdown lists you already fetched in full up front (only
   7 + 77 = 84 rows total, trivial to preload once and cache client-side) to auto-select/display
   Province and District with **no extra API call**. Submit `provinceCode`/`districtCode`/
   `localLevelCode` together, or just `localLevelCode` (see below) and let the backend do it.

## Backend auto-derivation (the actual "reverse map automatically")

`EmployeeService.ResolveAddressAsync` makes the reverse-mapping work even if the frontend only
sends the most specific field:

- Send only `localLevelCode` → the server looks it up and **fills in `districtCode` and
  `provinceCode` from its catalog row automatically** before saving.
- Send only `districtCode` (no local level yet) → `provinceCode` is auto-filled from the
  district's own catalog row.
- Send more than one level explicitly → they're **cross-checked, not silently overridden**. A
  `localLevelCode` that doesn't actually belong to the supplied `districtCode`/`provinceCode` (or
  a `districtCode` that doesn't belong to the supplied `provinceCode`) is rejected with a
  `VALIDATION_ERROR` naming the mismatch, rather than one of the two silently winning.

This means the UI pattern in option 2 above can submit `localLevelCode` and `wardNo` alone and
trust the backend to persist the correct district/province — the "auto reverse-map" the request
asked for is enforced server-side, not just echoed back by the dropdown response.

## Failure cases

| Condition | Response |
|---|---|
| `localLevelCode` not a known Config option | `VALIDATION_ERROR` — "LocalLevelCode '...' is not a known local level option." |
| `districtCode` not a known Config option | `VALIDATION_ERROR` — "DistrictCode '...' is not a known district option." |
| `provinceCode` not a known Config option | `VALIDATION_ERROR` — "ProvinceCode '...' is not a known province option." |
| Supplied `localLevelCode` doesn't belong to the supplied `districtCode` | `VALIDATION_ERROR` naming the local level's real district |
| Supplied `localLevelCode`/`districtCode` doesn't belong to the supplied `provinceCode` | `VALIDATION_ERROR` naming the real province |
| `wardNo` outside `1`–`99` | `VALIDATION_ERROR` (FluentValidation, same envelope as every other field-shape error) |

## Typical UI flow (student/employee-profile "Address" panel)

1. On page load, fetch `GET /api/configs/dropdown/1020` (Province, 7 rows) and cache it for the
   session — small enough to hold entirely client-side.
2. Give the user a single searchable Local Level field (`GET /api/configs/dropdown/1023?search=`,
   debounced). On selection, read `additionalValue1`/`additionalValue2` off the chosen result to
   display Province/District read-only next to it (look up their labels in the cached Province
   list and a lazily-fetched `GET /api/configs/dropdown/1022?parentCode={provinceCode}` District
   list) — no extra round trip needed for the labels beyond that one District fetch.
3. Add a plain numeric Ward No. input (no catalog, no dropdown).
4. Submit `provinceCode`, `districtCode`, `localLevelCode`, `wardNo` together on
   `POST /api/employees` / `PUT /api/employees/{id}` — or, if you'd rather keep the form even
   simpler, submit only `localLevelCode` + `wardNo` and skip steps that populate `provinceCode`/
   `districtCode` client-side entirely, trusting the backend auto-derivation above.
5. If the district you need isn't in the seeded 80, either add it via `POST /api/configs` (see
   above) or fall back to the classic cascading Province → District → Local Level selects, which
   still work over whatever's currently seeded for that district.

## Needs a migration

Three new nullable columns on `dbo.employees`: `district_code varchar(100)`,
`local_level_code varchar(100)`, `ward_no integer`. No new tables — District/LocalLevel reuse the
existing `dbo.config_types`/`dbo.configs` tables (new `TypeCode` rows only, seeded automatically on
next boot once the migration is applied). Every `provinceCode`/`districtCode`/`localLevelCode`/
`wardNo` read/write 500s until the three columns exist.
