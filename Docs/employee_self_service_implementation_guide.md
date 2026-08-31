# CMSApp — Employee Self-Service ("My ...") (UI)

**2026-08-06.** Any login whose `ApplicationUser` is linked to an `Employee` row
(`Employee.UserId`, set via the portal-account-provisioning flow — see
`Docs/portal_account_provisioning_implementation_guide.md`) can now see and act on their own
record — profile, leave balance, leave requests, payslips/taxes, and their own class assignments —
**without needing any permission grant**. This is deliberately **not** a teacher feature: a
Teacher is just an `Employee` with a particular `JobPositionCode`, and every other staff role
(Accountant, HR, Principal, Vice Principal, ...) gets exactly the same self-service surface. Which
persona someone is comes entirely from their assigned **role**, same as everywhere else in this
app — nothing here branches on `UserType` or checks for a "teacher" concept.

## The routes: 12 (2026-08-06) + Dashboard (2026-08-07) + Documents/Qualifications (2026-08-07)

All under `EmployeesController`, no `{id}` route parameter — the caller's own `Employee` is
resolved server-side from the JWT identity (`ICurrentUserService.UserId` → `Employee.UserId`).

| Route | Verb | Mirrors (admin route) | Response shape |
|---|---|---|---|
| `/api/employees/me/dashboard` | GET | `GET /api/employees/{id}/dashboard` | `EmployeeDashboardDto` |
| `/api/employees/me/profile` | GET | `GET /api/employees/{id}/profile` | `EmployeeProfileDto` |
| `/api/employees/me/leavebalances` | GET | `GET /api/employees/{id}/leavebalances` | `List<EmployeeLeaveBalanceDto>` |
| `/api/employees/me/leaverequests` | POST | `POST /api/employees/{id}/leaverequests` | `LeaveRequestDto` |
| `/api/employees/me/leaverequests` | GET | `GET /api/employees/{id}/leaverequests` | `List<LeaveRequestDto>` |
| `/api/employees/me/leaverequests/{requestId}` | GET | `GET /api/employees/{id}/leaverequests/{requestId}` | `LeaveRequestDto` |
| `/api/employees/me/leaverequests/{requestId}/cancel` | POST | `POST /api/employees/{id}/leaverequests/{requestId}/cancel` | `bool` |
| `/api/employees/me/payslips` | GET | `GET /api/employees/{id}/payslips` | `List<PayslipSummaryDto>` |
| `/api/employees/me/payslips/{fiscalYearId}/{monthIndex}` | GET | `GET /api/employees/{id}/payslips/{fiscalYearId}/{monthIndex}` | `PayslipDetailDto` |
| `/api/employees/me/salaries/payslip-preview` | GET | `GET /api/employees/{id}/salaries/payslip-preview` | `DocumentPreviewDto` |
| `/api/employees/me/salaries/tax-planning` | GET | `GET /api/employees/{id}/salaries/tax-planning` | `TaxPlanningDto` |
| `/api/employees/me/salaries/tax-details` | GET | `GET /api/employees/{id}/salaries/tax-details` | `TaxDetailsGridDto` |
| `/api/employees/me/assignments` | GET | `GET /api/employees/{id}/assignments` | `List<TeacherAssignmentDto>` |
| `/api/employees/me/qualifications` | POST | `POST /api/employees/{id}/qualifications` | `EmployeeQualificationDto` (starts `verificationStatus: Pending`) |
| `/api/employees/me/qualifications` | GET | `GET /api/employees/{id}/qualifications` | `List<EmployeeQualificationDto>` |
| `/api/employees/me/documents` | POST | `POST /api/employees/{id}/documents` | `EmployeeDocumentDto` (starts `verificationStatus: Pending`) |
| `/api/employees/me/documents` | GET | `GET /api/employees/{id}/documents` | `List<EmployeeDocumentDto>` |
| `/api/employees/me/documents/{documentId}/download` | GET | `GET /api/employees/{id}/documents/{documentId}/download` | raw file stream |

