# Student Timetable & Profile Optimization

2026-08-05. Three related changes to the Student feature, all aimed at the same problem: the
student profile page (`GET /api/students/{id}`) was returning far more than any single screen
ever needed, and codes that need a display label (grade, section, subject, guardian relationship)
were left for the UI to resolve itself against separate dropdown endpoints. This guide covers:

1. A new student-facing **timetable** endpoint — "who teaches my classes, and when."
2. A **slimmed profile response** — `GET /api/students/{id}` now returns only what the profile
   header always shows; everything tab-specific moved to its own endpoint.
3. **Server-resolved labels** on Config-backed codes, so the UI stops making a dropdown call just
   to turn `"FATHER"` into `"Father"`.

It also documents the two underlying conventions (tab-scoped loading, backend label resolution) so
they can be applied consistently to other profile-style screens going forward — this round fixes
the Student feature concretely; it does not touch every other feature in the codebase (see
"Scope of this round" at the end).

## 1. New: `GET /api/students/{id}/timetable`

Answers "what does this student's class routine look like, and who teaches each subject" — the
thing that was previously impossible to get in one call (the old `currentEnrollment.subjects[]`
had a `teacherName` field, but nothing about *when* — no period, no time).

```
GET /api/students/{id}/timetable
```

Permission: `STUDENT_TIMETABLE` (seeded under `STUDENT_LIST`, action `GetTimetable`).

**Response** (`StudentTimetableDto`):

```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "enrollmentId": "019f5c83-...",
    "academicYearId": "019f57ab-...",
    "academicYearCode": "AY2083",
    "academicYearName": "Academic Year 2083/84",
    "academicClassId": "019f59c5-...",
    "gradeCode": "LKG",
    "gradeLabel": "LKG",
    "classSectionId": "019f59cb-...",
    "sectionCode": "A",
    "sectionLabel": "Section A",
    "classTeacherName": "Ramesh Adhikari",
    "entries": [
      {
        "classSubjectId": "019f59cb-...",
        "subjectCode": "ENGLISH",
        "subjectLabel": "English",
        "isMandatory": true,
        "teacherId": "019f659b-...",
        "teacherName": "Ramesh Adhikari",
        "employeeCode": "EMP2026001",
        "timePeriodId": "019fc6ef-...",
        "timePeriodName": "Period 1",
        "timePeriodStartTime": "08:00:00",
        "timePeriodEndTime": "08:45:00"
      },
      {
        "classSubjectId": "019f5c78-...",
        "subjectCode": "SCIENCE",
        "subjectLabel": "Science",
        "isMandatory": true,
        "teacherId": null,
        "teacherName": null,
        "employeeCode": null,
        "timePeriodId": null,
        "timePeriodName": null,
        "timePeriodStartTime": null,
        "timePeriodEndTime": null
      }
    ]
  }
}
```

- `entries[]` covers every subject the student actually studies (every mandatory subject of their
  section, plus whichever electives this enrollment picked) — the same "studying subjects"
  resolution the old `currentEnrollment.subjects[]` used, factored into a shared
  `ResolveStudyingSubjectsAsync` helper.
- A subject with no teacher/period assigned yet still appears, with those fields `null` — this is
  a normal state (see "Known gap" below), not an error.
- `entries[]` is ordered by `timePeriodStartTime` (subjects with a period land first, earliest
  first); subjects with no period yet sort last, by `subjectCode`.
- `classTeacherName` is the section's homeroom teacher (any assignment in the section with
  `isClassTeacher: true`), independent of which subject that assignment happens to ride on — it's
  resolved once from the same underlying assignment list, not per-entry.
- 404 (`NotFound`) if the student doesn't exist, or if they have no active enrollment
  ("This student has no active enrollment to build a timetable from.").

**Co-teaching simplification**: if two teachers are both assigned to the exact same subject in the
same section, `teacherName` lists both (comma-joined, same convention the old
`StudentSubjectDto.TeacherName` used), but `teacherId`/`employeeCode`/`timePeriod*` describe only
the first assignment found. A genuinely rare case, not worth a nested per-teacher structure.

**Known gap**: this reuses `ITeacherRepository.GetAssignmentsByAcademicClassAsync`, which matches
on the assignment's **exact** `ClassSectionId` — per the 2026-08-04 validation round, every new
`TeacherAssignment` now always has one, so this is complete for current data. A `TeacherAssignment`
row created **before** 2026-08-04 (with a null `ClassSectionId`, meaning "every section") will
**not** show up here — only the section-scoped-assignment-required convention is read. This
codebase's dev database currently has no such legacy rows (cleared 2026-08-04), so it isn't visible
today, but note it if an older database is ever restored against this code.

