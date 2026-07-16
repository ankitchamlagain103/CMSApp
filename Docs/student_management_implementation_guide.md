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
| Document type (teacher) | `1006` | ✅ CITIZENSHIP, ID_CARD, PAN_CARD, PASSPORT, DRIVING_LICENSE, POLICE_REPORT, ACADEMIC_CERTIFICATE, APPOINTMENT_LETTER, OTHER (see `teacher_documents_implementation_guide.md`) |
| Document type (student) | `1007` | ✅ BIRTH_CERTIFICATE, TRANSFER_CERTIFICATE, CHARACTER_CERTIFICATE, PREVIOUS_MARKSHEET, CITIZENSHIP, PASSPORT, PHOTO, IMMUNIZATION_RECORD, DISABILITY_CARD, MIGRATION_CERTIFICATE, GUARDIAN_CITIZENSHIP, OTHER (same guide) |

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
| `GET /api/academicyears?page=1&pageSize=20&search=AY2026&isCurrent=true&status=1` | | Paginated, newest `startDate` first. `search` matches `code`/`name`. Full filter list: `filters_update.md` |
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
| `GET /api/academicclasses?page=1&pageSize=20&academicYearId=…&gradeCode=…&status=1` | | Filters optional; every row nests its `sections`. Full filter list: `filters_update.md` |
| `GET /api/academicclasses/{id}` | | |
| `PUT /api/academicclasses/{id}` | `{ "status" }` | **Year/grade immutable** — a class's identity can't move under its enrollments |
| `DELETE /api/academicclasses/{id}` | | Soft; `409` while it still has sections |
| `POST /api/academicclasses/{id}/sections` | `{ "sectionCode": "SECTION_B", "capacity": 30 }` | `409` if the section already exists on the class |
| `GET /api/academicclasses/{id}/sections` | | Ordered by `sectionCode` |
| `PUT /api/academicclasses/{id}/sections/{classSectionId}` | `{ "capacity", "status" }` | `sectionCode` immutable |
| `DELETE /api/academicclasses/{id}/sections/{classSectionId}` | | Soft; `409` while the section has enrollments |
| `POST /api/academicclasses/{id}/subjects` | `{ "subjectCode": "MATH", "isMandatory": true, "displayOrder": 1, "classSectionId": null, "creditHours": 4, "fullMarks": 100, "passMarks": 40, "theoryMarks": 75, "practicalMarks": 25 }` | Class-wide by default (**applies to every section**). An **optional** subject may set `classSectionId` to be offered in that section only (`400` if mandatory); a subject is either once class-wide or once per section (`409` on overlap). Grading fields (2026-07-15) are all optional; when supplied, `passMarks` ≤ `fullMarks`, and `theoryMarks` + `practicalMarks` must equal `fullMarks` if both are given |
| `GET /api/academicclasses/{id}/subjects?classSectionId=…` | | Ordered by `displayOrder`; the optional `classSectionId` returns one section's effective list (class-wide + that section's scoped rows) |
| `PUT /api/academicclasses/{id}/subjects/{classSubjectId}` | `{ "displayOrder": 1, "creditHours": 4, "fullMarks": 100, "passMarks": 40, "theoryMarks": 75, "practicalMarks": 25 }` | **New (2026-07-15)**. Only grading metadata + `displayOrder` are editable — `subjectCode`/`isMandatory`/`classSectionId` are identity-like and immutable (re-assign instead) |
| `DELETE /api/academicclasses/{id}/subjects/{classSubjectId}` | | Hard delete; `409` while teachers are assigned to it or students have elected it |