**Request/response shapes are byte-for-byte identical to their admin-route counterparts** —
these are thin wrappers (`EmployeeService.GetMyProfileAsync` etc.) that resolve the caller's own
`employeeId` then call the exact same underlying logic the `{id}`-scoped route already uses (for
documents/qualifications specifically, both routes funnel through one shared private method that
only the initial `VerificationStatus` differs on — see `employee_documents_and_qualifications_implementation_guide.md`).
Every field is already documented wherever that admin route is documented
(`employee_management_implementation_guide.md`, `pay_and_taxes_implementation_guide.md`,
`leave_management_and_employee_profile_implementation_guide.md`,
`employee_teaching_profile_and_assignments_implementation_guide.md` for assignments,
`employee_documents_and_qualifications_implementation_guide.md` for documents/qualifications) —
nothing new to look up.

`POST /api/employees/me/leaverequests` and `POST /api/employees/me/documents` are both
**multipart/form-data** — the leave route takes `leaveTypeId`, `fromDate`, `toDate`, `reason`,
`substituteEmployeeId` (optional), `isEmergency`, `attachment` (optional file); the document
route takes `file`, `documentTypeCode`, `documentName`, `validUntil` (optional), `remarks`
(optional) — identical fields to the admin upload.

**Deliberately not exposed as self-service**: manager/HR approve-reject decisions on leave
requests, document/qualification **verify/reject** decisions, salary-structure editing, loans,
photo management, notifications-as-admin-view, and self-service **delete** of a submitted
document/qualification (fix a mistake by asking an admin to remove it, then re-submit). Those
remain `{id}`-scoped, admin/HR-only routes — an employee can submit and view their *own* records,
but deciding on them is still someone else's job.

**404, not 500, for an unlinked account**: if the caller's `ApplicationUser` has no linked
`Employee` (a Student-only login, or an Admin/SuperAdmin account with no HR record), every route
above returns `404 NotFound` with `"Your account is not linked to an employee record."` — a clean,
expected response, not a server error.

## Dashboard (2026-08-07)

`GET /api/employees/me/dashboard` (self-service) / `GET /api/employees/{id}/dashboard`
(admin, permission `EMPLOYEE_DASHBOARD_VIEW`) — a single composite call for the Employee
Dashboard landing page: leave status, class routine, and upcoming holidays/events.

```jsonc
{
  "employeeId": "…",
  "employeeName": "Jane Doe",
  "leaveSummary": [ { "leaveTypeId": "…", "leaveTypeName": "Annual Leave", "used": 4, "allocated": 18, "balance": 14 } ],
  "pendingLeaveRequests": [ /* same LeaveRequestDto shape as GET .../leaverequests?isPending=true */ ],
  "classRoutine": [ /* TeacherAssignmentDto[], same shape as GET .../assignments, ordered by period start time */ ],
  "nextClass": { /* one TeacherAssignmentDto, or null */ },
  "upcomingEvents": [
    { "type": "Birthday", "label": "Birthday", "date": "2026-09-01" },
    { "type": "Holiday", "label": "Constitution Day", "date": "2026-09-20" },
    { "type": "Event", "label": "Sports Week", "date": "2026-08-25" },
    { "type": "Festival", "label": "Dashain", "date": "2026-10-10" }
  ]
}
```

- **`leaveSummary`/`pendingLeaveRequests`** are the exact same figures `GET .../me/profile`
  already returns — reused, not recomputed differently.
- **`classRoutine`/`nextClass`**: this codebase has **no day-of-week timetable** (a
  `TeacherAssignment.TimePeriodId` names one recurring period, not "Period 4 on Mondays" — see
  `Docs/time_period_and_class_routine_implementation_guide.md`), so `classRoutine` is the
  employee's whole assigned routine (every period they teach, on every school day), not "today's
  schedule" in the literal sense. `nextClass` is a best-effort pick: the first routine entry whose
  period hasn't started yet today (Nepal time-of-day) — `null` once every timed period for today
  has already started, or if the employee holds no assignment with a configured `TimePeriod`. A
  non-teaching employee (accountant, driver, ...) will simply see an empty `classRoutine` and a
  `null` `nextClass` — this is expected, not an error.