## 2. Slimmed `GET /api/students/{id}`

**Before** (what prompted this round): one call returned the full guardian list, the full current
class + every studied subject (each requiring a teacher-name lookup), *and* the entire enrollment
history — all loaded on every single profile page open, even though the profile UI's tab strip
(Current Class / Guardians / History / Documents / Fee Summary / Statement) only shows a header
card up front and puts everything else behind tabs the user may never click.

| Tab | Data source |
|---|---|
| Current Class | *(no source before this round — now `GET .../timetable`)* |
| Guardians | `GET /api/students/{id}/guardians` (already existed, but was **also** duplicated into the main GET) |
| History | *(embedded in main GET — now `GET .../enrollment-history`)* |
| Documents | `GET /api/students/{id}/documents` (already its own call, unaffected) |
| Fee Summary / Statement | Fee module endpoints (unaffected — never went through `StudentsController`) |

**After**:

```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": {
    "id": "019f5c83-...",
    "userId": "019fb1d4-...",
    "admissionNo": "ADM2026192",
    "firstName": "Aarati",
    "middleName": null,
    "lastName": "Acharya",
    "gender": 1,
    "dateOfBirth": "2021-08-08T00:00:00",
    "email": "jchamlagain@gmail.com",
    "phone": "+9779862699637",
    "address": "Butwal-1, Nepal",
    "admissionDate": "2026-07-13T00:00:00",
    "status": 1,
    "createdBy": "system",
    "createdTs": "2026-07-13T17:25:53.145351+00:00",
    "updatedBy": "superadmin",
    "updatedTs": "2026-07-30T07:02:35.887993+00:00",
    "guardians": [],
    "currentEnrollment": {
      "enrollmentId": "019f5c83-...",
      "academicYearId": "019f57ab-...",
      "academicYearCode": "AY2083",
      "academicYearName": "Academic Year 2083/84",
      "academicClassId": "019f59c5-...",
      "gradeCode": "LKG",
      "gradeLabel": "LKG",
      "classSectionId": "019f59cb-...",
      "sectionCode": "A",
      "sectionLabel": "Section A",
      "rollNumber": "5",
      "enrollmentDate": "2026-07-13T00:00:00"
    }
  }
}
```

### What changed on `StudentDto`

