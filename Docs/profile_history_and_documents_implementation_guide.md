# CMSApp — Profile History, Subject Teachers & Student Documents (UI)

**What shipped (2026-07-13, this batch)** — four related profile upgrades:

1. **Student schooling history** — `GET /api/students/{id}` returns `enrollmentHistory`: every enrollment ever, oldest academic year first ("studying in this school since AY2082").
2. **Teacher service history** — `GET /api/teachers/{id}` returns `serviceHistory`: assignments with their academic years, oldest first ("teaching here since AY2081").
3. **Teacher name on each subject** — the student profile's `currentEnrollment.subjects` rows now carry `teacherName` (who teaches that subject to the student's section).
4. **Student documents** — `/api/students/{id}/documents` mirrors teacher documents (upload/list/download/delete, catalog `1007`). Full endpoint reference: `teacher_documents_implementation_guide.md`.

Only item 4 needs a migration (`dbo.student_documents` — already in the pending wave). Items 1–3 are read-only response enrichments over existing tables.

---

## 1. Student `enrollmentHistory`

`GET /api/students/{id}` (detail only — the paged list stays light):

```json
"enrollmentHistory": [
  { "enrollmentId": "…",
    "academicYearId": "…", "academicYearCode": "AY2082", "academicYearName": "2082/83", "academicYearStartDate": "2025-04-01",
    "academicClassId": "…", "gradeCode": "NURSERY",
    "classSectionId": "…", "sectionCode": "SECTION_A",
    "rollNumber": "1", "enrollmentDate": "2025-04-05", "status": 4 },
  { "…": "…", "academicYearCode": "AY2083", "gradeCode": "LKG", "status": 1 }
]
```

- Includes **every** enrollment regardless of status (`1` Enrolled, `2` Transferred, `3` Withdrawn, `4` Completed), ordered oldest academic year first, so the rows read as the student's promotion timeline.
- **"Studying here since"**: first row's `academicYearCode` (show beside `admissionDate` in the profile header).
- Empty array = never enrolled. Render as a **History** tab next to Current Class / Guardians / Documents.

## 2. Teacher `serviceHistory`

`GET /api/teachers/{id}` (detail only):

```json
"serviceHistory": [
  { "assignmentId": "…",
    "academicYearId": "…", "academicYearCode": "AY2083", "academicYearName": "2083/84", "academicYearStartDate": "2026-04-01",
    "academicClassId": "…", "gradeCode": "LKG",
    "classSectionId": null, "sectionCode": null,
    "classSubjectId": "…", "subjectCode": "NEPALI", "isClassTeacher": false }
]
```

- One row per assignment, oldest academic year first; `sectionCode` null = teaches all sections of that class.
- **"Teaching here since"**: first row's `academicYearCode`, shown alongside `joiningDate`. Group rows by year for a per-year timeline — a **History** tab beside Qualifications / Class Assignments / Documents.
- Caveat: it's assignment-based — a year with no class/subject assignment produces no row.

## 3. `teacherName` on subjects studying

Each row of the student profile's `currentEnrollment.subjects` now includes the teacher:

```json
"subjects": [
  { "classSubjectId": "…", "subjectCode": "NEPALI", "isMandatory": true,  "displayOrder": 1, "teacherName": "Ankit Chamlagain" },
  { "classSubjectId": "…", "subjectCode": "MATH",   "isMandatory": true,  "displayOrder": 3, "teacherName": null }
]
```

- Resolution: assignments **scoped to the student's section win** over all-section assignments; several teachers sharing one subject come back comma-joined; `null` = nobody assigned yet (show "—" or an "unassigned" hint).
- Nice UI: subject chip with the teacher name as secondary text/tooltip.

## 4. Student documents

Same four endpoints as teacher documents, under the student:

| Method/Route | Notes |
|---|---|
| `POST /api/students/{id}/documents` | multipart/form-data: `file` (PDF/JPG/JPEG/PNG, ≤10 MB) + `documentTypeCode` (catalog **1007**) + `documentName` + optional `validUntil`/`remarks` |
| `GET /api/students/{id}/documents` | Metadata list (`StudentDocumentDto` — no file bytes, no storage path) |
| `GET /api/students/{id}/documents/{documentId}/download` | Raw file stream (non-enveloped success; errors enveloped). Needs the Authorization header → fetch as blob |
| `DELETE /api/students/{id}/documents/{documentId}` | Hard delete, file removed too |

Seeded type options (`GET /api/configs/dropdown/1007`): `BIRTH_CERTIFICATE`, `TRANSFER_CERTIFICATE`, `CHARACTER_CERTIFICATE`, `PREVIOUS_MARKSHEET`, `CITIZENSHIP`, `PASSPORT`, `PHOTO`, `IMMUNIZATION_RECORD`, `DISABILITY_CARD`, `MIGRATION_CERTIFICATE`, `GUARDIAN_CITIZENSHIP`, `OTHER`. (Deliberately separate from the teacher catalog `1006`.)

## Permissions

- Items 1–3 ride on existing `STUDENT_DETAIL` / `TEACHER_DETAIL` — nothing new to grant.
- Item 4: `STUDENT_DOCUMENT_UPLOAD`, `STUDENT_DOCUMENT_LIST`, `STUDENT_DOCUMENT_DOWNLOAD`, `STUDENT_DOCUMENT_DELETE` (seeded to SuperAdmin; grant via `POST /api/roles/claims`).

## Suggested profile layouts

- **Student profile tabs**: Current Class (year/class/section/roll + subject chips with teacher names) · Guardians · **History** (enrollment timeline) · **Documents**.
- **Teacher profile tabs**: Qualifications · Class Assignments · **Documents** · **History** (service timeline; "Teaching since" in the header).
