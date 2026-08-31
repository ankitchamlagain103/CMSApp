# Global Search (Students / Employees / Teachers)

2026-08-05. A single navbar-level search endpoint — the "Ctrl + K" style search box that sits
outside any specific menu (Student Management, Employee Management, ...) and needs to find a
record by name, admission number, or employee code regardless of which screen the user is
currently on. This is distinct from the search box already built into the Students list page
(`GET /api/students?search=...`) or the Employees list page — those are scoped to one list and
one entity type; this is the cross-entity, cross-menu lookup a global search bar needs.

## Endpoint

```
GET /api/dashboard/global-search?query={text}&limit={n}
```

Permission: `DASHBOARD_GLOBAL_SEARCH` (seeded under `DASHBOARD`, action `GlobalSearch`). Gated
like every other Dashboard widget — **not** in `DefaultEnabledMenu`, so a role must be explicitly
granted this via `POST /api/roles/claims` before its users can search. This is a deliberate
security choice: global search can surface Student/Employee names, codes, and status across the
whole organization to whoever can call it, independent of whether they separately hold
`STUDENT_LIST`/`EMPLOYEE_LIST` — granting it should be a conscious admin decision per role, the
same way every other cross-cutting permission in this codebase already works, not a blanket
default.

### Query parameters

| Param | Type | Required | Notes |
|---|---|---|---|
| `query` | string | **yes** | The search text. Trimmed; blank/whitespace-only is a `ValidationError` ("A search query is required."). |
| `limit` | int | no (default `5`) | Caps **each group independently** (so `limit=5` can return up to 5 students *and* up to 5 employees, not 5 total). Capped server-side at `20` regardless of what's requested. |

### What `query` matches

| Entity | Matched columns | Notes |
|---|---|---|
| Student | `FirstName`, `LastName`, `AdmissionNo` (all case-insensitive partial match) | `MiddleName` is **not** matched — same convention `GET /api/students?search=` already uses; kept consistent rather than diverging for this endpoint. |
| Employee | `FirstName`, `LastName`, `EmployeeCode` (same matching rules) | Covers teaching staff too — see "Why no separate Teacher group" below. |
| Both | The record's own `Id`, if `query` parses as a valid GUID | Covers pasting a student's or employee's own id from another screen — this is the "Student ID"/"Employee ID" half of the original ask, distinct from `AdmissionNo`/`EmployeeCode`. |

A query like `"aarati"` matches by name; `"ADM2026192"` matches by admission number;
`"EMP2026001"` matches by employee code; `"019f5c83-8d77-7ce9-80cc-d3c64e2a1096"` matches by id
directly. All are handled by the same endpoint — the caller doesn't need to know in advance which
kind of value it's typing.

## Response

```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "query": "ramesh",
    "students": [],
    "employees": [
      {
        "id": "019f659b-955d-76dc-a247-128ce153bab0",
        "employeeCode": "EMP2026001",
        "fullName": "Ramesh Adhikari",
        "jobPositionCode": "TEACHER",
        "jobPositionLabel": "Teacher",
        "employmentStatus": 1,
        "isTeacher": true
      }
    ]
  }
}
```

- Results are **grouped by entity type** (`students[]` / `employees[]`), not one flat interleaved
  list — lets the UI render "Students" / "Employees" sections directly, matching how a command
  palette or navbar search dropdown typically presents mixed-type results.
- Each group is sorted by first name, then last name.
- `jobPositionLabel` is resolved server-side (via the existing `ConfigLabelHelper`, catalog 1012)
  — the caller doesn't need a separate dropdown call just to turn `"TEACHER"` into `"Teacher"`.

### Why no separate "Teacher" group

Per this codebase's Employee/Teacher split, a `Teacher` is not a standalone identity record — it's
a thin profile sharing its primary key with an `Employee` row (`Teacher.Id == Employee.Id`).
Introducing a third `teachers[]` group here would mean either querying the same `Employee` rows
twice (once as "employee," once as "teacher," with near-total overlap) or arbitrarily excluding
teaching staff from the `employees[]` group — both worse than the alternative taken here: **one**
`employees[]` group, with `isTeacher` telling the UI which profile route to send the user to
(`/apps/teacher/detail/{id}` vs `/apps/employee/detail/{id}`). This mirrors the same `Employee.Teacher`
nav-based check `EmployeeMapper.ToDto`'s own `HasTeacherProfile` field already uses elsewhere.

## Typical UI use

The navbar "Ctrl + K" search box: debounce keystrokes, call this endpoint (a small `limit`, e.g.
5, is enough for a dropdown preview), and render up to two sections — "Students" and
"Employees" — each row linking to that record's own detail page using `isTeacher` (for employees)
to pick the right route. If the user wants the full result set beyond the preview, route them to
the existing per-entity list page with the same text pre-filled into its own `search` param
(`GET /apps/student/list?search=...` / `GET /apps/employee/list?search=...`) rather than trying to
paginate this endpoint — it's a typeahead lookup, not a general-purpose paged search (see
"Deliberately out of scope" below).

## Implementation notes

- Lives on the existing `IDashboardService`/`DashboardService` (`Infrastructure/Identity/Services/DashboardService.cs`)
  as `GlobalSearchAsync`, **not** a new `Application/GlobalSearch/` feature. `DashboardService` is
  already this codebase's home for cross-cutting, multi-aggregate, UI-navigation-oriented queries
  (`GetQuickMenusAsync`, `GetAccountsSummaryAsync`, `GetHrSummaryAsync` are all the same shape —
  read-only, spanning entities no single aggregate's own repository owns) — a search across
  Student *and* Employee is exactly this kind of concern, so it was added there instead of
  introducing a new feature folder and a new "where does this belong" question.
- Queries `ApplicationDbContext.Students`/`ApplicationDbContext.Employees` **directly**, not
  through `IStudentRepository`/`IEmployeeRepository` — same reasoning as every other
  `DashboardService` widget: this isn't a domain operation either aggregate's own repository
  should own, it's a cross-cutting UI read. No new repository methods were added.
- Reuses the existing `BuildFullName` private helper already on `DashboardService` (used by
  `GetAccountsSummaryAsync`/`GetHrSummaryAsync`) and the existing `ConfigLabelHelper` pattern for
  `jobPositionLabel` — no new patterns introduced, just applied to a new endpoint.
- New route: `GET /api/dashboard/global-search` on the existing `DashboardController`. New
  permission `DASHBOARD_GLOBAL_SEARCH` (order 13, the next number after `DASHBOARD_HR_SUMMARY`).

## Deliberately out of scope

- **Not a paged/sortable search** — it's a typeahead preview (`limit`-capped per group, no `page`
  parameter, no total count). A "see all matching students" action should go to the existing
  `GET /api/students?search=...` list endpoint instead, which already has full paging/filtering.
- **No fuzzy/typo-tolerant matching** — plain `ILike '%text%'`, same as every other search filter
  in this codebase. A misspelled name won't match; this isn't a full-text search engine.
- **No relevance ranking** beyond "matches or doesn't" — results are sorted alphabetically by name,
  not by how well they match (e.g. an exact code match isn't promoted above a partial name match).
  Fine for the small result sets a `limit`-capped typeahead returns; would need revisiting if this
  endpoint's role ever grows into a full search-everything feature.
- **No Guardian search** — the original ask was Student/Employee/Teacher specifically; Guardians
  have their own list page (`GET /api/guardians`) already.

## No new migration

Read-only over existing `Student`/`Employee` data — no entity, column, or index changes.