| Field | Before | After |
|---|---|---|
| `guardians[]` | Full guardian list on the detail endpoint | **Always empty `[]` on `GET .../{id}`** — call `GET .../{id}/guardians` (unchanged endpoint, gained `relationshipLabel`, see §3). Create/Update responses still populate it (see below). |
| `currentEnrollment` | Full object **including `subjects[]`** (each subject's teacher resolved via a batched lookup) | Same object, **`subjects[]` removed**. Everything else (`enrollmentId`, year/grade/section ids and codes, `rollNumber`, `enrollmentDate`) stays, now also carrying `gradeLabel`/`sectionLabel`. Call `GET .../{id}/timetable` for the subject/teacher/period breakdown. |
| `enrollmentHistory[]` | Every enrollment ever, embedded | **Property removed entirely.** Call the new `GET .../{id}/enrollment-history`. |

**`guardians[]` stays populated on create/update responses** (`POST /api/students`,
`PUT /api/students/{id}`) — those are "what did I just submit" confirmations, where returning the
guardians saves an immediate re-fetch; only the plain `GET .../{id}` leaves it empty, same
convention the paged list already used ("populated on detail/create, empty on the paged list" —
now also empty on the *plain* detail endpoint, with a dedicated endpoint replacing that role).

### New: `GET /api/students/{id}/enrollment-history`

Same `StudentEnrollmentHistoryDto[]` shape as before (now with `gradeLabel`/`sectionLabel` added),
just its own call. Permission: `STUDENT_ENROLLMENT_HISTORY` (`GetEnrollmentHistory`).

### Internal fix: `GetIdCardPreviewAsync`

The ID card preview composes itself from `GetStudentByIdAsync`'s own DTO internally — since that
DTO no longer carries `Guardians`, the primary-guardian lookup for the card (name/phone) now
queries `IStudentRepository.GetGuardianLinksAsync` directly instead of reading the (now-empty)
`studentDto.Guardians`. This was the one internal call site that would have silently started
printing blank guardian fields on ID cards if left unfixed — caught and fixed as part of this
round, not a separate bug report.

## 3. Server-resolved Config labels

Every DTO in this round that carries a Config-catalog-backed code now also carries the resolved
label, via the existing `Application/Common/Helpers/ConfigLabelHelper` (already used by the
Fee/Payroll modules — this round is its first use in Student):

| DTO | Code field | New label field | Catalog |
|---|---|---|---|
| `StudentGuardianDto` | `relationshipCode` | `relationshipLabel` | `GuardianRelationship` (1004) |
| `StudentCurrentEnrollmentDto` | `gradeCode` / `sectionCode` | `gradeLabel` / `sectionLabel` | `Grade` (1001) / `Section` (1002) |
| `StudentEnrollmentHistoryDto` | `gradeCode` / `sectionCode` | `gradeLabel` / `sectionLabel` | `Grade` / `Section` |
| `StudentTimetableDto` | `gradeCode` / `sectionCode` | `gradeLabel` / `sectionLabel` | `Grade` / `Section` |
| `StudentTimetableEntryDto` | `subjectCode` | `subjectLabel` | `Subject` (1003) |

Each label field is resolved via `_unitOfWork.Configs.GetByTypeCodeAsync(typeCode, ct)` →
`ConfigLabelHelper.BuildLabelMap`/`MergeLabelMap`, loaded once per request (not once per row) via
two small private `StudentService` helpers — `LoadClassLabelMapAsync()` (merges Grade + Section)
and `LoadRelationshipLabelMapAsync()` (GuardianRelationship alone) — the same "load the map once,
resolve many rows against it" shape `FeeInvoiceService.LoadFeeLabelMapAsync` already established.
`ConfigLabelHelper.Resolve` falls back to the raw code itself if no matching option exists, so an
orphaned/unrecognized code degrades to showing the code, never a blank.

**Why this matters for "excessive API calls"**: before this round, a UI rendering a guardian's
relationship, a grade, a section, or a subject had exactly two options — hardcode a
code→label map client-side (drifts the moment an admin edits the Config catalog), or call
`GET /api/configs/dropdown/{typeCode}` separately and join it client-side (the "excessive calls"
pattern this round removes). Now the label travels with the row that needs it, resolved
server-side against the live catalog, in the same request.

## Frontend implication: one API call per tab, not one call for the whole page

The concrete fix in this round should be read as the reference implementation of a general rule
for any tabbed detail screen in this application (Student profile, and by the same reasoning
Teacher/Employee profiles, which already mostly follow it — Employee's Loans/Salaries/Documents/
Qualifications tabs already have their own dedicated endpoints rather than being embedded in
`GetEmployeeByIdAsync`):

1. **The always-visible header** (name, status, a handful of core fields, and just enough of the
   "current class"/"current position" to render one summary line) is the *only* thing the base
   `GET /{id}` should return.
2. **Every tab below the header gets its own endpoint**, called only when that tab is opened (or
   prefetched deliberately, if the UI wants to warm a likely-next tab — that's a frontend caching
   decision, not a reason to bloat the base response).
3. A field belongs in the base response only if it's rendered **outside** a tab. If it's only ever
   shown after clicking into a tab, it doesn't belong in the base response, full stop — that's the
   test this round applied to `guardians`/`currentEnrollment.subjects`/`enrollmentHistory`.

## Scope of this round (what this does *not* cover)

This round fixes the **Student** feature concretely — the profile response, the new timetable
endpoint, and label resolution on the five fields listed above. It does **not** retrofit every
other feature in the codebase:

- Other profile-style screens (Teacher, Employee) already mostly follow the tab-scoped-endpoint
  convention; they were not audited field-by-field as part of this round.
- Config-backed codes elsewhere in the app (fee categories already have labels via
  `FeeInvoiceService`'s own `LoadFeeLabelMapAsync`; many others — e.g. `EmploymentStatus` display
  strings, most `JobPositionCode`/`EmployeeCategoryCode` usages outside the Fee/Payroll modules —
  still return only the raw code) were **not** swept in this round. Apply the same
  `ConfigLabelHelper` pattern documented in §3 the next time one of those screens is touched, using
  this guide (and `FeeInvoiceService.LoadFeeLabelMapAsync`) as the reference shape, rather than
  re-deriving the pattern from scratch.

## No new migration

Every change in this round is DTO/service-layer only — no new entity, column, or index. Existing
endpoints keep the same routes; only `StudentDto`'s shape changed (see the table in §2) and two new
GET routes were added.