`AcademicClassDto`: `{ "id", "academicYearId", "gradeCode", "status", "sections": [ { "id", "academicClassId", "sectionCode", "capacity", "status" } ] }`.
`ClassSubjectDto`: `{ "id", "academicClassId", "subjectCode", "isMandatory", "displayOrder", "classSectionId", "sectionCode", "scope", "creditHours", "fullMarks", "passMarks", "theoryMarks", "practicalMarks" }` — **`id` here is the `classSubjectId`** used by teacher assignments and electives; `classSectionId`/`sectionCode` null = offered to all sections. `scope` (2026-07-15) is `0` ClassWide / `1` Section — the same fact as `classSectionId`'s nullability, exposed as an explicit enum so consumers don't have to infer it. Grading fields (2026-07-15) are all nullable — a subject can exist before its marks scheme is finalized. They live on `ClassSubject` rather than the global Subject Config catalog entry because marks schemes commonly vary by grade for the same subject (e.g. Science gains practicals in grade 9-10). See `filters_update.md` for the class/section scoping redesign and `fee_and_payroll_implementation_guide.md` for this addition's rationale.

## Teachers — `/api/teachers`

**2026-07-15**: `Teacher` is now a thin teaching-specific profile (`teachingLicenseNo`/`experienceYears`/`specialization`) sharing its id with an `Employee` row that owns identity/HR/bank fields — see `employee_management_implementation_guide.md`. `POST /api/teachers` still creates everything in one call (an `Employee` row with `employeeCategoryCode = ACADEMIC` plus the `Teacher` profile), and `/api/teachers` routes/response shape stay externally compatible, just with more optional fields available.

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/teachers` | `{ "employeeCode": null, "firstName", "middleName", "lastName", "gender": 0, "dateOfBirth", "email", "phone", "joinDate", "jobPositionCode": "TEACHER", "bankName", "bankAccountNumber", "paymentMode": 1, "teachingLicenseNo", "experienceYears", "specialization" }` | `employeeCode` optional — blank = auto-generated `EMP{year}{seq}` (see `auto_number_generation_implementation_guide.md`); if supplied, unique (≤30). `jobPositionCode` must be `TEACHER`/`PRINCIPAL`/`VICE_PRINCIPAL` (catalog `1012`). Only first/last required |
| `GET /api/teachers?page=1&pageSize=20&search=priya&phone=…&qualificationCode=…&status=1&dateField=1&fromDate=2026-01-01&toDate=2026-06-30` | | `search` matches first/last name or employeeCode, case-insensitive. `dateField` (`0` CreatedDate default / `1` JoiningDate) picks which column `fromDate`/`toDate` filters. `status` is now `EmploymentStatus` (`1` Active / `2` OnLeave / `3` Suspended / `4` Resigned / `5` Terminated / `6` Retired), read off the owning `Employee` row. Full filter list: `filters_update.md` |
| `GET /api/teachers/{id}` · `PUT /api/teachers/{id}` (adds `employmentStatus`, **no `employeeCode`**) · `DELETE /api/teachers/{id}` | | Detail adds `serviceHistory` (assignments with their academic years, oldest first — "teaching here since"; see `student_profile_enhancements_implementation_guide.md`). Delete soft-deletes the owning `Employee` row; `409` while the teacher has assignments |
| `POST /api/employees/{id}/teacher-profile` | `{ "teachingLicenseNo", "experienceYears", "specialization" }` | Alternate path: promotes an *existing* `Employee` (e.g. hired as Office Assistant, later becomes a teacher) into also having a Teacher profile — requires `employeeCategoryCode = ACADEMIC` + a Teacher/Principal/Vice Principal `jobPositionCode`; `409` if a profile already exists. See `employee_management_implementation_guide.md` |
| `POST/GET /api/teachers/{id}/salaries` · `GET …/salaries/tax-calculation?fiscalYearId=` | | Thin aliases over `/api/employees/{id}/salaries` (the teacher's id *is* its Employee id via the shared-PK link) — full compensation-plan/tax-calculation reference in `employee_management_implementation_guide.md` |
| `POST /api/teachers/{id}/qualifications` | `{ "qualificationCode": "MASTERS", "courseName": "M.Sc. Mathematics", "institution": "Tribhuvan University", "completionYear": 2018, "score": "3.7 GPA", "remarks": null }` | Only `qualificationCode` required (catalog 1005); `completionYear` 1950–2100 |
| `GET /api/teachers/{id}/qualifications` | | Newest completion year first |
| `DELETE /api/teachers/{id}/qualifications/{qualificationId}` | | Hard delete |
| `POST /api/teachers/{id}/assignments` | `{ "classSubjectId": "…", "classSectionId": null, "isClassTeacher": false }` | `classSectionId` optional (null = teaches the subject to all sections; must belong to the subject's class). `409` if that exact teacher+subject+section combination exists; `isClassTeacher: true` **requires** a `classSectionId` (`400`) and `409`s if that section already has a class teacher (one per section) |
| `GET /api/teachers/{id}/assignments` | | Returns `{ id, teacherId, classSubjectId, academicClassId, subjectCode, classSectionId, sectionCode, scope, isClassTeacher }` (`classSectionId`/`sectionCode` null = all sections; `scope` `0` ClassWide / `1` Section). **Behavior fix (2026-07-15)**: assigning a teacher to a section-scoped subject now always pins the assignment to that subject's own section (previously a caller could omit `classSectionId` and end up with an assignment that looked like "all sections" for a subject that isn't offered everywhere) — see `filters_update.md` |
| `DELETE /api/teachers/{id}/assignments/{assignmentId}` | | Hard delete |
| `POST /api/teachers/{id}/documents` | **multipart/form-data**: `file` + `documentTypeCode` + `documentName` + optional `validUntil`/`remarks` | PDF/JPG/JPEG/PNG, max 10 MB; type from catalog 1006. Full reference: `teacher_documents_implementation_guide.md` |
| `GET /api/teachers/{id}/documents` · `GET …/documents/{documentId}/download` · `DELETE …/documents/{documentId}` | | List (metadata only) / raw file stream / hard delete incl. the stored file |

## Guardians — `/api/guardians`

Plain CRUD: `POST` / `GET ?page&pageSize&search=&phone=&fromDate=&toDate=` / `GET /{id}` / `PUT /{id}` / `DELETE /{id}`. `search` matches first/last name; `fromDate`/`toDate` filter on `createdTs`.
Body: `{ "firstName", "lastName", "email", "phone", "occupation", "address" }` (first/last required). A guardian is a standalone record precisely so one guardian can be linked to several students. Delete is soft; `409` while linked to any student.

## Students — `/api/students`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/students` | `{ "admissionNo": "ADM-2026-001", "firstName", "middleName", "lastName", "gender": 0, "dateOfBirth": "2015-06-10", "email", "phone", "address", "admissionDate": "2026-04-01", "guardians": [ { "guardianId": "…", "relationshipCode": "FATHER", "isPrimary": true }, { "firstName": "Maya", "lastName": "Sharma", "phone": "…", "relationshipCode": "MOTHER" } ] }` | `admissionNo` optional — blank = auto-generated `ADM{year}{seq}` (see `auto_number_generation_implementation_guide.md`); if supplied, unique (≤30). Required: first/last + gender. `guardians` optional — each entry is an existing guardian (`guardianId`) or inline new-guardian fields; at most one `isPrimary`; all-or-nothing |
| `GET /api/students?page=1&pageSize=20&search=adm&phone=…&gradeCode=…&academicYearId=…&classSectionId=…&status=1&gender=0&dateField=1&fromDate=2026-01-01&toDate=2026-06-30` | | `search` matches names or admissionNo; `guardians` left empty on list rows. `gradeCode`/`academicYearId`/`classSectionId` match a student's current **Enrolled**-status enrollment (a snapshot, not full history). `dateField` (`0` CreatedDate default / `1` EnrollmentDate) picks which column `fromDate`/`toDate` filters. Full filter list: `filters_update.md` |
| `GET /api/students/{id}` · `PUT /api/students/{id}` (adds `status` and three-way `guardians`; **no `admissionNo`**) · `DELETE /api/students/{id}` | | Detail (and create/update) responses include `guardians`; detail adds `currentEnrollment` (current year/grade/section/roll + subjects studying) and `enrollmentHistory` (every enrollment, oldest year first — "studying here since") — see `student_profile_enhancements_implementation_guide.md`. Update's `guardians`: null = unchanged, `[]` = unlink all, list = replace-sync. Delete is soft — enrollment history survives |
| `POST /api/students/{id}/guardians` | `{ "guardianId": "…", "relationshipCode": "FATHER", "isPrimary": true }` | `409` if already linked; `isPrimary: true` automatically demotes the previous primary |
| `GET /api/students/{id}/guardians` | | Primary first; each row flattens guardian name/phone/email — no per-row guardian lookup needed |
| `DELETE /api/students/{id}/guardians/{linkId}` | | `linkId` is the link row's `id`, **not** the guardianId |
| `POST /api/students/{id}/documents` | **multipart/form-data**: `file` + `documentTypeCode` + `documentName` + optional `validUntil`/`remarks` | PDF/JPG/JPEG/PNG, max 10 MB; type from catalog 1007. Same shape as teacher documents — `teacher_documents_implementation_guide.md` |
| `GET /api/students/{id}/documents` · `GET …/documents/{documentId}/download` · `DELETE …/documents/{documentId}` | | List (metadata only) / raw file stream / hard delete incl. the stored file |
| `GET /api/students/{id}/id-card-preview` | | Renders an admin-configurable HTML template with this student's profile/enrollment/guardian data — see `document_preview_implementation_guide.md`. Same shape at `GET /api/teachers/{id}/id-card-preview` for teachers |

