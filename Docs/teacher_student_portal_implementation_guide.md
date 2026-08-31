# Teacher & Student Portal — implementation guide (2026-08-21)

This is the UI contract for a redesigned Sidebar/Navbar/Dashboard shell for **Teacher** and
**Student** logins — deliberately not a re-skin of the Admin Panel. It complements (never
replaces) the summary in `UI-Implementation-Guide.md`. There is no frontend project inside this
repo (`CMSApp` is the .NET backend only); this guide is what a separate frontend codebase builds
against.

## The three account types, and why "Teacher Portal" isn't a fourth `UserType`

This application only ever has three `UserType` values: `SuperAdmin`, `Admin`, `User`. There is no
`UserType.Teacher` or `UserType.Student` — every portal account (Employee or Student) is
provisioned as plain `User` (see `Docs/portal_account_provisioning_implementation_guide.md`).
"Which portal does this login see" is decided by two independent, already-existing mechanisms —
this round wires the second one up for Students for the first time:

1. **Which menus the account's role(s) are granted** (`POST /api/roles/claims`) — an admin creates
   a `Teacher` role and a `Student` role (arbitrary names, this codebase never hardcodes them) and
   grants each only the menus that persona should see. A Teacher role granted only `MY_WORKSPACE` +
   its children never has an Admin-catalog menu in its tree, so its sidebar is *already* scoped
   correctly with zero frontend logic — this is the main lever for "doesn't feel like the admin
   portal."
2. **Menu audience** (`Menu.MenuFor`: `ADMIN` / `USER` / `BOTH`, enforced in
   `RoleService.GetUserRolesAsync` since the 2026-08-07 privilege-escalation round) — a second,
   coarser filter on top of (1). `ResolveMenuAudienceAsync` derives the caller's audience from
   account linkage, not the raw `UserType` claim: `SuperAdmin`/`Admin`, and any `User`-type account
   linked to an `Employee` (i.e. every Teacher/staff login), resolve to `ADMIN` audience; everything
   else — Student-linked or unlinked — resolves to `USER` audience. **Before this round, every
   seeded menu was hardcoded `MenuFor = ADMIN`**, so this mechanism was a no-op in practice. This
   round is the first to actually populate `MenuFor = USER` rows (the new `STUDENT_PORTAL` tree
   below) and to make `MenuSeedDefinition`/`MainMenu`/`SubMenu` accept an audience per row instead
   of hardcoding `MenuAudience.Admin` everywhere (`Infrastructure/Persistence/DataSeeder/MenuSeeder.cs`).

