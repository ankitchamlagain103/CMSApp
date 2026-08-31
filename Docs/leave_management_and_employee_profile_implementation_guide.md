# Leave Management, Notifications & Employee Profile — implementation guide

**2026-07-23.** Built from a user-supplied schema sketch (`employees`/`leave_types`/
`employee_leave_balance`/`leave_requests`/`leave_substitutes`/`holidays`/`notifications`) and an
Employee Profile page mockup, adapted to this codebase's conventions. Everything here is
**admin-facing** — there is still no employee-login feature in this codebase (`Employee.UserId` is
forward-looking and unpopulated, same as ever), so every endpoint below is reached through
`/api/employees/{id}/...`, the same shape every other Employee sub-resource (salaries, loans,
documents, qualifications) already uses. Once employee self-service login exists, these same
endpoints work unchanged for a "my profile"/"my leave" self-service UI — only the caller's
identity resolution changes, not the API surface.

## What changed vs. the raw sketch (deliberate deviations)

- **`holidays` was not built as its own table.** Per instruction, it reuses the existing Dual
  Calendar module: `CalendarEvent.EventType == PublicHoliday` (already existed) now optionally
  carries `ProvinceCode`/`BranchCode` (both Config-catalog codes, both null = applies everywhere)
  for the scoping the sketch's `province`/`branch` columns asked for. See "Calendar extension"
  below.
- **`employee_leave_balance` gained a `FiscalYearId`** the sketch didn't have. An unscoped
  "allocated 18 days" is meaningless once the year rolls over — this codebase already has a
  `FiscalYear` entity built exactly for periodic payroll/leave resets, reused here instead of
  inventing a parallel "leave year" concept.
- **The `employees` sketch's `department_id`/`branch_id`/`manager_id`** map onto the existing
  `Employee` entity directly rather than new tables: `EmployeeCategoryCode` already *is*
  "Department" (Config catalog `1011`), `JobPositionCode` already *is* "Designation" (`1012`).
  New: `BranchCode`/`ProvinceCode`/`LevelCode` (Config catalogs `1019`/`1020`/`1021`) and
  `ManagerId` (a real self-referencing `Employee` FK, Restrict delete — same pattern as `Menu`'s
  self-referencing `ParentId`).
- **`leave_requests.manager_status`/`hr_status`**: two independent one-shot fields, not a single
  status column. See "Approval workflow" below for the exact derivation/override rules.

## Employee "org" fields & photo

`EmployeeDto`/`CreateEmployeeCommand`/`UpdateEmployeeCommand` gained:

| Field | Notes |
|---|---|
| `branchCode` | Config catalog `1019` — type-only, admin-created (`POST /api/configs`), same split as Grade/Section (school-specific). |
| `provinceCode` | Config catalog `1020` — seeded with Nepal's 7 federal provinces. |
| `levelCode` | Config catalog `1021` — seeded with a generic Junior/Mid/Senior/Lead/Executive ladder, admin-extensible. |
| `managerId` | Another `Employee`'s id. Can't be the employee's own id. `EmployeeDto.managerName` is resolved on `GET /api/employees/{id}` (not on the paged list, same "only when the nav is loaded" convention `hasTeacherProfile` already uses). |

All four fields are optional on both create and update.

**2026-07-24 addendum:** `provinceCode` is now also the anchor of a full address chain —
`districtCode`/`localLevelCode` (Config catalogs `1022`/`1023`) and a plain `wardNo` were added
alongside it, with automatic district/province derivation from whichever field is most specific.
See `employee_address_implementation_guide.md` for the full reference; unchanged otherwise.

**Profile photo** (`Employee.PhotoPath`, never exposed directly):

```
POST   /api/employees/{id}/photo             multipart/form-data, field "file", jpg/jpeg/png only, max 10 MB
GET    /api/employees/{id}/photo/download     streams the raw image (no envelope on success)
DELETE /api/employees/{id}/photo
```

Re-uploading replaces the previous photo (old file deleted only after the new one saves
successfully). `EmployeeDto.hasPhoto` (bool) tells the UI whether to render an `<img>` pointed at
the download endpoint or a placeholder avatar.

## Leave Types — `/api/leavetypes` (master data)

Real typed columns, not a Config catalog — `daysPerYear`/`carryForward`/`isPaid` are structured
facts a generic Code/Label/3-AdditionalValue row can't express cleanly (same reasoning
`ClassSubject`'s grading columns used over `Config.AdditionalValue1`).

