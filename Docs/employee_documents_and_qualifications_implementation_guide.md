# CMSApp — Employee Qualifications, Employee & Student Documents, Accounts and Codes (UI)

> **2026-08-07: self-service upload + HR verification.** An employee can now upload their own
> document / add their own qualification via new `me/...` routes, and every record (self-service
> or admin-entered) carries a `verificationStatus`. See the dedicated section near the bottom of
> this guide — everything above it describes the admin `{id}`-scoped routes, which are otherwise
> unchanged.

> **2026-08-06: the standalone `Teacher` entity/`TeachersController` were removed entirely** (this
> guide already routed qualifications/documents through `/api/employees/...`, so none of its
> routes changed) — see `employee_teaching_profile_and_assignments_implementation_guide.md` for
> what did change elsewhere.

**2026-07-23 consolidation.** This supersedes `teacher_documents_implementation_guide.md` (deleted)
for the teacher-facing half of that guide: qualifications and documents are no longer scoped to
`Teacher` at all — they belong to `Employee` generically, since neither concept is actually
teaching-specific (an accountant holds a degree too; every employee needs identity/verification
documents on file the same way a teacher does). Student documents are unaffected and still live at
`/api/students/{id}/documents` (catalog `1007`) — included below for completeness since the shape
is identical.

## Why this changed

Before this round, `TeacherQualification`/`TeacherDocument` FK'd to `Teacher.Id` and were only
reachable via `/api/teachers/{id}/qualifications` / `/api/teachers/{id}/documents`. That meant a
non-teaching employee (an accountant, a driver, an office assistant) had nowhere to record a
qualification or upload a citizenship document — the feature existed, it just wasn't reachable for
~90% of the staff roster. Since `Teacher` already only exists for staff in an Academic-category
Teacher/Principal/Vice-Principal position (see `employee_management_implementation_guide.md`), and
neither a qualification record nor an uploaded document has anything to do with *teaching*
specifically, both were moved onto `Employee` wholesale rather than duplicated on both entities.
**No Teacher-side alias was kept** (unlike some other Employee/Teacher consolidations in this
codebase) — every consumer, teacher or not, now goes through `/api/employees/{id}/...`.

## Qualifications — `/api/employees/{id}/qualifications`

```
POST   /api/employees/{id}/qualifications                       add
DELETE /api/employees/{id}/qualifications/{qualificationId}     remove
GET    /api/employees/{id}/qualifications                       list
```

Body (`AddEmployeeQualificationCommand`):

```json
{
  "qualificationCode": "MASTERS",
  "courseName": "M.Sc. Mathematics",
  "institution": "Tribhuvan University",
  "completionYear": 2018,
  "score": "3.7 GPA",
  "remarks": null
}
```

- `qualificationCode` — required, catalog `1005` (`ConfigTypeCodes.EmployeeQualification`, renamed
  2026-07-23 from `TeacherQualification` — **same TypeCode, 1005, only the name changed**, so no
  re-seeding is needed for the option rows themselves). Seeded: `PHD`, `MASTERS`, `BACHELORS`,
  `DIPLOMA`, `CERTIFICATE`, `OTHER`. Populate the dropdown from `GET /api/configs/dropdown/1005`.
- `completionYear` — optional, `1950`–`2100` when given.
- Failure: `400 VALIDATION_ERROR` (unknown code, out-of-range year), `404` unknown employee.

Response `data` (add/list) — `EmployeeQualificationDto`:

```json
{
  "id": "…", "employeeId": "…",
  "qualificationCode": "MASTERS", "courseName": "M.Sc. Mathematics",
  "institution": "Tribhuvan University", "completionYear": 2018,
  "score": "3.7 GPA", "remarks": null,
  "verificationStatus": 2,
  "verificationRemarks": null,
  "verifiedTs": "2026-07-23T09:15:00+00:00",
  "verifiedBy": "hr.staff"
}
```

`verificationStatus` (`1` Pending / `2` Approved / `3` Rejected, added 2026-08-07) is `Approved`
here because this row came through the admin route — see the Self-service section below for the
`Pending` case and the verify/reject endpoints.

No update endpoint (add + remove only, same convention as every other line-item child record in
this codebase — e.g. salary components/deductions).

## Documents — `/api/employees/{id}/documents`

