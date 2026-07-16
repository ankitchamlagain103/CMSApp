# Dashboard API update (2026-07-14)

Six new read-only endpoints added to `DashboardController` (`/api/dashboard`), alongside the existing `summary` / `error-logs` / `access-logs` endpoints. All are permission-gated the same way as the rest of the Dashboard controller (`DASHBOARD_*` rows seeded under the `DASHBOARD` main menu in `MenuSeeder`, granted to SuperAdmin by default — grant to other roles via `POST /api/roles/claims` as needed). This is a companion to the Dashboard section in `Docs/UI-Implementation-Guide.md` — keep both in sync on future changes.

| Endpoint | Purpose | Permission code |
|---|---|---|
| `GET /api/dashboard/enrollment-stats` | Student Enrollments widget | `DASHBOARD_ENROLLMENT_STATS` |
| `GET /api/dashboard/teachers?take=5` | Teachers List widget | `DASHBOARD_TEACHER_WIDGET` |
| `GET /api/dashboard/users?take=5` | User List widget | `DASHBOARD_USER_WIDGET` |
| `GET /api/dashboard/bar-graph?metric=...` | Bar graph / chart data | `DASHBOARD_BAR_GRAPH` |
| `GET /api/dashboard/current-academic-year` | Current academic year widget | `DASHBOARD_CURRENT_ACADEMIC_YEAR` |
| `GET /api/dashboard/quick-menus?take=8` | Quick menu suggestions | `DASHBOARD_QUICK_MENUS` |

All responses use the standard envelope: `{ responseCode, responseMessage, data }`.

## 1. GET /api/dashboard/enrollment-stats

No query parameters.

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
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
}
```

- `totalStudents` — count of all (non-soft-deleted) `Student` rows, regardless of enrollment state.
- `totalActiveEnrollments` — count of `Enrollment` rows with `status = Enrolled` (1), across all academic years.
- `enrollmentsByStatus` — every `Enrollment` row (any academic year, any status), grouped by `EnrollmentStatus`: `1` Enrolled, `2` Transferred, `3` Withdrawn, `4` Completed.
- `enrollmentsByGrade` — **scoped to the current academic year only** (`AcademicYear.IsCurrent = true`) and to `Enrolled`-status rows. One entry per seeded Grade config option (`typeCode 1001`), in the option's `order`, including grades with zero active enrollments. Returns an empty array (not an error) if no academic year is currently marked current.

No failure cases beyond the standard 401/403 from `AuthorizedAction`.

## 2. GET /api/dashboard/teachers?take=5

`take` (query, optional int) — how many recent teachers to return. Defaults to `5` if omitted or `<= 0`.

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "totalTeachers": 20,
    "activeTeachers": 19,
    "recentTeachers": [
      {
        "id": "b1111111-0000-0000-0000-000000000020",
        "employeeNo": "EMP2026020",
        "firstName": "Anita",
        "middleName": null,
        "lastName": "Sharma",
        "status": 1,
        "joiningDate": "2026-04-01T00:00:00"
      }
    ]
  }
}
```

- `totalTeachers` / `activeTeachers` — all-time count and the subset with `status = Active` (1).
- `recentTeachers` — the `take` most recently created teacher rows (`createdTs` descending), a trimmed shape (no qualifications/assignments/documents — use `GET /api/teachers/{id}` for the full profile).
- `status` is `RecordStatus`: `1` Active, `2` Inactive.

## 3. GET /api/dashboard/users?take=5

`take` (query, optional int) — how many recent users to return. Defaults to `5` if omitted or `<= 0`.

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "totalUsers": 42,
    "activeUsers": 39,
    "recentUsers": [
      {
        "id": "c1111111-0000-0000-0000-000000000042",
        "userName": "jdoe",
        "email": "jdoe@example.com",
        "firstName": "John",
        "lastName": "Doe",
        "userType": 2,
        "isActive": true,
        "createdTs": "2026-07-13T09:00:00+00:00"
      }
    ]
  }
}
```

- `totalUsers` / `activeUsers` — same numbers as `GET /api/dashboard/summary` (soft-deleted excluded by the global query filter).
- `recentUsers` — the `take` most recently created user accounts (`createdTs` descending).
- `userType` is `UserType`: `0` SuperAdmin, `1` Admin, `2` User.

## 4. GET /api/dashboard/bar-graph?metric={metric}

`metric` (query, **required** string) — one of:

| Metric | Chart | Notes |
|---|---|---|
| `EnrollmentsByGrade` | Active enrollments per grade | Current academic year only, same data as `enrollment-stats.enrollmentsByGrade` |
| `EnrollmentsByMonth` | New enrollments trend | Last 6 calendar months (inclusive of the current month), by `enrollmentDate`, zero-filled |
| `StudentsByStatus` | Students by status | Two bars: Active / Inactive |
| `TeachersByStatus` | Teachers by status | Two bars: Active / Inactive |

Missing or unrecognized `metric` → `400 VALIDATION_ERROR` with a message listing the allowed values.

**Response** (`200`) — shape is the same for every metric, so one chart component can render all four:
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "metric": "EnrollmentsByMonth",
    "title": "New Enrollments (Last 6 Months)",
    "labels": ["Feb 2026", "Mar 2026", "Apr 2026", "May 2026", "Jun 2026", "Jul 2026"],
    "series": [
      { "name": "New Enrollments", "data": [3, 5, 2, 8, 6, 4] }
    ]
  }
}
```

