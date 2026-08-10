# CMSApp — Teacher entity removed, folded into Employee (UI)

**2026-08-06.** The standalone `Teacher` entity, `TeachersController`, and every `/api/teachers/*`
route are **gone**. A teacher is now nothing more than an `Employee` categorized by
`EmployeeCategoryCode`/`JobPositionCode` — no separate profile, no separate CRUD, no separate
permission tree. This closes out a long-standing complaint: the Role → Claims permission screen
carried 26 near-duplicate `TEACHER_*` rows on top of the equivalent `EMPLOYEE_*` rows, most of
which were pure aliases forwarding to the exact same `EmployeeService` methods.

## Why this changed

`TeachersController` had 27 actions. ~13 of them (salary, tax, payslip, loans) were 1-line
forwards into `IEmployeeService` — kept only for backward compatibility with the original
`Teacher.Id == Employee.Id` shared-PK design. The rest (Create/Update/Delete/List/Detail) fully
duplicated Employee CRUD. `MenuSeeder` gave every one of those 27 actions its own permission row,
so assigning a role meant scrolling past nearly-identical "Teacher" and "Employee" toggles for the
same underlying capability. Removing the duplication was a direct, explicit ask — the user wants
"everything categorized under Employee profile by Job Category or Position," not a parallel
Teacher concept.

## What actually moved where

| Old (`/api/teachers/{id}/...`) | New | Notes |
|---|---|---|
| `POST /api/teachers` (create) | `POST /api/employees` | Set `EmployeeCategoryCode`/`JobPositionCode` and, optionally, the three teaching fields below. No more hardcoded `ACADEMIC` category. |
| `GET /api/teachers`, `GET /api/teachers/{id}`, `PUT /api/teachers/{id}`, `DELETE /api/teachers/{id}` | `GET /api/employees`, `GET /api/employees/{id}`, `PUT /api/employees/{id}`, `DELETE /api/employees/{id}` | Employee's own CRUD already covered the exact same operation. |
| `.../assignments`, `.../assignments/bulk`, `.../assignments/bulk-entry`, `DELETE .../assignments/{assignmentId}`, `GET .../assignments` | `POST/DELETE/GET /api/employees/{id}/assignments...` (same route tails) | Unchanged request/response shapes — see below. |
| `.../salaries*`, `.../salaries/tax-calculation*`, `.../salaries/tax-planning`, `.../salaries/annual-forecast`, `.../salaries/tax-details`, `.../payslips*`, `.../salary-forecast`, `.../loans*` | `/api/employees/{id}/...` (same route tails) | These were pure aliases — the real implementation always lived on `IEmployeeService`. Nothing else changes. |
| `GET .../id-card-preview` | `GET /api/employees/{id}/id-card-preview` | Moved to `EmployeeService.GetIdCardPreviewAsync`, generalized to read `TeachingLicenseNo`/`Specialization` off the `Employee` row directly. Reachable for any employee, not just teaching staff — the printed card just prints blank for those two fields if unset. |
| `POST /api/employees/{id}/teacher-profile` (promote to teacher) | *(removed, no replacement)* | Set `TeachingLicenseNo`/`ExperienceYears`/`Specialization` via the normal `POST /api/employees` or `PUT /api/employees/{id}` call instead — no separate "add teacher profile" step, no eligibility gate. |

**Nothing changed** on these routes — they were never on `TeachersController`:
`POST /api/academicclasses/{id}/teacher-assignments/bulk-entry`,
`GET /api/academicclasses/{id}/teacher-assignments`, `GET /api/dashboard/teachers`.

## `EmployeeDto` changes