- **`upcomingEvents`** merges four sources into one date-sorted, capped-at-10 list covering the
  next 30 days: live-computed Birthday/WorkAnniversary (same as the Profile page), `PublicHoliday`/
  `InternalEvent` rows from the shared Dual Calendar module (`GET /api/calendar/events`,
  Province/Branch-scoped exactly like the calendar itself — an event scoped to a different
  province/branch than the employee's own is filtered out), and `FestivalOccurrence` rows (BS
  festivals like Dashain/Tihar; a multi-day festival's `date` is its start date only). `Exam` and
  `Note` calendar events are deliberately excluded — not "holidays and events" in the sense this
  widget targets.

## Calendar & Holidays — no new routes, just opened up

`GET /api/calendar/month-view`, `GET /api/calendar/events`, and `GET /api/calendar/festivals` are
now callable by any authenticated user (added to `DefaultEnabledMenu`) — these were never
Employee-scoped to begin with (the calendar is school-wide, not per-person), so there's no "Me"
wrapper for them, just the same "every page needs this read" carve-out already used for
`AcademicYears`/`Configs`/etc. `Create`/`Update`/`Delete` on events and festivals are still
permission-gated (admin-only), unchanged.

## How access control works here (read this before wiring up the sidebar)

**`DefaultEnabledMenu` controls who can *call* an endpoint — the menu-grant system controls
whether a role's *sidebar* shows a link to it. These are two separate things.**

Every "Me" action in the table above (18 in total as of 2026-08-07) — `GetMyDashboard`,
`AddMyQualification`/`GetMyQualifications`, `UploadMyDocument`/`GetMyDocuments`/`DownloadMyDocument`
included — plus the 3 Calendar read actions are listed in `WebApi/appsettings.json`'s
`DefaultEnabledMenu` section, under `"Employees"` and `"Calendar"` respectively. This means **the
API call succeeds for any authenticated Employee-linked user, regardless of which role they have
or what's been granted to it** — there is no `EMPLOYEE_*` permission row gating any `me/...`
route, on purpose (self-service to your own data isn't a privilege a role should have to be
handed). The admin `{id}`-scoped mirror routes are the exception, as always —
`GET /api/employees/{id}/dashboard` needs `EMPLOYEE_DASHBOARD_VIEW`, same as `{id}/profile` needs
`EMPLOYEE_PROFILE_VIEW`, and the document/qualification **verify/reject** actions need their own
`EMPLOYEE_DOCUMENT_VERIFY`/`REJECT`/`EMPLOYEE_QUALIFICATION_VERIFY`/`REJECT` grant (there is no
"Me" wrapper for those — deciding on someone else's submission was never going to be a self-service
action).

Separately, five `SUB_MENU` rows exist under the `MY_WORKSPACE` main menu (My Dashboard, My
Profile, Leave & Balance, Payslip & Taxes, My Classes — My Dashboard added 2026-08-07 as order 1,
the workspace's default/first tab) purely so the **menu tree** (`GET /api/roles/user-menus`, what
a frontend typically renders its sidebar from) has something to return. **These follow the normal
grant rules** — SuperAdmin gets them automatically like every menu row; every other role (Teacher,
Accountant, HR, ...) needs `MY_WORKSPACE`'s sub-menus granted once via the existing
`POST /api/roles/claims` screen, exactly like any other menu, before their sidebar will show the
links. **The underlying API access already works without that grant** — the grant only affects
navigation visibility, not authorization. If your frontend doesn't derive its sidebar purely from
the granted-menu tree (e.g. it always renders a fixed "My Workspace" section for any authenticated
user), you don't need the grant at all.

## Migration

The Dashboard/leave/profile/payslip/assignment "Me" routes need none — they reuse existing
tables/columns (`Employee.UserId`, `CalendarEvent`, `FestivalOccurrence` all already existed).
**The document/qualification "Me" routes ride on the same `VerificationStatus` columns the
verify/reject feature needs** — see `employee_documents_and_qualifications_implementation_guide.md`'s
migration addendum (four new columns on both `dbo.employee_documents` and
`dbo.employee_qualifications`); every document/qualification call 500s until that's applied,
`me/...` and `{id}/...` routes alike. See `Application/Employees/EmployeeService.cs`'s
`ResolveCurrentEmployeeIdAsync`, the `GetMy*`/`CreateMy*`/`CancelMy*`/`AddMy*`/`UploadMy*` methods,
and `GetEmployeeDashboardAsync`/`GetMyDashboardAsync` (2026-08-07) for the implementation.