`series` is a list (not a single object) so a future metric that needs multiple stacked/grouped bars (e.g. enrollments-by-grade split by gender) can reuse the same shape without a breaking change — every current metric returns exactly one series.

**Adding a new metric later**: add a constant to `Application/Dashboard/DashboardBarGraphMetrics.cs`, add a `Build<Metric>BarGraphAsync` branch in `DashboardService.GetBarGraphAsync`, and document it here + in `UI-Implementation-Guide.md`.

## 5. GET /api/dashboard/current-academic-year

No query parameters.

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "id": "a1111111-0000-0000-0000-000000000001",
    "code": "2026",
    "name": "Academic Year 2026",
    "startDate": "2026-01-01T00:00:00",
    "endDate": "2026-12-31T00:00:00",
    "totalClasses": 13,
    "totalSections": 26,
    "totalActiveEnrollments": 96
  }
}
```

- Looks up the `AcademicYear` with `isCurrent = true` (the service layer guarantees at most one). `totalClasses` / `totalSections` count that year's `AcademicClass`/`ClassSection` rows; `totalActiveEnrollments` counts `Enrolled`-status `Enrollment` rows within it.
- **Failure**: `404 NOT_FOUND` — `"No academic year is marked as current."` — if none is set. Set one via `PUT /api/academicyears/{id}` (`isCurrent: true`, which auto-demotes any other year).

## 6. GET /api/dashboard/quick-menus?take=8

`take` (query, optional int) — how many suggestions to return. Defaults to `8` if omitted or `<= 0`.

Resolves shortcuts the **same way `GET /api/roles/user-menus` resolves the caller's permitted menu tree** (roles → `ApplicationRoleClaim` → `Menu`), then narrows to menus that are actually useful as a clickable shortcut: `menuType = SUB_MENU` (the feature's list page, which is what carries a real `url`), `isHidden = false`, `url` not null. `PERMISSION` rows and `MAIN_MENU` rows are excluded even if granted — they either have no URL or aren't a single actionable destination. Ordered by the menu's seeded `order`.

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": [
    { "id": 12, "code": "STUDENT_LIST", "displayName": "Students", "url": "/apps/student/list", "icon": "icons.student", "order": 1 },
    { "id": 20, "code": "TEACHER_LIST", "displayName": "Teachers", "url": "/apps/teacher/list", "icon": "icons.teacher", "order": 2 }
  ]
}
```

- Empty array (not an error) if the user's roles have zero `SUB_MENU` grants.
- **Failure**: `401 UNAUTHORIZED` if there's no authenticated user (shouldn't normally be reachable — `AuthorizedAction` already requires a valid token); `404 NOT_FOUND` if the caller's user row can't be found (e.g. deleted between token issuance and this call).

## Menu seeding

Six new `PERMISSION` rows were added to `MenuSeeder.BuildMenuCatalog()` under the existing `DASHBOARD` main menu (orders 5–10, right after the existing `DASHBOARD_SUMMARY` row at order 4):

```
DASHBOARD_ENROLLMENT_STATS        -> Dashboard.GetEnrollmentStats
DASHBOARD_TEACHER_WIDGET          -> Dashboard.GetTeacherListWidget
DASHBOARD_USER_WIDGET             -> Dashboard.GetUserListWidget
DASHBOARD_BAR_GRAPH               -> Dashboard.GetBarGraph
DASHBOARD_CURRENT_ACADEMIC_YEAR   -> Dashboard.GetCurrentAcademicYear
DASHBOARD_QUICK_MENUS             -> Dashboard.GetQuickMenus
```

Like every other `PERMISSION` row, these sync on every app startup and are granted to `SuperAdmin` automatically; `Admin`/`User` roles need them granted explicitly via `POST /api/roles/claims` before those roles can call the new endpoints.

## Implementation notes (for future maintenance)

- `Application/Dashboard/Dtos/` gained: `EnrollmentStatsDto`, `EnrollmentStatusCountDto`, `GradeEnrollmentCountDto`, `TeacherListWidgetDto`, `DashboardTeacherSummaryDto`, `UserListWidgetDto`, `DashboardUserSummaryDto`, `BarGraphDto`, `BarGraphSeriesDto`, `CurrentAcademicYearDto`, `QuickMenuDto`. `Application/Dashboard/DashboardBarGraphMetrics.cs` holds the allowed `metric` values.
- `Infrastructure/Identity/Services/DashboardService.cs` now also injects `ICurrentUserService` and `UserManager<ApplicationUser>` (needed for the quick-menus role resolution, same pattern as `RoleService.GetUserRolesAsync`). No new repository methods were added to `IStudentRepository`/`ITeacherRepository`/`IEnrollmentRepository` — these are cross-cutting dashboard aggregates, not per-aggregate CRUD queries, so `DashboardService` queries `ApplicationDbContext` directly for the counts/groupings, the same precedent already set by the existing `GetSummaryAsync` (which queries `_dbContext.Users` directly since Identity's user set has no dedicated repository). The one exception: fetching the current academic year reuses the existing `IAcademicYearRepository.GetCurrentYearsAsync()`.
- No new database tables or columns — every new endpoint reads existing entities (`Student`, `Teacher`, `Enrollment`, `ClassSection`, `AcademicClass`, `AcademicYear`, `Config`, `Menu`, `ApplicationUser`, `ApplicationRoleClaim`). No migration needed for this change.