## Enrollments — `/api/enrollments`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/enrollments` | `{ "studentId": "…", "classSectionId": "…", "rollNumber": "07", "enrollmentDate": "2026-04-05" }` | Fails `409` if: student already has a row in that section, **student already has an active (status-1) enrollment anywhere in that academic year**, section at capacity (only status-1 rows count), or roll number taken in that section |
| `GET /api/enrollments?page=1&pageSize=20&studentId=…&academicClassId=…&classSectionId=…&academicYearId=…&status=1&dateField=0&fromDate=2026-01-01&toDate=2026-06-30` | | All filters optional; `academicClassId` = whole grade, `classSectionId` = one section. `dateField` (`0` EnrollmentDate default / `1` CreatedDate) picks which column `fromDate`/`toDate` filters. Rows flatten `studentAdmissionNo`/`studentFirstName`/`studentLastName` plus `academicClassId`/`academicYearId`/`gradeCode`/`sectionCode`. Full filter list: `filters_update.md` |
| `GET /api/enrollments/{id}` | | |
| `PUT /api/enrollments/{id}` | `{ "rollNumber", "enrollmentDate", "status" }` | Student/section immutable — to move a student, set this to `2` (Transferred) and create a new enrollment. Setting status back to `1` re-runs the one-active-per-year check |
| `DELETE /api/enrollments/{id}` | | Soft delete. Note: the (student, section) pair stays reserved, so prefer status changes over delete |
| `POST /api/enrollments/{id}/subjects/{classSubjectId}` | | Electives only — `400` if the subject is mandatory (mandatory applies automatically), belongs to a different class, or is scoped to a different section than the enrollment's; `409` if already added |
| `GET /api/enrollments/{id}/subjects` · `DELETE /api/enrollments/{id}/subjects/{electiveSubjectId}` | | |
| `POST /api/enrollments/{id}/fee-selections/{feeStructureItemId}` · `GET .../fee-selections` · `DELETE .../fee-selections/{feeSelectionId}` | | Opts the enrollment into an **optional** fee item (by its id, e.g. Hostel/Meal/Transport/Special Training); `400` if not optional or not charged by the class. See `fee_management_implementation_guide.md` |
| `GET /api/enrollments/{id}/discounts` / `/scholarships` (+ `POST`/`DELETE`) · `GET /api/enrollments/{id}/fee-structure` | | Per-enrollment discount/scholarship awards and the composed priced fee view for the student detail page — full detail in `fee_management_implementation_guide.md` |
| `GET /api/enrollments/{id}/fee-receipt-preview` | | Renders an admin-configurable HTML fee receipt from the same data as `fee-structure` — see `document_preview_implementation_guide.md` |

