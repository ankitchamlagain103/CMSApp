# CMSApp — Student Management System (UI)

**What shipped (2026-07-12)**: a full student-management backend — academic years, classes, class subjects, teachers (with qualification records and class assignments), guardians, students (with guardian links), and enrollments (with elective subjects). Follows every convention in `UI-Implementation-Guide.md` (envelope, camelCase, Bearer token, permission-gated).

**Restructured (2026-07-13)** — see `class_section_structure_implementation_guide.md` for the full breaking-change detail: a class is now one grade per year with **sections nested inside it**, subjects are mapped once per class (shared by all sections), enrollments target a **section** (`classSectionId`) and a student can hold only one active enrollment per academic year, and guardians can be captured directly in `POST /api/students`. The tables below are already updated to the new shapes.

**Students and teachers are records, not accounts** — they cannot log in. Login for them is a planned later phase; nothing in this feature touches auth.

---

## Key design point: dropdown catalogs instead of tables

Grade, Section, Subject, Guardian Relationship, and Teacher Qualification are **not** their own tables/endpoints — they are entries in the existing Config catalog, referenced everywhere **by their `code` string** (not id). Populate every such dropdown from the existing endpoint:

```
GET /api/configs/dropdown/{typeCode}     (any authenticated user, no permission needed)
```

| Catalog | `typeCode` | Seeded options? |
|---|---|---|
| Grade | `1001` | ❌ admin creates them (`POST /api/configs`, e.g. `GRADE_1`/"Grade 1") |
| Section | `1002` | ❌ admin creates them (e.g. `SECTION_A`/"A") |
| Subject | `1003` | ❌ admin creates them; convention: `additionalValue1` = short name, `additionalValue2` = credit, `additionalValue3` = category (`CORE`/`ELECTIVE` display hint) |
| Guardian relationship | `1004` | ✅ FATHER, MOTHER, GRANDFATHER, GRANDMOTHER, BROTHER, SISTER, UNCLE, AUNT, LEGAL_GUARDIAN, OTHER |
| Teacher qualification | `1005` | ✅ PHD, MASTERS, BACHELORS, DIPLOMA, CERTIFICATE, OTHER |
| Document type | `1006` | ✅ CITIZENSHIP, ID_CARD, PAN_CARD, PASSPORT, DRIVING_LICENSE, POLICE_REPORT, ACADEMIC_CERTIFICATE, APPOINTMENT_LETTER, OTHER (see `teacher_documents_implementation_guide.md`) |

The `ConfigType` rows themselves are seeded at startup (`ConfigCatalogSeeder`). **Sending a code that isn't in the catalog fails with `400 VALIDATION_ERROR`** (e.g. `"GradeCode 'GRADE_99' is not a known grade option."`). API responses return codes only — resolve display labels from the cached dropdown data.

⚠️ Grade/Section/Subject options must be created **before** any class can be created — build the admin "Configs" screen flow first, or create them via Swagger as superadmin.

## Enums (numbers, like `gender`/`userType`)

- `status` on years/classes/teachers/students (`RecordStatus`): `1` Active, `2` Inactive.
- `status` on enrollments (`EnrollmentStatus`): `1` Enrolled, `2` Transferred, `3` Withdrawn, `4` Completed.
- Dates are ISO `yyyy-MM-dd` (date-only semantics).

---

## Academic Years — `/api/academicyears`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/academicyears` | `{ "code": "AY2026", "name": "2026-2027", "startDate": "2026-04-01", "endDate": "2027-03-31", "isCurrent": true }` | `code` unique (≤20); `endDate` > `startDate`; `isCurrent: true` automatically un-flags every other year |
| `GET /api/academicyears?page=1&pageSize=20` | | Paginated, newest `startDate` first |
| `GET /api/academicyears/{id}` | | |
| `PUT /api/academicyears/{id}` | `{ "name", "startDate", "endDate", "isCurrent", "status" }` | **`code` is immutable** — not in the body |
| `DELETE /api/academicyears/{id}` | | Soft delete; `409 CONFLICT` while the year still has classes |
| `POST /api/academicyears/{id}/clone-structure` | `{ "sourceAcademicYearId": "…" }` | Copies the source year's classes + sections + subject mappings into year `{id}`; grades already present are skipped (returned in `skippedGradeCodes`); enrollments/teacher assignments are not copied |