Practically: a **Teacher** login is an `Employee`-linked `User` account → `ADMIN` audience → sees
whatever Admin-catalog menus its role is granted (in practice, just `MY_WORKSPACE` and its
children, if the admin scoped the Teacher role tightly). A **Student** login is a `Student`-linked
`User` account → `USER` audience → sees the new `STUDENT_PORTAL` tree once its role is granted
those menus (`STUDENT_PORTAL` rows are `MenuFor = USER`, so they're invisible to every Admin/Teacher
login regardless of grants, and vice versa — an Admin-audience menu can never leak into the Student
Portal's sidebar).

## What already existed vs. what this round adds

| Area | Teacher (Employee self-service) | Student (new this round) |
|---|---|---|
| Profile | `GET /api/employees/me/profile` (existing, 2026-08-06) | `GET /api/students/me/profile` (new) |
| Dashboard | `GET /api/employees/me/dashboard` (existing) | `GET /api/students/me/dashboard` (new) |
| Timetable / routine | `GET /api/employees/me/assignments` (existing) | `GET /api/students/me/timetable` (new) |
| Leave | Apply/list/cancel, `GET /api/employees/me/leavebalances` (existing) | *(none — see Deliberately out of scope)* |
| Payslip & Tax | `GET /api/employees/me/payslips`, `.../tax-planning`, `.../tax-details` (existing) | *(none)* |
| My Students (teaching staff only) | `GET /api/students/me`, `me/{id}` (existing, 2026-08-07) | n/a |
| Marks entry (teaching staff only) | `GET /api/exams/me`, `GET/POST/PUT /api/studentexammarks/me...` (existing) | n/a |
| Enrollment history | n/a | `GET /api/students/me/enrollment-history` (new) |
| Fee due summary | n/a | folded into the dashboard (new) |
| Recent exam results | n/a | folded into the dashboard (new) |

The Teacher side needed **no new backend work** — every "Me" endpoint a Staff/Teacher Portal needs
already existed (Employee Self-Service round, 2026-08-06, and Teacher Data Scoping, 2026-08-07).
This round's backend work is entirely the **Student side**, which had zero self-service surface
before it (`StudentsController` only had teacher-facing `me`/`me/{id}` routes for "my students" —
a teacher's own roster, not a student's own record) — plus the `MenuFor` audience wiring above.

## New Student self-service endpoints

All four are `[HttpGet]` on `StudentsController`, no route parameter — the caller's own `Student`
is resolved from the JWT via a new `IStudentRepository.GetByUserIdAsync(userId)` (mirrors
`IEmployeeRepository.GetByUserIdAsync`) and a private `StudentService.ResolveCurrentStudentIdAsync`.
Gated via `appsettings.json`'s `DefaultEnabledMenu` (`"Students": "...,GetMyProfile,GetMyTimetable,
GetMyEnrollmentHistory,GetMyDashboard"`), same "self access to your own data isn't a privilege, no
permission row" convention as every other "Me" route in this codebase — a `Student` role starts
with zero granted permissions and these four still work. 404 (`"Your account is not linked to a
student record."`) if the caller's `ApplicationUser` isn't linked to a `Student` row.

### `GET /api/students/me/profile`

Resolve-then-delegate to the existing `GetStudentByIdAsync` — response is byte-for-byte the same
`StudentDto` shape the admin `GET /api/students/{id}` route returns (profile-header fields +
`CurrentEnrollment`, no `Guardians`/`EnrollmentHistory` — those moved to their own endpoints in the
2026-08-05 profile-optimization round, same rule applies here).

### `GET /api/students/me/timetable`

Delegates to the existing `GetTimetableAsync` — `StudentTimetableDto` (header + `Entries[]` of
subject/teacher/period), identical to `GET /api/students/{id}/timetable`.

### `GET /api/students/me/enrollment-history`

Delegates to the existing `GetEnrollmentHistoryAsync` — `List<StudentEnrollmentHistoryDto>`,
identical to `GET /api/students/{id}/enrollment-history`.

### `GET /api/students/me/dashboard`

New composite endpoint, `StudentService.GetMyDashboardAsync` → `StudentDashboardDto`
(`Application/Students/Dtos/StudentDashboardDto.cs`). One call for the Student Portal's landing
page, mirroring `EmployeeDashboardDto`'s shape/reasoning for the Staff Portal:

```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "...",
  "data": {
    "studentId": "guid",
    "studentName": "Aarav Sharma",
    "currentEnrollment": {
      "enrollmentId": "guid",
      "academicYearId": "guid",
      "academicYearCode": "2082/83",
      "academicYearName": "...",
      "academicClassId": "guid",
      "gradeCode": "GRADE_5",
      "gradeLabel": "Grade 5",
      "classSectionId": "guid",
      "sectionCode": "A",
      "sectionLabel": "A",
      "rollNumber": 12,
      "enrollmentDate": "2082-04-01"
    },
    "totalOutstandingAmount": 4500.00,
    "nextFeeDueDate": "2082-05-10",
    "openInvoiceCount": 2,
    "recentResults": [ /* StudentResultDto[], newest exam term first, capped to 5 */ ],
    "upcomingEvents": [
      { "type": "Birthday", "label": "Birthday", "date": "2082-05-02" },
      { "type": "Holiday", "label": "Constitution Day", "date": "2082-05-03" },
      { "type": "Festival", "label": "Dashain", "date": "2082-05-20" }
    ]
  }
}
```

Field notes:

- `currentEnrollment` is `null` when the student has no active (`Status == Enrolled`) enrollment
  anywhere — same "profile header" building block `GetStudentByIdAsync` already uses
  (`StudentService.BuildCurrentEnrollmentAsync`/`ResolveActiveEnrollmentAsync`), reused verbatim so
  this figure can never disagree with the profile page's own.
- `totalOutstandingAmount`/`nextFeeDueDate`/`openInvoiceCount` come from
  `IFeeInvoiceRepository.GetOpenByEnrollmentAsync` (Generated/Pending/PartiallyPaid invoices,
  oldest first) scoped to the active enrollment only — same `NetAmount - PaidAmount` formula used
  throughout `FeeInvoiceService`. A student with no active enrollment gets `0`/`null`/`0`, not an
  error.
- `recentResults` reuses `IExamTermRepository.GetResultsPagedByFilterAsync(null, null,
  enrollmentId, 1, 5)` and the existing `ExamMapper.ToStudentResultDto` — same `StudentResultDto`
  shape the admin `GET /api/examresults?enrollmentId=` route returns. **Scoped to the active
  enrollment only** — a student's results from a prior grade (a different, historical enrollment
  after promotion) don't appear here; use `GET /api/examresults?enrollmentId={id}` per historical
  enrollment id (from `GET /api/students/me/enrollment-history`) for that.