---

## Failure summary (all endpoints)

| HTTP | `responseCode` | Typical causes |
|---|---|---|
| 400 | `VALIDATION_ERROR` | Missing/too-long field; unknown catalog code; elective rule violations |
| 400 | `CONFLICT` | Duplicate code/employeeCode/admissionNo (soft-deleted rows still reserve them); duplicate link/assignment; class at capacity; roll number taken; delete blocked by children |
| 404 | `NOT_FOUND` | Unknown id anywhere (including a link/child id not belonging to the parent in the route) |
| 403 | `FORBIDDEN` | Caller's roles lack the permission row (all endpoints are permission-gated; SuperAdmin bypasses) |

## Permissions (seeded to SuperAdmin only, grant to other roles via `POST /api/roles/claims`)

Three new admin main menus: `ACADEMIC_MANAGEMENT`, `TEACHER_MANAGEMENT`, `STUDENT_MANAGEMENT`. Permission codes follow the existing convention — `YEAR_*`, `CLASS_*`, `CLASS_SECTION_*`, `CLASS_SUBJECT_*`, `TEACHER_*`, `TEACHER_QUALIFICATION_*`, `TEACHER_ASSIGNMENT_*`, `TEACHER_DOCUMENT_*`, `STUDENT_*`, `STUDENT_GUARDIAN_*`, `STUDENT_DOCUMENT_*`, `GUARDIAN_*`, `ENROLLMENT_*`, `ENROLLMENT_SUBJECT_*`.