`AcademicYearDto`: `{ "id", "code", "name", "startDate", "endDate", "isCurrent", "status" }`

## Classes — `/api/academicclasses`

A class = **one grade within one academic year** (`(year, gradeCode)` unique); its sections are child rows (`(class, sectionCode)` unique). Capacity lives on the section.

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/academicclasses` | `{ "academicYearId": "…", "gradeCode": "GRADE_1", "sections": [ { "sectionCode": "SECTION_A", "capacity": 40 } ] }` | `sections` optional; every code validated against the catalogs; `capacity: 0` = unlimited |
| `GET /api/academicclasses?page=1&pageSize=20&academicYearId=…` | | Filter optional; every row nests its `sections` |
| `GET /api/academicclasses/{id}` | | |
| `PUT /api/academicclasses/{id}` | `{ "status" }` | **Year/grade immutable** — a class's identity can't move under its enrollments |
| `DELETE /api/academicclasses/{id}` | | Soft; `409` while it still has sections |
| `POST /api/academicclasses/{id}/sections` | `{ "sectionCode": "SECTION_B", "capacity": 30 }` | `409` if the section already exists on the class |
| `GET /api/academicclasses/{id}/sections` | | Ordered by `sectionCode` |
| `PUT /api/academicclasses/{id}/sections/{classSectionId}` | `{ "capacity", "status" }` | `sectionCode` immutable |
| `DELETE /api/academicclasses/{id}/sections/{classSectionId}` | | Soft; `409` while the section has enrollments |
| `POST /api/academicclasses/{id}/subjects` | `{ "subjectCode": "MATH", "isMandatory": true, "displayOrder": 1, "classSectionId": null }` | Class-wide by default (**applies to every section**). An **optional** subject may set `classSectionId` to be offered in that section only (`400` if mandatory); a subject is either once class-wide or once per section (`409` on overlap) |
| `GET /api/academicclasses/{id}/subjects?classSectionId=…` | | Ordered by `displayOrder`; the optional `classSectionId` returns one section's effective list (class-wide + that section's scoped rows) |
| `DELETE /api/academicclasses/{id}/subjects/{classSubjectId}` | | Hard delete; `409` while teachers are assigned to it or students have elected it |

`AcademicClassDto`: `{ "id", "academicYearId", "gradeCode", "status", "sections": [ { "id", "academicClassId", "sectionCode", "capacity", "status" } ] }`.
`ClassSubjectDto`: `{ "id", "academicClassId", "subjectCode", "isMandatory", "displayOrder", "classSectionId", "sectionCode" }` — **`id` here is the `classSubjectId`** used by teacher assignments and electives; `classSectionId`/`sectionCode` null = offered to all sections.

## Teachers — `/api/teachers`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/teachers` | `{ "employeeNo": "EMP001", "firstName", "middleName", "lastName", "email", "phone", "joiningDate" }` | `employeeNo` unique (≤30); everything but employeeNo/first/last optional |
| `GET /api/teachers?page=1&pageSize=20&search=priya` | | `search` matches first/last name or employeeNo, case-insensitive |
| `GET /api/teachers/{id}` · `PUT /api/teachers/{id}` (adds `status`, **no `employeeNo`**) · `DELETE /api/teachers/{id}` | | Delete is soft; `409` while the teacher has assignments |
| `POST /api/teachers/{id}/qualifications` | `{ "qualificationCode": "MASTERS", "courseName": "M.Sc. Mathematics", "institution": "Tribhuvan University", "completionYear": 2018, "score": "3.7 GPA", "remarks": null }` | Only `qualificationCode` required (catalog 1005); `completionYear` 1950–2100 |
| `GET /api/teachers/{id}/qualifications` | | Newest completion year first |
| `DELETE /api/teachers/{id}/qualifications/{qualificationId}` | | Hard delete |
| `POST /api/teachers/{id}/assignments` | `{ "classSubjectId": "…", "classSectionId": null, "isClassTeacher": false }` | `classSectionId` optional (null = teaches the subject to all sections; must belong to the subject's class). `409` if that exact teacher+subject+section combination exists; `isClassTeacher: true` **requires** a `classSectionId` (`400`) and `409`s if that section already has a class teacher (one per section) |
| `GET /api/teachers/{id}/assignments` | | Returns `{ id, teacherId, classSubjectId, academicClassId, subjectCode, classSectionId, sectionCode, isClassTeacher }` (`classSectionId`/`sectionCode` null = all sections) |
| `DELETE /api/teachers/{id}/assignments/{assignmentId}` | | Hard delete |
| `POST /api/teachers/{id}/documents` | **multipart/form-data**: `file` + `documentTypeCode` + `documentName` + optional `validUntil`/`remarks` | PDF/JPG/JPEG/PNG, max 10 MB; type from catalog 1006. Full reference: `teacher_documents_implementation_guide.md` |
| `GET /api/teachers/{id}/documents` · `GET …/documents/{documentId}/download` · `DELETE …/documents/{documentId}` | | List (metadata only) / raw file stream / hard delete incl. the stored file |

## Guardians — `/api/guardians`

Plain CRUD: `POST` / `GET ?page&pageSize` / `GET /{id}` / `PUT /{id}` / `DELETE /{id}`.
Body: `{ "firstName", "lastName", "email", "phone", "occupation", "address" }` (first/last required). A guardian is a standalone record precisely so one guardian can be linked to several students. Delete is soft; `409` while linked to any student.

## Students — `/api/students`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/students` | `{ "admissionNo": "ADM-2026-001", "firstName", "middleName", "lastName", "gender": 0, "dateOfBirth": "2015-06-10", "email", "phone", "address", "admissionDate": "2026-04-01", "guardians": [ { "guardianId": "…", "relationshipCode": "FATHER", "isPrimary": true }, { "firstName": "Maya", "lastName": "Sharma", "phone": "…", "relationshipCode": "MOTHER" } ] }` | `admissionNo` unique (≤30), required + first/last + gender; rest optional. `guardians` optional — each entry is an existing guardian (`guardianId`) or inline new-guardian fields; at most one `isPrimary`; all-or-nothing |
| `GET /api/students?page=1&pageSize=20&search=adm` | | `search` matches names or admissionNo; `guardians` left empty on list rows |
| `GET /api/students/{id}` · `PUT /api/students/{id}` (adds `status` and three-way `guardians`; **no `admissionNo`**) · `DELETE /api/students/{id}` | | Detail (and create/update) responses include `guardians` and detail adds `currentEnrollment` (current year/grade/section/roll + subjects studying) — see `student_profile_enhancements_implementation_guide.md`. Update's `guardians`: null = unchanged, `[]` = unlink all, list = replace-sync. Delete is soft — enrollment history survives |
| `POST /api/students/{id}/guardians` | `{ "guardianId": "…", "relationshipCode": "FATHER", "isPrimary": true }` | `409` if already linked; `isPrimary: true` automatically demotes the previous primary |
| `GET /api/students/{id}/guardians` | | Primary first; each row flattens guardian name/phone/email — no per-row guardian lookup needed |
| `DELETE /api/students/{id}/guardians/{linkId}` | | `linkId` is the link row's `id`, **not** the guardianId |

## Enrollments — `/api/enrollments`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/enrollments` | `{ "studentId": "…", "classSectionId": "…", "rollNumber": "07", "enrollmentDate": "2026-04-05" }` | Fails `409` if: student already has a row in that section, **student already has an active (status-1) enrollment anywhere in that academic year**, section at capacity (only status-1 rows count), or roll number taken in that section |
| `GET /api/enrollments?page=1&pageSize=20&studentId=…&academicClassId=…&classSectionId=…` | | All filters optional; `academicClassId` = whole grade, `classSectionId` = one section. Rows flatten `studentAdmissionNo`/`studentFirstName`/`studentLastName` plus `academicClassId`/`academicYearId`/`gradeCode`/`sectionCode` |
| `GET /api/enrollments/{id}` | | |
| `PUT /api/enrollments/{id}` | `{ "rollNumber", "enrollmentDate", "status" }` | Student/section immutable — to move a student, set this to `2` (Transferred) and create a new enrollment. Setting status back to `1` re-runs the one-active-per-year check |
| `DELETE /api/enrollments/{id}` | | Soft delete. Note: the (student, section) pair stays reserved, so prefer status changes over delete |
| `POST /api/enrollments/{id}/subjects/{classSubjectId}` | | Electives only — `400` if the subject is mandatory (mandatory applies automatically), belongs to a different class, or is scoped to a different section than the enrollment's; `409` if already added |
| `GET /api/enrollments/{id}/subjects` · `DELETE /api/enrollments/{id}/subjects/{electiveSubjectId}` | | |

---

## Failure summary (all endpoints)

| HTTP | `responseCode` | Typical causes |
|---|---|---|
| 400 | `VALIDATION_ERROR` | Missing/too-long field; unknown catalog code; elective rule violations |
| 400 | `CONFLICT` | Duplicate code/employeeNo/admissionNo (soft-deleted rows still reserve them); duplicate link/assignment; class at capacity; roll number taken; delete blocked by children |
| 404 | `NOT_FOUND` | Unknown id anywhere (including a link/child id not belonging to the parent in the route) |
| 403 | `FORBIDDEN` | Caller's roles lack the permission row (all endpoints are permission-gated; SuperAdmin bypasses) |

## Permissions (seeded to SuperAdmin only, grant to other roles via `POST /api/roles/claims`)

Three new admin main menus: `ACADEMIC_MANAGEMENT`, `TEACHER_MANAGEMENT`, `STUDENT_MANAGEMENT`. Permission codes follow the existing convention — `YEAR_*`, `CLASS_*`, `CLASS_SECTION_*`, `CLASS_SUBJECT_*`, `TEACHER_*`, `TEACHER_QUALIFICATION_*`, `TEACHER_ASSIGNMENT_*`, `TEACHER_DOCUMENT_*`, `STUDENT_*`, `STUDENT_GUARDIAN_*`, `GUARDIAN_*`, `ENROLLMENT_*`, `ENROLLMENT_SUBJECT_*`.

## Typical UI flows

1. **Setup (admin, once per school):** create Grade/Section/Subject config options → create the academic year (`isCurrent: true`) → create one class per grade with its sections → assign subjects to each class (shared by all its sections; optional subjects may be scoped to one section). **Every following year:** create the year → `clone-structure` from the previous one → adjust deltas.
2. **Class roster screen:** class + section cascading dropdowns (filter by current year) → `GET /api/enrollments?classSectionId=…` → rows already carry student names/roll numbers/grade/section.
3. **Student admission wizard:** create student **with guardians inline** (pick existing or enter new, one primary) → enroll into a section (roll number optional) → add electives.
4. **Teacher profile:** teacher basics + qualifications table (add/remove rows) + assignments table (pick class → `GET .../subjects` to list its classSubjects → assign, optionally as class teacher).

## Backend notes

- 12 tables (`dbo.academic_years`, `dbo.academic_classes`, `dbo.class_sections`, `dbo.class_subjects`, `dbo.teachers`, `dbo.teacher_qualifications`, `dbo.teacher_assignments`, `dbo.guardians`, `dbo.students`, `dbo.student_guardians`, `dbo.enrollments`, `dbo.enrollment_subjects`). The original 11 have their EF Core migration (2026-07-12); **the 2026-07-13 restructure (new `class_sections`, changed `academic_classes`/`enrollments`) still needs its migration** — user-owned, as always.
- Config-code columns (`grade_code`, `section_code`, `subject_code`, `relationship_code`, `qualification_code`) are **not database FKs** (a Config code is only unique per type) — validity is enforced in the services. Corollary: **deleting a Config option that's already referenced leaves existing rows pointing at a dead code** — the admin UI should warn before deleting Grade/Section/Subject options that are in use.
- Soft-deleted years/classes/teachers/students/enrollments keep their unique codes/pairs reserved (clean 409, by design). Link rows (subjects, assignments, guardian links, electives) hard-delete.