- `upcomingEvents` is a 30-day forward window, capped to 10, sorted by date — the student's own
  next birthday (live-computed from `DateOfBirth`, same `RecurringDateHelper.ResolveNextOccurrence`
  the Employee dashboard uses) plus school-wide `PublicHoliday`/`InternalEvent` calendar entries and
  `FestivalOccurrence` rows. **Deliberately school-wide only** — `CalendarEvent.ProvinceCode`/
  `BranchCode` scoping (used to narrow an Employee's events to their own branch/province) is skipped
  for students, since `Student` has no `ProvinceCode`/`BranchCode` fields to scope against; only
  events with both fields null (school-wide) are included.

### `STUDENT_MY_DASHBOARD` is also wired into the generic widget registry

`Application/Dashboard/Widgets/StudentDashboardWidgetProvider.cs` adapts
`GetMyDashboardAsync` into the `GET /api/dashboard/widgets` envelope (2026-08-07 registry), so a
Student-audience role that's granted the `STUDENT_MY_DASHBOARD` menu gets it composed there too,
same as `MY_DASHBOARD` does for the Staff Portal. Use whichever call fits the frontend's data-
loading shape — both return the same underlying data.

## Menu catalog changes

`Infrastructure/Persistence/DataSeeder/MenuSeeder.cs`:

- `MenuSeedDefinition` gained a `MenuFor` field (defaults to `MenuAudience.Admin`); `MainMenu`/
  `SubMenu` gained an optional trailing `menuFor` parameter. The two places that used to hardcode
  `MenuFor = MenuAudience.Admin` on every row (insert pass and sync pass) now read
  `definition.MenuFor` instead — **every pre-existing catalog row keeps behaving exactly as
  before** (the parameter defaults to Admin when omitted), this is additive.
- New `STUDENT_PORTAL` main menu (`MenuFor = MenuAudience.User`, order 17, icon
  `icons.ReadOutlined`), with four `SUB_MENU` children (all also `MenuFor = MenuAudience.User`):

  | Code | Display name | Suggested route | Controller/Action |
  |---|---|---|---|
  | `STUDENT_MY_DASHBOARD` | My Dashboard | `/apps/student-portal/dashboard` | `Students/GetMyDashboard` |
  | `STUDENT_MY_PROFILE` | My Profile | `/apps/student-portal/profile` | `Students/GetMyProfile` |
  | `STUDENT_MY_TIMETABLE` | My Timetable | `/apps/student-portal/timetable` | `Students/GetMyTimetable` |
  | `STUDENT_MY_ENROLLMENT_HISTORY` | My Enrollment History | `/apps/student-portal/history` | `Students/GetMyEnrollmentHistory` |

  `STUDENT_MY_DASHBOARD` also carries `IsQuickLink = true`/`IsDashboardWidget = true`, same as
  `MY_DASHBOARD` under `MY_WORKSPACE`.

- **These rows exist purely for the nav tree** — same "not itself an authorization gate" reasoning
  as `MY_WORKSPACE`'s children: `AuthorizedAction` never checks them for the four actions above,
  since all four are `DefaultEnabledMenu`-listed and already work for any Student-linked login
  regardless of grant. Grant `STUDENT_PORTAL` and its children to the seeded `Student` role (or any
  future student-facing role) via `POST /api/roles/claims` so the sidebar actually shows the links —
  the API access itself already works either way, exactly like `MY_WORKSPACE`.

**No migration needed** — `Menu.MenuFor` is an existing column (already `NOT NULL varchar`), this
only changes what value the seeder writes into it, and only for the four new rows plus the one new
main menu.

## Sidebar / Navbar / Dashboard information architecture

This is what the separate frontend project should build. See the companion visual mockup
(published as a Claude Artifact in the same session this guide was written) for a concrete layout;
the structure below is the authoritative contract.

### Admin Portal (unchanged)

Full catalog-driven sidebar exactly as today — `Dashboard`, `Logs`, `User Management`,
`Master Settings`, `Setup`, `Student Management`, `Accounts`, `Employee Management`,
`Calendar Management`, `Exam Management`, `Leave Management`, plus `My Workspace`/`My Portal` if
the signed-in admin also happens to be Employee/Student-linked. No shell change.

### Staff / Teacher Portal shell

A **different chrome**, not a different API: same `GET /api/roles/user-menus` call, but the tree
returned to a Teacher-role login is small (in practice just `MY_WORKSPACE`'s children, once an
admin scopes the Teacher role tightly) — the frontend should detect "this account's granted tree is
exactly the My Workspace subtree" (or, simpler: branch shell purely on account type — Employee-
linked vs. Student-linked vs. neither — which the profile endpoints already expose) and render a
lighter shell:

- **Sidebar**: My Dashboard · My Profile · Leave & Balance · Payslip & Taxes · My Classes · (My
  Students, only if `IsTeachingStaff` — already on `EmployeeDto`). No admin branding, no dense
  multi-level tree — flat list, five to six items.
- **Navbar**: notification bell (`GET /api/employees/me` — actually
  `GET /api/notifications`-equivalent is Employee-scoped, see
  `Docs/leave_management_and_employee_profile_implementation_guide.md`'s Notification section),
  profile photo + name + job-position label dropdown (Change Password / Set Password / Logout). No
  global search, no quick-links rail, no "Ctrl+K" — those are admin-density affordances this portal
  shouldn't carry.
- **Dashboard** (`GET /api/employees/me/dashboard`): leave balance tiles, pending leave requests,
  today's/next class (`NextClass`, best-effort — see that field's own doc comment: this codebase has
  no day-of-week timetable, so "next class" is the next configured period today, not "next class
  this week"), upcoming holidays/events. Quick actions: Apply Leave, View Payslip.

### Student Portal shell (new)

- **Sidebar**: My Dashboard · My Profile · My Timetable · My Enrollment History. Four items,
  deliberately minimal — see "Deliberately out of scope" below for what's *not* here yet.
- **Navbar**: profile dropdown only (name, class/section, Change Password/Logout) — **no
  notification bell for students** (see gap below), no global search, no admin affordances at all.
- **Dashboard** (`GET /api/students/me/dashboard`): current class/section/roll-number tile, fee-due
  summary tile (`totalOutstandingAmount`/`nextFeeDueDate` — link to a "pay fees" flow if one exists
  outside this repo), recent exam results list, upcoming events list. No quick actions (a student
  portal has nothing to "quick-create").

## Deliberately out of scope (this round)

- **Fee statement / payment history detail, exam-result detail-by-term, and document viewing for
  students.** The dashboard surfaces *summaries* (outstanding total, recent results) by design —
  drilling into a full `GET /api/feeinvoices/account-statement/{enrollmentId}` ledger or a full
  `GET /api/examresults?enrollmentId=` per-term breakdown from the Student Portal is a natural
  next step, reusing those existing admin endpoints (they take an `enrollmentId`, which
  `GET /api/students/me/profile`'s `currentEnrollment.enrollmentId` already supplies) — just not
  wired into a `me/...` route yet. No backend blocker; add a thin `me/...` wrapper the same way this
  round did for profile/timetable/enrollment-history when the frontend needs it.
- **Student notifications.** `Notification` (`Application/Notifications/`) is Employee-scoped only
  (`EmployeeId` FK, no `StudentId` column) — extending it to Students needs a schema change
  (nullable `StudentId` alongside `EmployeeId`, or a second notification table) and isn't done here.
  Until then, a Student Portal navbar has nothing to badge; leave the bell off rather than fake one.
- **Guardian-facing views.** A `Guardian` can be linked to several `Student`s (`StudentGuardian`),
  but guardians have no portal login concept anywhere in this codebase (`Guardian` has no `UserId`,
  unlike `Employee`/`Student`) — a parent portal is a materially larger feature (new provisioning
  path, new "which students can this guardian see" scoping), not attempted here.
- **Leave/attendance for students.** No attendance module exists in this codebase at all (called
  out already in the Leave Management round's own "Known gaps"); nothing to expose here either.
- **Role-scoping the Teacher role automatically.** This guide doesn't create a `Teacher`/`Student`
  role or grant menus to it — that stays an admin action via the existing
  `POST /api/roles`/`POST /api/roles/claims` flow, same as every other department-specific role
  this codebase already supports (Accounts, HR, ...).

## Files touched this round

- `Domain/Interfaces/IStudentRepository.cs` / `Infrastructure/Persistence/Repositories/StudentRepository.cs` — `GetByUserIdAsync`.
- `Application/Students/Dtos/StudentDashboardDto.cs` — new.
- `Application/Students/IStudentService.cs` / `StudentService.cs` — `GetMyProfileAsync`/`GetMyTimetableAsync`/`GetMyEnrollmentHistoryAsync`/`GetMyDashboardAsync` + `ResolveCurrentStudentIdAsync`.
- `WebApi/Controllers/StudentsController.cs` — four `me/...` routes.
- `WebApi/appsettings.json` — extended the `"Students"` `DefaultEnabledMenu` entry.
- `Infrastructure/Persistence/DataSeeder/MenuSeeder.cs` — `MenuFor` threading + `STUDENT_PORTAL` catalog.
- `Application/Dashboard/Widgets/StudentDashboardWidgetProvider.cs` + `Application/DependencyInjection.cs` registration.