## Typical UI flows

1. **Setup (admin, once per school):** create Grade/Section/Subject config options → create the academic year (`isCurrent: true`) → create one class per grade with its sections → assign subjects to each class (shared by all its sections; optional subjects may be scoped to one section). **Every following year:** create the year → `clone-structure` from the previous one → adjust deltas.
2. **Class roster screen:** class + section cascading dropdowns (filter by current year) → `GET /api/enrollments?classSectionId=…` → rows already carry student names/roll numbers/grade/section.
3. **Student admission wizard:** create student **with guardians inline** (pick existing or enter new, one primary) → enroll into a section (roll number optional) → add electives.
4. **Teacher profile:** teacher basics + qualifications table (add/remove rows) + assignments table (pick class → `GET .../subjects` to list its classSubjects → assign, optionally as class teacher).

## Backend notes

- 12 tables (`dbo.academic_years`, `dbo.academic_classes`, `dbo.class_sections`, `dbo.class_subjects`, `dbo.teachers`, `dbo.teacher_qualifications`, `dbo.teacher_assignments`, `dbo.guardians`, `dbo.students`, `dbo.student_guardians`, `dbo.enrollments`, `dbo.enrollment_subjects`). The original 11 have their EF Core migration (2026-07-12); **the 2026-07-13 restructure (new `class_sections`, changed `academic_classes`/`enrollments`) still needs its migration** — user-owned, as always.
- Config-code columns (`grade_code`, `section_code`, `subject_code`, `relationship_code`, `qualification_code`) are **not database FKs** (a Config code is only unique per type) — validity is enforced in the services. Corollary: **deleting a Config option that's already referenced leaves existing rows pointing at a dead code** — the admin UI should warn before deleting Grade/Section/Subject options that are in use.
- Soft-deleted years/classes/teachers/students/enrollments keep their unique codes/pairs reserved (clean 409, by design). Link rows (subjects, assignments, guardian links, electives) hard-delete.