```
POST/GET   /api/leavetypes                CRUD, soft-deleted
GET/PUT    /api/leavetypes/{id}
DELETE     /api/leavetypes/{id}           409 while any balance/request still references it
```

Body/response shape:

```json
{ "id": "…", "name": "Annual Leave", "daysPerYear": 18.0, "carryForward": true, "isPaid": true }
```

**Seeded on first boot** (`LeaveTypeSeeder`, create-if-missing by `Name`, illustrative — verify
against your actual policy): Annual Leave (18, carry-forward, paid), Sick Leave (12, no
carry-forward, paid), Casual Leave (12, no carry-forward, paid) — matching the profile mockup's
`11/18`, `11/12`, `8/12` figures.

**2026-07-24 addendum:** `LeaveType` gained three policy fields (`maxConsecutiveDays`/
`maxDaysPerWeek`/`maxDaysPerMonth`) and `LeaveRequest` gained `isEmergency` (unconditionally
bypasses those three caps for any leave type); two new seeded types, Bereavement Leave and
Marriage Leave. See `leave_configurability_implementation_guide.md` for the full reference — this
section's endpoint shapes are otherwise unchanged.

**2026-08-24: `GET /api/leavetypes` moved to `DefaultEnabledMenu`.** Found while testing the
self-service Apply Leave modal (`MyLeaveApplyModal`): a Teacher/self-service login had no
`LEAVE_TYPE_LIST` grant, so the Leave Type dropdown 403'd every time — nobody could actually
submit a leave request through the self-service flow. `LEAVE_TYPE_LIST`'s admin CRUD
(create/update/delete/detail) stays permission-gated as before; only the read-only list moved,
same "every page needs this, don't gate it behind a permission row" reasoning `Configs:
GetConfigsByTypeCode`/`AcademicYears: GetAcademicYears` already established.

**2026-08-24: new `GET /api/employees/lookup?search=&limit=`** — a minimal, `DefaultEnabledMenu`-
gated employee search for picker UIs (`EmployeePicker.jsx`'s Manager/Substitute autocomplete),
found in the same testing pass: the Substitute picker on this same Apply Leave modal called the
full `GET /api/employees` list, which requires `EMPLOYEE_LIST` — another 403 for the same
self-service caller, and even for an admin caller it was needlessly returning PAN/SSF/CIT/
bank-account fields just to populate an autocomplete. Response: `EmployeeLookupDto[]` — `{ id,
firstName, middleName, lastName, employeeCode, jobPositionCode, jobPositionLabel }`, nothing more.
`GET /api/employees` itself is unchanged and still requires `EMPLOYEE_LIST` — this is an
additional, narrower endpoint, not a replacement.

## Leave Balances — `/api/employees/{id}/leavebalances`

```
POST /api/employees/{id}/leavebalances                allocate/re-allocate (upsert)
GET  /api/employees/{id}/leavebalances?fiscalYearId=   list (defaults to the IsCurrent fiscal year)
```

`POST` body: `{ "leaveTypeId": "…", "fiscalYearId": null, "allocated": 18 }` — `fiscalYearId`
optional (blank = current year, same convention `GetCurrentSalaryTaxCalculationAsync` uses).
Re-posting for the same employee/leave-type/year updates `allocated` in place and recomputes
`balance`; it does **not** touch `used`/`pending`.

Response (`EmployeeLeaveBalanceDto`):

```json
{
  "id": "…", "employeeId": "…", "leaveTypeId": "…", "leaveTypeName": "Annual Leave",
  "fiscalYearId": "…", "fiscalYearCode": "2084/85",
  "allocated": 18.0, "used": 7.0, "pending": 3.0, "balance": 8.0
}
```

`balance = allocated - used - pending`, recalculated by the service on every mutation (create,
approve, reject, cancel) — never trust a stale client-cached value.

## Leave Requests — `/api/employees/{id}/leaverequests`

```
POST   /api/employees/{id}/leaverequests                                   apply (multipart/form-data)
GET    /api/employees/{id}/leaverequests?managerStatus=&hrStatus=&isPending=
GET    /api/employees/{id}/leaverequests/{requestId}
POST   /api/employees/{id}/leaverequests/{requestId}/manager-approve       { "remarks": null }
POST   /api/employees/{id}/leaverequests/{requestId}/manager-reject
POST   /api/employees/{id}/leaverequests/{requestId}/hr-approve
POST   /api/employees/{id}/leaverequests/{requestId}/hr-reject
POST   /api/employees/{id}/leaverequests/{requestId}/cancel                only while fully Pending
POST   /api/employees/{id}/leaverequests/{requestId}/substitutes           { "employeeId": "…", "responsibility": "…" }
DELETE /api/employees/{id}/leaverequests/{requestId}/substitutes/{substituteId}
```

### Apply (`POST .../leaverequests`, multipart/form-data)

| Form field | Required | Notes |
|---|---|---|
| `leaveTypeId` | ✅ | |
| `fromDate` / `toDate` | ✅ | `toDate >= fromDate`; `days` is computed inclusively (`toDate - fromDate + 1`) |
| `reason` | ❌ | ≤1000 chars |
| `substituteEmployeeId` | ❌ | Quick single "who's covering" reference — can't be the requester themselves |
| `isEmergency` | ❌ | (2026-07-24) Default `false` — see below; **not** the same "emergency" as HR's override in the approval workflow. |
| `attachment` | ❌ | PDF/JPG/JPEG/PNG, max 10 MB — same `DocumentFileRules` as employee/student documents |

Rejected with `409 Conflict` if the employee already has a non-Rejected request overlapping the
same date range. Notifications are raised for the requester and (if `Employee.ManagerId` is set)
the manager.

**Two unrelated "emergency" concepts — don't conflate them.** `isEmergency` here is the
*requester's own claim at submission time*, and only affects whether the request can exceed its
leave type's `maxConsecutiveDays`/`maxDaysPerWeek`/`maxDaysPerMonth` caps (see
`leave_configurability_implementation_guide.md`) — it plays no role in approval. The "emergency
override" in the Approval workflow section below is a completely different thing: HR's ability to
decide a request regardless of the manager's status. A request can be `isEmergency = true` and
still go through the perfectly normal manager-then-HR flow, and a request with `isEmergency = false`
can still be HR-approved ahead of the manager.