```
POST   /api/employees/{id}/documents                          upload (multipart/form-data)
GET    /api/employees/{id}/documents                          list
GET    /api/employees/{id}/documents/{documentId}/download    download raw file
DELETE /api/employees/{id}/documents/{documentId}              delete
```

### Upload — `POST /api/employees/{id}/documents` (**multipart/form-data**, not JSON)

| Form field | Required | Notes |
|---|---|---|
| `file` | ✅ | PDF/JPG/JPEG/PNG only, max **10 MB** |
| `documentTypeCode` | ✅ | Catalog `1006` code |
| `documentName` | ✅ | Display name, ≤150 chars (e.g. "Driving License — B category") |
| `validUntil` | ❌ | Expiry date (`yyyy-MM-dd`) for license/report-type documents |
| `remarks` | ❌ | ≤500 chars |

Failures: `400 VALIDATION_ERROR` (no file / bad extension / >10 MB / unknown type code / missing
name), `404` unknown employee.

Response `data` — `EmployeeDocumentDto`:

```json
{
  "id": "…", "employeeId": "…",
  "documentTypeCode": "DRIVING_LICENSE", "documentName": "Driving License — B category",
  "fileName": "license-scan.pdf", "contentType": "application/pdf", "fileSizeBytes": 482133,
  "validUntil": "2028-03-01", "remarks": null,
  "uploadedTs": "2026-07-23T09:15:00+00:00",
  "verificationStatus": 2,
  "verificationRemarks": null,
  "verifiedTs": "2026-07-23T09:15:00+00:00",
  "verifiedBy": "hr.staff"
}
```

`verificationStatus` (`1` Pending / `2` Approved / `3` Rejected, added 2026-08-07) is `Approved`
here because this row came through the admin route — see the Self-service section below.

### List — `GET /api/employees/{id}/documents`

Returns `EmployeeDocumentDto[]` (no file bytes). UI tip: flag rows where `validUntil` is past
(expired) or within ~30 days (expiring soon).

### Download — `GET /api/employees/{id}/documents/{documentId}/download`

Streams the **raw file** (correct `Content-Type`, original filename in `Content-Disposition`) —
the one endpoint that does *not* return the JSON envelope on success; errors (`404`) still do.
Open in a new tab or fetch as blob. The token still applies (permission
`EMPLOYEE_DOCUMENT_DOWNLOAD`), so a plain `<a href>` needs the Authorization header via
fetch/blob.

### Delete — `DELETE /api/employees/{id}/documents/{documentId}`

Hard delete: removes the row **and** the stored file.

### Document type catalog

`ConfigTypeCodes.DocumentType = 1006` (unchanged code and name — it was already generic, only its
seeded description text was reworded from "(teacher documents)" to "(employee documents)").
Seeded: `CITIZENSHIP`, `ID_CARD`, `PAN_CARD`, `PASSPORT`, `DRIVING_LICENSE`, `POLICE_REPORT`,
`ACADEMIC_CERTIFICATE`, `APPOINTMENT_LETTER`, `OTHER`. Populate from
`GET /api/configs/dropdown/1006`; admins add more via `POST /api/configs`. Suggested UI hint: show
the "Valid until" field prominently for expiring types (the catalog's `additionalValue1` can be
set to `"Y"` on those).

## Student documents (unaffected, included for completeness)

`/api/students/{id}/documents` — the **identical four endpoints and shapes**, substituting
`StudentDocumentDto` and catalog `1007` (`BIRTH_CERTIFICATE`, `TRANSFER_CERTIFICATE`,
`CHARACTER_CERTIFICATE`, `PREVIOUS_MARKSHEET`, `CITIZENSHIP`, `PASSPORT`, `PHOTO`,
`IMMUNIZATION_RECORD`, `DISABILITY_CARD`, `MIGRATION_CERTIFICATE`, `GUARDIAN_CITIZENSHIP`,
`OTHER`). This entity/table/endpoint was never touched by the 2026-07-23 consolidation — students
were always their own thing, separate from the Teacher/Employee split.

## "Accounts and Codes" — new Employee fields (2026-07-23)

Requested alongside this consolidation: five new optional fields on `Employee`, exposed on
`EmployeeDto` and settable via `POST`/`PUT /api/employees`:

| Field | Example |
|---|---|
| `panNumber` | `"119175732"` |
| `providentFundNumber` | |
| `ssfNumber` | |
| `citNumber` | |
| `gratuityNumber` | |

