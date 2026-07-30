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

Resolves shortcuts the **same way `GET /api/roles/user-menus` resolves the caller's permitted menu tree** (roles → `ApplicationRoleClaim` → `Menu`), then narrows to menus **explicitly curated as quick links** (2026-07-28): `menuType = SUB_MENU`, `isHidden = false`, `isQuickLink = true`. `PERMISSION`/`MAIN_MENU` rows and any `SUB_MENU` not flagged are excluded even if granted — `isQuickLink` is additive on top of the permission check, not a substitute for it. **Deprecates the earlier heuristic** (any granted `SUB_MENU` with a non-null `url`) — that implicitly showed every accessible list page rather than a curated set. Ordered by the menu's seeded `order`. Seeded quick links: Students (`STUDENT_LIST`), Fee Generation (`FEE_INVOICE_LIST`), Config Types (`CONFIG_TYPE_LIST`), Users (`USER_LIST`), Employees (`EMPLOYEE_LIST`), Academic Years (`YEAR_LIST`) — an admin can curate more (or unset these) via `POST/PUT /api/menus`' new `isQuickLink` field.

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

## 7. GET /api/dashboard/accounts-summary?take=5 (2026-07-28)

Composite "Accounts" (finance) widget — one call covering fee collection, outstanding dues, and
the current payroll run. `take` controls `recentPayments` length (default 5).

**Response** (`200`): `data` =
```json
{
  "feeCollectedToday": 12500.00,
  "feeCollectedThisMonth": 340000.00,
  "feeCollectedThisFiscalYear": 2100000.00,
  "totalOutstandingDue": 85400.00,
  "invoiceCountsByStatus": [ { "status": 2, "count": 40 }, { "status": 5, "count": 300 } ],
  "pendingFeeAdjustmentCount": 3,
  "currentPayrollRun": {
    "payrollRunId": "...",
    "fiscalYearCode": "2084/85",
    "monthIndex": 4,
    "status": 1,
    "slipCount": 20,
    "totalNetPay": 950000.00
  },
  "recentPayments": [
    { "id": "...", "receiptNo": "RCPT2026001", "studentName": "Anish Rai", "amount": 5000.00, "paymentDate": "2026-07-27T00:00:00" }
  ]
}
```
`invoiceCountsByStatus.status` is `FeeInvoiceStatus` (1 Draft / 2 Generated / 3 Pending / 4
PartiallyPaid / 5 Paid / 6 Cancelled). `totalOutstandingDue` uses the same `NetAmount - PaidAmount`
formula (excluding Draft/Cancelled) used everywhere else in the Fee module. `currentPayrollRun` is
the most recently created `PayrollRun` (`status`: 1 Draft / 2 Approved / 3 Paid / 4 Cancelled),
`null` if none exists yet; its `slipCount`/`totalNetPay` exclude Cancelled slips, same as every
other payroll-run aggregate in this codebase. Fee-collected totals exclude Voided payments.

## 8. GET /api/dashboard/hr-summary?take=5 (2026-07-28)

Composite "HR" widget — headcount, pending approvals, recent hires, and upcoming
birthdays/anniversaries. `take` controls `recentHires` length (default 5).

**Response** (`200`): `data` =
```json
{
  "totalEmployees": 45,
  "employeesByStatus": [ { "status": 1, "count": 42 }, { "status": 2, "count": 3 } ],
  "employeesByCategory": [ { "categoryCode": "ACADEMIC", "categoryLabel": "Academic", "count": 25 } ],
  "pendingLeaveRequestCount": 4,
  "pendingLoanRequestCount": 1,
  "recentHires": [
    { "employeeId": "...", "fullName": "Anita Sharma", "jobPositionCode": "TEACHER", "jobPositionLabel": "Teacher", "joinDate": "2026-07-01T00:00:00" }
  ],
  "upcomingBirthdays": [ { "employeeId": "...", "fullName": "Anita Sharma", "date": "2026-08-05T00:00:00" } ],
  "upcomingWorkAnniversaries": []
}
```
`employeesByStatus.status` is `EmploymentStatus` (1 Active / 2 OnLeave / 3 Suspended / 4 Resigned /
5 Terminated / 6 Retired). `pendingLeaveRequestCount` counts `LeaveRequest.hrStatus == Pending` —
HR's decision is the authoritative gate regardless of the manager's own decision, per the Leave
Management module's own design. `upcomingBirthdays`/`upcomingWorkAnniversaries` look 30 days ahead
for **Active** employees only, rolling into next year once this year's date has passed (same logic
the Employee Profile page's own upcoming-events widget uses for a single employee).

## Menu seeding

Eight new `PERMISSION` rows now exist under the `DASHBOARD` main menu (orders 4–12; the three
log-related rows that used to sit at orders 1–3 moved out, see below):

