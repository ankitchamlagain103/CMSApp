# CMSApp — Employee Qualifications, Employee & Student Documents, Accounts and Codes (UI)

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
  "score": "3.7 GPA", "remarks": null
}
```

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
  "uploadedTs": "2026-07-23T09:15:00+00:00"
}
```

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

`EMPLOYEE_QUALIFICATION_ADD/REMOVE/LIST` and `EMPLOYEE_DOCUMENT_UPLOAD/LIST/DOWNLOAD/DELETE`
(both under `EMPLOYEE_LIST`) — grant to other roles via `POST /api/roles/claims`. The old
`TEACHER_QUALIFICATION_*`/`TEACHER_DOCUMENT_*` rows are **retired** (soft-deleted by
`MenuSeeder.BuildRetiredMenuCodes` on next boot) — any role that previously held those grants
loses them and needs the new `EMPLOYEE_*` rows granted instead. `STUDENT_DOCUMENT_*` (under
`STUDENT_MANAGEMENT`) is unaffected.

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