- **Removed**: `hasTeacherProfile`.
- **Added**: `teachingLicenseNo` (string), `experienceYears` (int), `specialization` (string) — all
  optional, settable on any employee regardless of category/position (no more "must be Academic +
  Teacher/Principal/Vice Principal" gate).
- **Added**: `isTeachingStaff` (bool) — a read-only derived flag (`EmployeeCategoryCode ==
  "ACADEMIC"` and `JobPositionCode` in `TEACHER`/`PRINCIPAL`/`VICE_PRINCIPAL`), for UI badging.
  Replaces the old `hasTeacherProfile`'s purpose without implying a separate profile record exists.
- **Added** (detail endpoint only, empty on the paged list): `serviceHistory` — same shape as the
  old `TeacherDto.serviceHistory` (assignments with their academic years, oldest first).

`CreateEmployeeCommand`/`UpdateEmployeeCommand` both gained `teachingLicenseNo`/`experienceYears`/
`specialization` (optional, same free-form treatment as every other optional field).

## Assignments — `/api/employees/{id}/assignments...`

Unchanged behavior and shapes from the old `/api/teachers/{id}/assignments...` routes:

```
POST   /api/employees/{id}/assignments               single assign
POST   /api/employees/{id}/assignments/bulk           same subject, several sections
POST   /api/employees/{id}/assignments/bulk-entry     whole routine, one row per class/subject/section/period
DELETE /api/employees/{id}/assignments/{assignmentId}
GET    /api/employees/{id}/assignments
```

Request/response DTOs (`AssignTeacherCommand`, `AssignTeacherBulkCommand`,
`AssignTeacherBulkEntryCommand`, `TeacherAssignmentDto`, `TeacherAssignmentBulkResultDto`,
`TeacherAssignmentBulkEntryResultDto`) are **byte-for-byte unchanged** — only their home moved
from `Application/Teachers/` to `Application/Employees/`. The underlying `TeacherAssignment`
entity/table also kept its name and its `TeacherId` column — only the column's target changed,
from the now-gone `Teacher.Id` to `Employee.Id` directly (the two were always numerically equal
under the old shared-PK design, so this is a pure FK retarget, not a data rewrite).

## ID card preview — `GET /api/employees/{id}/id-card-preview`

Same response shape as before (`DocumentPreviewDto` — `{ templateType, html }`), same backing
`DocumentTemplateType.TeacherIdCard` template (name kept as-is — a deliberate minimal-churn call,
not a design statement that only teachers get ID cards). Placeholder values now read straight off
the `Employee` row (`TeachingLicenseNo`/`Specialization` come back empty for non-teaching staff).

## Migration (schema change — user-owned, not applied here)

This round needs a real EF Core migration. Exact steps:

1. Add columns to `dbo.employees`: `teaching_license_no varchar(100) NULL`,
   `experience_years integer NULL`, `specialization varchar(255) NULL`.
2. Data-copy existing values (raw SQL inside the migration's `Up()`,
   `migrationBuilder.Sql(...)`):
   ```sql
   UPDATE dbo.employees e
   SET teaching_license_no = t.teaching_license_no,
       experience_years = t.experience_years,
       specialization = t.specialization
   FROM dbo.teachers t
   WHERE t.id = e.id;
   ```
3. Drop the FK `teacher_assignments.teacher_id -> teachers.id`; add a new FK
   `teacher_assignments.teacher_id -> employees.id`. No data rewrite needed for
   `teacher_assignments` itself — every existing row's `teacher_id` value already equals the
   correct `employee_id`, by construction of the old shared-PK design.
4. Drop table `dbo.teachers`.
5. Run `dotnet ef migrations add RemoveTeacherEntity` — this should pick up exactly the above
   once the code changes are in place, and will regenerate
   `ApplicationDbContextModelSnapshot.cs`.

**Until this migration is applied**: every `Employee` create/update that touches the three new
columns, and every read of `EmployeeDto.teachingLicenseNo`/`experienceYears`/`specialization`,
500s (EF selects/inserts the mapped columns). `TeacherAssignment` reads/writes are unaffected
until the FK is actually retargeted (the column itself didn't move).

## Permission catalog changes

Retired (26 `TEACHER_*` rows + `EMPLOYEE_TEACHER_PROMOTE`, no replacement needed — an equivalent
`EMPLOYEE_*` permission already existed for every capability except assignments/ID-card):
`TEACHER_LIST`, `TEACHER_CREATE`, `TEACHER_DETAIL`, `TEACHER_UPDATE`, `TEACHER_DELETE`,
`TEACHER_ASSIGNMENT_ADD`, `TEACHER_ASSIGNMENT_BULK_ADD`, `TEACHER_ASSIGNMENT_BULK_ENTRY_ADD`,
`TEACHER_ASSIGNMENT_REMOVE`, `TEACHER_ASSIGNMENT_LIST`, `TEACHER_SALARY_ADD`,
`TEACHER_SALARY_LIST`, `TEACHER_SALARY_TAX_CALCULATION`, `TEACHER_PAYSLIP_PREVIEW`,
`TEACHER_ID_CARD_PREVIEW`, `TEACHER_SALARY_TAX_CALCULATION_MONTHLY`, `TEACHER_PAYSLIP_LIST`,
`TEACHER_PAYSLIP_DETAIL`, `TEACHER_LOAN_REQUEST`, `TEACHER_LOAN_LIST`, `TEACHER_LOAN_APPROVE`,
`TEACHER_LOAN_REJECT`, `TEACHER_LOAN_CANCEL`, `TEACHER_SALARY_FORECAST`, `TEACHER_TAX_PLANNING`,
`TEACHER_SALARY_ANNUAL_FORECAST`, `TEACHER_TAX_DETAILS_GRID`, `EMPLOYEE_TEACHER_PROMOTE`.

New, under `EMPLOYEE_LIST`: `EMPLOYEE_ASSIGNMENT_ADD`, `EMPLOYEE_ASSIGNMENT_BULK_ADD`,
`EMPLOYEE_ASSIGNMENT_BULK_ENTRY_ADD`, `EMPLOYEE_ASSIGNMENT_REMOVE`, `EMPLOYEE_ASSIGNMENT_LIST`,
`EMPLOYEE_ID_CARD_PREVIEW`.

**Any role that had a `TEACHER_*` grant loses it on next boot** and needs the corresponding
`EMPLOYEE_*` grant added instead (most already had the `EMPLOYEE_*` equivalent, since it always
existed alongside the alias). `DASHBOARD_TEACHER_WIDGET` (Dashboard's teacher-count widget) and
`CLASS_TEACHER_ASSIGNMENT_BULK_ENTRY_ADD`/`CLASS_TEACHER_ASSIGNMENT_LIST` (the class-scoped
sibling endpoints on `AcademicClassesController`) are **unaffected** — neither was ever on
`TeachersController`.

## Unaffected / deliberately unchanged

- `GET /api/academicclasses/{id}/teacher-assignments*` (the class-scoped "who teaches this class"
  endpoints) — same routes, same DTOs, just repointed internally from `Teacher`/`ITeacherRepository`
  to `Employee`/`IEmployeeRepository`.
- `GET /api/dashboard/teachers` (the dashboard teacher-list widget) — same route/response shape,
  now queries `Employee` rows filtered by the same "teaching staff" predicate
  (`EmployeeCategoryCode == "ACADEMIC"` and `JobPositionCode` in
  `TEACHER`/`PRINCIPAL`/`VICE_PRINCIPAL`) instead of a `Teacher` table.
- `GET /api/exams?...&teacherId=` — the `teacherId` query parameter name is kept (still an
  accurate description of "the person assigned to teach this"), just resolved against
  `Employee.Id` now.