All free-form strings, ≤50 characters, no format validated (PAN/PF/SSF/CIT/Gratuity numbering
schemes aren't standardized enough across employers to enforce a shape — same reasoning as why
`countryIso3` on the user-registration side is "shape-only, not a real whitelist"). All optional —
not every employee is enrolled in every scheme (Gratuity in particular typically only vests after
a service-length threshold). Distinct from the pre-existing `bankName`/`bankAccountNumber` fields,
which are payment-routing details, not statutory scheme identifiers.

## Permissions (seeded to SuperAdmin)

`EMPLOYEE_QUALIFICATION_ADD/REMOVE/LIST`, `EMPLOYEE_DOCUMENT_UPLOAD/LIST/DOWNLOAD/DELETE`, and
(added 2026-08-07) `EMPLOYEE_QUALIFICATION_VERIFY/REJECT`, `EMPLOYEE_DOCUMENT_VERIFY/REJECT` (all
under `EMPLOYEE_LIST`) — grant to other roles via `POST /api/roles/claims`. The verify/reject
permissions are what you grant to whichever role your school treats as "HR" — there is no
hardcoded HR role in this codebase, same as the Accounts/HR dashboard summaries. The old
`TEACHER_QUALIFICATION_*`/`TEACHER_DOCUMENT_*` rows are **retired** (soft-deleted by
`MenuSeeder.BuildRetiredMenuCodes` on next boot) — any role that previously held those grants
loses them and needs the new `EMPLOYEE_*` rows granted instead. `STUDENT_DOCUMENT_*` (under
`STUDENT_MANAGEMENT`) is unaffected. The new self-service `me/...` routes below need **no**
permission grant at all (`DefaultEnabledMenu`, same as every other "Me" route).

## Self-service upload + HR verification (2026-08-07)

Mirrors the existing 12-route "Me" self-service pattern (`employee_self_service_implementation_guide.md`)
— any login whose `ApplicationUser` is linked to an `Employee` row can upload their own document
or add their own qualification, and HR (or whichever role holds the new verify/reject permissions)
decides on it.

### New "Me" routes (no `{id}`, no permission grant needed — `DefaultEnabledMenu`)

```
POST   /api/employees/me/qualifications                      add (same body as the admin route)
GET    /api/employees/me/qualifications                      list mine
POST   /api/employees/me/documents                            upload (multipart, same fields)
GET    /api/employees/me/documents                            list mine
GET    /api/employees/me/documents/{documentId}/download      download my own file
```

Request/response shapes are byte-for-byte identical to the admin `{id}`-scoped routes above — the
only functional difference is **the record starts `verificationStatus: 1` (Pending)** instead of
`2` (Approved). The success message also says so:
`"Document uploaded successfully. Pending HR verification."` /
`"Qualification added successfully. Pending HR verification."` An unlinked account (Student, or an
Admin with no HR record) gets a clean `404` `"Your account is not linked to an employee record."`,
same as every other "Me" route. **No self-service delete** — if a submission needs correcting
before HR reviews it, an admin removes it via the existing `DELETE .../documents/{documentId}` /
`.../qualifications/{qualificationId}` route and the employee re-submits.

### Why admin-route uploads are auto-Approved

A record entered through the existing `{id}`-scoped admin route (`POST /api/employees/{id}/documents`,
`POST /api/employees/{id}/qualifications` — used by HR/admin doing data entry on an employee's
behalf, e.g. onboarding) is stamped `verificationStatus: 2` (Approved) immediately, with
`verifiedBy`/`verifiedTs` set to the entering user — an already-authorized staff action doesn't
need a second review step. Only the two new self-service routes start `Pending`.

### Verify / Reject — `POST /api/employees/{id}/documents/{documentId}/verify|reject`, `POST /api/employees/{id}/qualifications/{qualificationId}/verify|reject`

Permission-gated (`EMPLOYEE_DOCUMENT_VERIFY`/`REJECT`, `EMPLOYEE_QUALIFICATION_VERIFY`/`REJECT`).
Body (`DocumentVerificationCommand`/`QualificationVerificationCommand`, both identical shape):

```json
{ "remarks": "Citizenship number doesn't match the record on file." }
```