### Approval workflow (the important part)

`ManagerStatus` and `HrStatus` are **two independent one-shot fields** (`Pending` →
`Approved`/`Rejected`, each field only ever decided once — a second decision on an already-decided
field is rejected with `409`). Per explicit instruction ("in emergency cases HR should be able to
directly approve"):

- **`HrStatus` is authoritative.** Only an HR decision actually consumes the leave balance
  (`used`/`pending` mutation) and only an HR decision determines whether the leave is really
  granted. `ManagerStatus` is advisory/informational — it does **not** gate HR's endpoint at all.
- **Normal flow**: Manager decides first (`manager-approve`/`manager-reject`), then HR decides
  (`hr-approve`/`hr-reject`).
- **Emergency override**: HR can call `hr-approve`/`hr-reject` at any time, including before the
  manager has acted, or *after* the manager rejected. HR's decision always wins.
- **`effectiveStatus`** on `LeaveRequestDto` is computed (never persisted, so it can't drift):
  `HrStatus` if it's `Approved`/`Rejected`; else `Rejected` if `ManagerStatus == Rejected`
  (a provisional rejection HR can still override by approving); else `Pending`.

Balance effects: applying (`POST`) bumps `pending` by `days` (best-effort — silently skipped if no
balance row exists for that leave type/current fiscal year, so a leave type with no allocation
doesn't block submission); an HR **approve** moves that amount from `pending` to `used`; an HR
**reject** or a **cancel** (only allowed while both fields are still `Pending`) releases it back
out of `pending`.

Response shape (`LeaveRequestDto`):

```json
{
  "id": "…", "employeeId": "…", "employeeName": "Priya Sharma",
  "leaveTypeId": "…", "leaveTypeName": "Sick Leave",
  "fromDate": "2026-08-01", "toDate": "2026-08-02", "days": 2.0, "reason": "Fever",
  "substituteEmployeeId": "…", "substituteEmployeeName": "Ram Thapa",
  "managerStatus": 1, "managerRemarks": null, "managerDecisionTs": null, "managerDecisionBy": null,
  "hrStatus": 1, "hrRemarks": null, "hrDecisionTs": null, "hrDecisionBy": null,
  "effectiveStatus": 1,
  "hasAttachment": false, "attachmentFileName": null,
  "substitutes": []
}
```

`managerStatus`/`hrStatus`/`effectiveStatus` are the raw `LeaveApprovalStatus` int (`1` Pending /
`2` Approved / `3` Rejected) — no string enum converter is registered anywhere in this API, same
as every other enum field in this codebase.

### Substitutes (the fuller multi-person breakdown)

`substituteEmployeeId` on the request itself is the one quick reference the Apply form sets;
`POST/DELETE .../substitutes` manage the fuller `LeaveSubstitute` child list (multiple colleagues,
each with their own `responsibility` text) for when more than one person splits the requester's
duties. Both coexist deliberately — see `LeaveRequest`'s own doc comment in the code.

## Notifications — `/api/employees/{id}/notifications`

```
GET  /api/employees/{id}/notifications?isRead=&page=&pageSize=
POST /api/employees/{id}/notifications/{notificationId}/read
POST /api/employees/{id}/notifications/read-all
```

System-raised only (no manual create endpoint) — `LeaveRequestService`-side code calls
`INotificationService.CreateNotificationAsync` as a side effect of leave-workflow transitions
(submitted, manager decision, HR decision). `type` is a `NotificationType` enum (`General`,
`LeaveRequestSubmitted`, `LeaveManagerApproved`, `LeaveManagerRejected`, `LeaveHrApproved`,
`LeaveHrRejected`, `BirthdayReminder`, `WorkAnniversaryReminder` — the last two are reserved for a
future scheduled job, nothing raises them yet, see "Deliberately out of scope" below). Hard-deleted
(an ephemeral inbox item, not an audit record).

## Employee Profile — `GET /api/employees/{id}/profile`

One composite call for the whole profile page (`EmployeeProfileDto`), matching the mockup layout:

```json
{
  "id": "…", "hasPhoto": true,
  "name": "Priya Sharma", "dateOfBirth": "1990-08-12", "phone": "9800000000", "email": "priya@school.local",
  "employeeCode": "EMP2026007", "levelCode": "SENIOR", "jobPositionCode": "TEACHER",
  "employeeCategoryCode": "ACADEMIC", "branchCode": "MAIN", "provinceCode": "BAGMATI",
  "joinDate": "2020-07-21", "servicePeriod": "6 years 0 months",
  "managerId": "…", "managerName": "Ram Thapa",
  "leaveSummary": [
    { "leaveTypeId": "…", "leaveTypeName": "Annual Leave", "used": 7.0, "allocated": 18.0, "balance": 8.0 }
  ],
  "upcomingEvents": [
    { "type": "Birthday", "label": "Birthday", "date": "2027-08-12" },
    { "type": "WorkAnniversary", "label": "Work Anniversary", "date": "2027-07-21" }
  ],
  "pendingLeaveRequests": [ /* LeaveRequestDto[], filtered to managerStatus == Pending || hrStatus == Pending */ ]
}
```

- **`servicePeriod`** — whole years/months only (`"6 years 0 months"`), computed live from
  `joinDate` against today; null when `joinDate` is null.
- **`upcomingEvents`** — computed live from `dateOfBirth`/`joinDate` (next annual occurrence,
  rolling into next year once this year's date has passed) — **no persisted row is needed** for
  an employee's own profile page to show their own next birthday/anniversary. `WorkAnniversary`
  only appears once at least one full year has elapsed since `joinDate` (a brand-new hire has no
  "anniversary" yet).
- **`leaveSummary`** — always scoped to the current fiscal year (`FiscalYear.IsCurrent`); empty if
  no fiscal year is marked current or no balances have been allocated yet.
- **Designation/Department show as raw codes** (`jobPositionCode`/`employeeCategoryCode`) — no
  resolved-label convention exists for these two anywhere else in this codebase either; the
  frontend already has to resolve catalog labels via `GET /api/configs/dropdown/{typeCode}` for
  every other code field, same here.

**"Quick Actions" from the mockup (Apply Leave / View Leave Balance / Assign Substitute / View
Attendance / Download Leave History) are a frontend concern** — four of the five are just buttons
linking to the endpoints above (`Assign Substitute` → the substitutes endpoint; `Download Leave
History` → client-side CSV from `GET .../leaverequests`, same "no server-side export" convention
`payroll_fixes_implementation_guide.md` already documents for payslip/Excel export). **"View
Attendance" has no backend today** — there is no Attendance module in this codebase at all (see
"Deliberately out of scope" below); that quick-action button has nothing to link to yet.

## Calendar extension — Holidays & Birthdays

`CalendarEvent` (`POST/PUT /api/calendar/events`) gained four fields:

| Field | Meaning |
|---|---|
| `provinceCode` / `branchCode` | Config codes (`1020`/`1019`). Scope a `PublicHoliday` event — both null = applies everywhere. Meaningless (and rejected) on any other `EventType`. |
| `studentId` / `employeeId` | Pin a `StudentBirthday`/`EmployeeBirthday` event to a specific person. Exactly one is required when `eventType` is the matching birthday type; both are rejected on every other type. |

Two new `CalendarEventType` values: `StudentBirthday = 3`, `EmployeeBirthday = 4` (holidays
already had `PublicHoliday = 1` — no new type needed there, just the two new scoping fields).

`GET /api/calendar/events` gained `studentId`/`employeeId` query filters, so "show this employee's
pinned birthday events" is a direct server-side filter, not a client-side scan.

**These manually-pinned calendar rows are separate from, and never required for, the Employee
Profile page's own live-computed `upcomingEvents`** (see above) — pinning a birthday onto the
shared calendar is for a *school-wide* "whose birthday is this month" view (the month-view/
calendar page everyone sees), while the profile page's figure is always computed fresh from that
one employee's own `dateOfBirth`/`joinDate`, with nothing to keep in sync.

## Permissions (seeded to SuperAdmin)

New main menu `LEAVE_MANAGEMENT` → sub-menu `LEAVE_TYPE_LIST` →
`LEAVE_TYPE_CREATE/DETAIL/UPDATE/DELETE`. Everything else (photo, leave balances, leave requests,
substitutes, notifications, profile) is permission rows under the existing `EMPLOYEE_LIST`
sub-menu (`EMPLOYEE_PHOTO_*`, `EMPLOYEE_LEAVE_BALANCE_*`, `EMPLOYEE_LEAVE_REQUEST_*`,
`EMPLOYEE_LEAVE_SUBSTITUTE_*`, `EMPLOYEE_NOTIFICATION_*`, `EMPLOYEE_PROFILE_VIEW`) — same
reasoning as Loans/Adjustments/Documents/Qualifications before them: no dedicated cross-employee
list action exists for any of these, so they don't get their own sub-menu.

## Deliberately out of scope

- **No Attendance module** — not sketched, not built. The mockup's "View Attendance" quick action
  has nothing to link to.
- **No employee-login / self-service** — every endpoint here is admin-facing, reached via
  `/api/employees/{id}/...`, same as the rest of this codebase. `managerDecisionBy`/
  `hrDecisionBy` record the *calling admin's* username (`ICurrentUserService.UserName`), not a
  verified "you are actually this employee's manager" check — there is no such check today,
  matching the codebase-wide convention that permission-gating, not identity-matching, is the
  authorization model until real employee login exists.
- **Leave balance scoping is always the CURRENT fiscal year**, never a specific request's own
  `fromDate`-implied year. Resolving which fiscal year a given date range falls into would need a
  date-range lookup this codebase's `IFiscalYearRepository` doesn't have yet — a reasonable
  follow-up if leave ever needs to span a fiscal-year boundary correctly.
- **No scheduled job raises `BirthdayReminder`/`WorkAnniversaryReminder` notifications** — those
  `NotificationType` values exist for a future cron-style job; today, birthdays/anniversaries only
  surface via the profile page's live computation or a manually-pinned calendar event.
- **Balance is best-effort, not a hard submission gate** — applying for leave when the balance is
  already exhausted (or no balance row exists at all) is not blocked; `pending`/`used` can go
  negative-clamped-to-zero on release, but nothing stops an over-allocation from being approved.
  Consistent with this feature's general "HR has final discretion" design.

## Migration required (not created here — user-owned, as always)

New tables: `dbo.leave_types`, `dbo.employee_leave_balances` (unique
`(employee_id, leave_type_id, fiscal_year_id)`), `dbo.leave_requests`, `dbo.leave_substitutes`,
`dbo.notifications`. New columns: `dbo.employees.branch_code/province_code/level_code/manager_id/
photo_path` (`manager_id` FK to `employees.id`, Restrict); `dbo.calendar_events.province_code/
branch_code/student_id/employee_id` (`student_id` FK to `students.id`, `employee_id` FK to
`employees.id`, both Restrict). Every endpoint in this guide 500s until these exist.