```
DASHBOARD_SUMMARY                 -> Dashboard.GetSummary
DASHBOARD_ENROLLMENT_STATS        -> Dashboard.GetEnrollmentStats
DASHBOARD_TEACHER_WIDGET          -> Dashboard.GetTeacherListWidget
DASHBOARD_USER_WIDGET             -> Dashboard.GetUserListWidget
DASHBOARD_BAR_GRAPH               -> Dashboard.GetBarGraph
DASHBOARD_CURRENT_ACADEMIC_YEAR   -> Dashboard.GetCurrentAcademicYear
DASHBOARD_QUICK_MENUS             -> Dashboard.GetQuickMenus
DASHBOARD_ACCOUNTS_SUMMARY        -> Dashboard.GetAccountsSummary   (2026-07-28)
DASHBOARD_HR_SUMMARY              -> Dashboard.GetHrSummary         (2026-07-28)
```

Like every other `PERMISSION` row, these sync on every app startup and are granted to `SuperAdmin`
automatically; any other role (`Admin`/`User`, or a school-created "Accounts"/"HR" role) needs
them granted explicitly via `POST /api/roles/claims`.

**Logs moved out of Dashboard (2026-07-28)**: `ERROR_LOG_LIST`/`ERROR_LOG_SUMMARY`/`ACCESS_LOG_LIST`
used to be hidden `PERMISSION` rows directly under `DASHBOARD` with no sidebar entry of their own.
They now live under a new top-level `LOGS` main menu as two real (visible) `SUB_MENU` pages:
`ACCESS_LOG_LIST` ("System Access Logs", `/logs/access`) and `ERROR_LOG_LIST` ("Error Logs",
`/logs/errors`, with `ERROR_LOG_SUMMARY` as a hidden permission underneath it). **The API routes
themselves are unchanged** (`GET /api/dashboard/error-logs`, `/error-logs/summary`,
`/access-logs`) — only where they appear in navigation and which menu row grants them moved; the
codes are the same, so every existing role's grant on them survived the move untouched.

## Menu quick links (2026-07-28)

`Menu` gained `isQuickLink` (bool, default `false`) — see the `quick-menus` section above for the
full behavior change. Exposed on `MenuDto`/`CreateMenuCommand`/`UpdateMenuCommand`, so an admin can
curate additional quick links (or unset a seeded one) via the normal Menu CRUD endpoints. **Needs
a migration**: `dbo.menus.is_quick_link boolean NOT NULL DEFAULT false`.

## Implementation notes (for future maintenance)

- `Application/Dashboard/Dtos/` gained: `EnrollmentStatsDto`, `EnrollmentStatusCountDto`, `GradeEnrollmentCountDto`, `TeacherListWidgetDto`, `DashboardTeacherSummaryDto`, `UserListWidgetDto`, `DashboardUserSummaryDto`, `BarGraphDto`, `BarGraphSeriesDto`, `CurrentAcademicYearDto`, `QuickMenuDto`, and (2026-07-28) `AccountsDashboardSummaryDto`, `FeeInvoiceStatusCountDto`, `CurrentPayrollRunSummaryDto`, `RecentFeePaymentDto`, `HrDashboardSummaryDto`, `EmploymentStatusCountDto`, `EmployeeCategoryCountDto`, `RecentHireDto`, `UpcomingHrEventDto`. `Application/Dashboard/DashboardBarGraphMetrics.cs` holds the allowed `metric` values.
- `Infrastructure/Identity/Services/DashboardService.cs` now also injects `ICurrentUserService` and `UserManager<ApplicationUser>` (needed for the quick-menus role resolution, same pattern as `RoleService.GetUserRolesAsync`). No new repository methods were added to `IStudentRepository`/`ITeacherRepository`/`IEnrollmentRepository`/`IFeeInvoiceRepository`/`IPayrollRunRepository`/`ILeaveRequestRepository`/`IEmployeeRepository` — these are cross-cutting dashboard aggregates, not per-aggregate CRUD queries, so `DashboardService` queries `ApplicationDbContext`/`ApplicationDbContext.Set<T>()` directly for the counts/groupings/sums, the same precedent already set by the existing `GetSummaryAsync` (which queries `_dbContext.Users` directly) and `GetCurrentAcademicYearAsync` (which uses `_dbContext.Set<ClassSection>()` for an entity with no top-level `DbSet` property). The one exception: fetching the current academic year/fiscal year reuses `IAcademicYearRepository.GetCurrentYearsAsync()`/`IFiscalYearRepository.GetCurrentYearAsync()`.
- `Application/Common/Helpers/RecurringDateHelper.cs` (new, 2026-07-28) extracts the "next annual occurrence" calculation that used to be a private method on `EmployeeService` alone, so `EmployeeService.GetEmployeeProfileAsync` (single employee) and `GetHrSummaryAsync` (org-wide) share one implementation.
- No new database tables — every new endpoint reads existing entities (`Student`, `Teacher`, `Enrollment`, `ClassSection`, `AcademicClass`, `AcademicYear`, `Config`, `Menu`, `ApplicationUser`, `ApplicationRoleClaim`, `FeePayment`, `FeeInvoice`, `FeeAdjustment`, `PayrollRun`, `SalarySlip`, `Employee`, `LeaveRequest`, `EmployeeLoan`). The one schema change in this update is `Menu.IsQuickLink` (see above) — everything else needs no migration.