`remarks` is optional either way (a plain approve typically leaves it blank). **One-shot**: only a
`Pending` record can be decided — deciding an already-decided one returns
`400 CONFLICT` `"This document has already been approved."` (or `rejected`), same guard
`LeaveRequest`'s manager/HR decisions use. Response `data` is the updated `EmployeeDocumentDto`/
`EmployeeQualificationDto` (`verificationStatus` now `2` or `3`, `verificationRemarks`/`verifiedTs`/
`verifiedBy` populated). The employee gets a `Notification` row either way (`NotificationType.DocumentVerified`/
`DocumentRejected`/`QualificationVerified`/`QualificationRejected`) — same
`GET /api/employees/{id}/notifications` feed the Leave workflow already raises into.

### `verificationStatus` values

`1` Pending, `2` Approved, `3` Rejected (`Domain.Enums.VerificationStatus`) — every `EmployeeDocumentDto`/
`EmployeeQualificationDto` from any route (admin or self-service) now carries this plus
`verificationRemarks`/`verifiedTs`/`verifiedBy`.

## Backend notes

- Storage subfolder renamed `teacher-documents/{teacherId}/` → `employee-documents/{employeeId}/`
  under `FileStorage:RootPath` (default `Uploads/` beside the API). Existing files physically on
  disk under the old `teacher-documents/` path are **not moved automatically** — either move them
  by hand to match the new `employee_documents.file_path` values after the data migration below,
  or accept that pre-existing uploads' download links will 404 until re-uploaded. New uploads
  always land under `employee-documents/`.
- `Application/Employees/EmployeeService.cs` now owns the upload/list/download/delete and
  add/remove/list logic (ported verbatim from the old `TeacherService`, just renamed) and takes a
  new `IFileStorageService` constructor dependency it didn't need before.
- `IEmployeeRepository`/`EmployeeRepository` gained the four document and four qualification
  methods (`Get*/GetById*/Add*/Remove*`), moved from `ITeacherRepository`/`TeacherRepository`.
  `TeacherRepository.GetPagedByFilterAsync`'s `QualificationCode` filter (used by the Teacher list
  page) still works — it now joins through `teacher.Employee.Qualifications` instead of
  `teacher.Qualifications`.
- `ConfigTypeCodes.TeacherQualification` (1005) renamed to `ConfigTypeCodes.EmployeeQualification`
  — same numeric TypeCode, so no data migration is needed for the catalog rows themselves, only
  for the source-code references (all updated).

## Migration required (not created here — user-owned, as always)

- **Rename** `dbo.teacher_documents` → `dbo.employee_documents`; rename its `teacher_id` column
  to `employee_id`; repoint its FK from `teachers.id` to `employees.id`.
- **Rename** `dbo.teacher_qualifications` → `dbo.employee_qualifications`; same column rename
  (`teacher_id` → `employee_id`) and FK repoint.
- **Add** five nullable `varchar(50)` columns to `dbo.employees`: `pan_number`,
  `provident_fund_number`, `ssf_number`, `cit_number`, `gratuity_number`.

Since `Teacher.Id` already equals its owning `Employee.Id` (the shared-PK design from the
2026-07-15 Employee/Teacher split), **every existing row's FK value is already correct** —
`teacher_id` on an existing `teacher_documents`/`teacher_qualifications` row already *is* the
right `employee_id` value, it just needs the column and constraint renamed, not its data rewritten.
This is a schema-only migration, not a data migration, which is the direct benefit of qualifications
and documents never having been given their own independent identity column in the first place.

## Migration required, addendum (2026-08-07, self-service + verification)

Four new columns on **both** `dbo.employee_documents` and `dbo.employee_qualifications`:

- `verification_status integer NOT NULL` (`1`/`2`/`3` — no default; every existing row needs a
  value backfilled, see below)
- `verification_remarks varchar(500) NULL`
- `verified_ts timestamptz NULL`
- `verified_by varchar(256) NULL`

**Data backfill for existing rows**: every row that already exists today was entered through the
(only, at the time) admin route, so backfill `verification_status = 2` (Approved) for all of them
— e.g. `UPDATE dbo.employee_documents SET verification_status = 2 WHERE verification_status IS
NULL;` (and the same for `employee_qualifications`) as part of the same migration, before adding
the `NOT NULL` constraint. Until this migration is applied, every document/qualification
create/read/verify/reject call 500s (EF selects the mapped columns).
