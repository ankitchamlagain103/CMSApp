# CMSApp — Class/Section Restructure, Enrollment Guard & Guardian Onboarding (UI)

**What changed (2026-07-13)**: four related fixes to the student-management module, driven by real usage of the first UI build. These are **breaking changes** to the Classes and Enrollments endpoints — the UI must be updated together with this backend.

1. **A class is now the grade, with sections inside it.** Previously "LKG Section A" and "LKG Section B" were two unrelated class rows. Now there is **one class per grade per academic year** (`LKG 2026`), and its sections (`A`, `B`, …) are child rows of that class.
2. **Subjects are mapped once per class and automatically apply to every section.** No more assigning Nepali/English/Math separately to Section A and Section B — the subject list belongs to the grade-level class. Optional (elective) subjects are unchanged: mark the subject `isMandatory: false` on the class, and each student picks it per enrollment.
3. **A student can hold only one active enrollment per academic year.** The bug where one student was simultaneously `Enrolled` in Nursery *and* LKG is now rejected with `409 CONFLICT`. Moving a student means closing the old enrollment first (status → Transferred/Withdrawn/Completed).
4. **Guardians are captured during student onboarding**, in the same `POST /api/students` call — and the student profile (`GET /api/students/{id}`) now returns guardian details inline.

5. **Teacher assignments are section-aware, and the class-teacher flag is per section.** An assignment can target one section of the class (or all sections, by omitting the section), and `isClassTeacher: true` now requires a section — one class teacher per **section**, not per grade.
6. **Optional subjects can be scoped to one section.** Mandatory subjects are always class-wide; an optional subject can be offered to all sections (default) or to a single section (`classSectionId` on the assign-subject call).
7. **New-year setup is one call**: `POST /api/academicyears/{id}/clone-structure` copies last year's classes, sections, and subject mappings into a new year.

> ⚠️ **Migration required (backend/DBA step, not UI):** this restructure adds `dbo.class_sections`, drops `section_code`/`capacity` from `dbo.academic_classes`, repoints `dbo.enrollments` from `academic_class_id` to `class_section_id`, adds nullable `class_section_id` to `dbo.teacher_assignments` (unique index becomes teacher+subject+section), and adds nullable `class_section_id` to `dbo.class_subjects` (unique index becomes class+subject+section). The EF migration is user-owned and must be created/applied before this build runs. Existing class/enrollment rows need a data migration decided by the owner (each old grade+section row becomes one class + one section).

---

## 1. Classes — `/api/academicclasses` (restructured)

A class = **one grade in one academic year** (`(year, gradeCode)` unique). Sections live inside it.

### Create a class (sections can come inline)

```
POST /api/academicclasses
{
  "academicYearId": "b3c1…",
  "gradeCode": "LKG",
  "sections": [
    { "sectionCode": "SECTION_A", "capacity": 30 },
    { "sectionCode": "SECTION_B", "capacity": 30 }
  ]
}
```

- `sections` is optional (empty array is fine) — sections can also be added later.
- Every `sectionCode` is validated against the Section catalog (typeCode `1002`); a duplicate code inside the list is a `400`.
- `409 CONFLICT` if a class for that year+grade already exists — the message tells you to add sections to the existing class instead.

### Response shape — `AcademicClassDto` (changed)

```json
{
  "id": "…",
  "academicYearId": "…",
  "gradeCode": "LKG",
  "status": 1,
  "sections": [
    { "id": "…", "academicClassId": "…", "sectionCode": "SECTION_A", "capacity": 30, "status": 1 },
    { "id": "…", "academicClassId": "…", "sectionCode": "SECTION_B", "capacity": 30, "status": 1 }
  ]
}
```

`sectionCode` and `capacity` are **gone from the class level** — capacity is per section. The list endpoint (`GET /api/academicclasses?page=1&pageSize=20&academicYearId=…`) returns this same shape with `sections` populated, so the Classes screen renders **one row per grade, expandable to its sections** — no client-side grouping needed.

### Full endpoint table

| Method/Route | Body | Notes |
|---|---|---|
| `POST /api/academicclasses` | see above | Creates class + optional initial sections in one call |
| `GET /api/academicclasses?academicYearId=…&page&pageSize` | | `sections` nested in every row |
| `GET /api/academicclasses/{id}` | | |
| `PUT /api/academicclasses/{id}` | `{ "status": 1 }` | Year/grade immutable; **capacity no longer here** |
| `DELETE /api/academicclasses/{id}` | | Soft; `409` while it still has sections |
| `POST /api/academicclasses/{id}/sections` | `{ "sectionCode": "SECTION_C", "capacity": 25 }` | `409` if that section already exists on the class (soft-deleted included) |
| `GET /api/academicclasses/{id}/sections` | | Ordered by `sectionCode` |
| `PUT /api/academicclasses/{id}/sections/{classSectionId}` | `{ "capacity": 35, "status": 1 }` | `sectionCode` immutable |
| `DELETE /api/academicclasses/{id}/sections/{classSectionId}` | | Soft; `409` while the section has enrollments |

## 2. Class subjects — mapped once per class, optionally scoped per section

The subject routes are unchanged (`POST/GET /api/academicclasses/{id}/subjects`, `DELETE …/subjects/{classSubjectId}`), but `{id}` is the **grade-level class**, and the assign body gained an optional `classSectionId`:

```
POST /api/academicclasses/{id}/subjects
{ "subjectCode": "MATH",  "isMandatory": true,  "displayOrder": 1 }                          // class-wide
{ "subjectCode": "DANCE", "isMandatory": false, "displayOrder": 9, "classSectionId": "…" }   // Section A only
```

Rules:

| HTTP / code | Cause |
|---|---|
| `400 VALIDATION_ERROR` | `classSectionId` on a **mandatory** subject — mandatory is always class-wide |
| `400 VALIDATION_ERROR` | Section belongs to a different class |
| `409 CONFLICT` | Subject already offered class-wide (remove that row before scoping per section) |
| `409 CONFLICT` | Subject already offered in that same section, or (when adding class-wide) any row for it exists |

So a subject is offered either **once class-wide or once per section, never both**. `ClassSubjectDto` now carries `classSectionId` + `sectionCode` (null = all sections).

**Effective subject list for one section** — what the elective picker should call:

```
GET /api/academicclasses/{id}/subjects?classSectionId=…
```

returns class-wide rows **plus** that section's scoped rows. Without the query param you get every row (admin mapping screen).

- The "Subjects" dialog belongs on the **class row**: mandatory subjects + all-section optionals at class level, with a section chip on the section-scoped ones.
- Electives per enrollment work as before (`POST /api/enrollments/{id}/subjects/{classSubjectId}`), with one new rule: a section-scoped optional can only be picked by a student **enrolled in that section** (`400` otherwise).
- Mandatory subjects still apply implicitly to every enrolled student — no elective row needed or allowed (`400` if attempted).
- Teacher assignments on a section-scoped subject must target that same section (or omit the section); a different section is a `400`.
- Deleting a section also `409`s while section-scoped subjects still point at it.

## 3. Enrollments — `/api/enrollments` (breaking + new invariant)

### Create (field renamed: `academicClassId` → `classSectionId`)

```
POST /api/enrollments
{
  "studentId": "…",
  "classSectionId": "…",     // the SECTION the student sits in
  "rollNumber": "07",
  "enrollmentDate": "2026-04-05"
}
```

Failure order worth surfacing in the UI:

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown student or section |
| `409 CONFLICT` | Student already has an enrollment row in that section (incl. soft-deleted) |
| `409 CONFLICT` | **New:** student already has an *active* (`status: 1`) enrollment in **any class of that academic year** — close it first |
| `409 CONFLICT` | Section at capacity (only `status: 1` rows count; capacity `0` = unlimited) |
| `409 CONFLICT` | Roll number taken **in that section** |

### Response shape — `EnrollmentDto` (changed)

```json
{
  "id": "…",
  "studentId": "…",
  "classSectionId": "…",
  "academicClassId": "…",
  "academicYearId": "…",
  "gradeCode": "LKG",
  "sectionCode": "SECTION_A",
  "rollNumber": "07",
  "enrollmentDate": "2026-04-05",
  "status": 1,
  "studentAdmissionNo": "ADM2026001",
  "studentFirstName": "Ankit",
  "studentLastName": "Chamlagain"
}
```

Grade/section/year are flattened in — the enrollments table can show "LKG / Section A" without extra lookups (resolve labels from cached dropdown data).

### List filters

```
GET /api/enrollments?page=1&pageSize=20&studentId=…&academicClassId=…&classSectionId=…
```

- `academicClassId` = whole grade (all sections); `classSectionId` = one section. All filters optional and combinable.

### Update / status changes

`PUT /api/enrollments/{id}` body unchanged (`{ "rollNumber", "enrollmentDate", "status" }`), with one new rule: **flipping a closed enrollment back to `status: 1` (Enrolled) re-runs the one-active-per-year check** and fails `409` if the student is active elsewhere that year.

**Moving a student between sections/grades (UI flow):** `PUT` the old enrollment to `status: 2` (Transferred) → `POST` a new enrollment into the target section. Never delete the old row — history should survive.

## 4. Student onboarding with guardians — `/api/students` (extended, backward-compatible)

### Create

```
POST /api/students
{
  "admissionNo": "ADM2026002",
  "firstName": "Sita",
  "lastName": "Sharma",
  "gender": 1,
  "dateOfBirth": "2019-06-10",
  "admissionDate": "2026-04-01",
  "guardians": [
    { "guardianId": "e91f…", "relationshipCode": "FATHER", "isPrimary": true },
    { "firstName": "Maya", "lastName": "Sharma", "phone": "+9779841000000",
      "relationshipCode": "MOTHER", "isPrimary": false }
  ]
}
```

- Each `guardians` entry is **either** an existing guardian (`guardianId` — use for siblings sharing a parent record) **or** a new one (leave `guardianId` out and send `firstName`/`lastName` + optional `email`/`phone`/`occupation`/`address` — same fields as `POST /api/guardians`).
- `relationshipCode` required per entry (catalog `1004`); at most **one** entry may have `isPrimary: true` (`400` otherwise).
- All-or-nothing: one bad entry (unknown guardianId, bad relationship code, duplicate guardianId) fails the whole request; no partial student is saved.
- `guardians` is optional — omitting it behaves exactly like the old endpoint, and `POST /api/students/{id}/guardians` still works for linking later.

### Student profile now includes guardians

`GET /api/students/{id}` (and the create response) return:

```json
{
  "id": "…", "admissionNo": "ADM2026002", "firstName": "Sita", …,
  "guardians": [
    { "id": "<linkId>", "studentId": "…", "guardianId": "…",
      "relationshipCode": "FATHER", "isPrimary": true,
      "guardianFirstName": "Ram", "guardianLastName": "Sharma",
      "guardianPhone": "+977984…", "guardianEmail": "ram@example.com" }
  ]
}
```

The paged list (`GET /api/students`) keeps `guardians` empty for performance — use the detail call for the profile screen. `id` on each entry is the **link id** used by `DELETE /api/students/{id}/guardians/{linkId}`.

## 5. Teacher assignments — `/api/teachers/{id}/assignments` (extended)

### Create

```
POST /api/teachers/{id}/assignments
{
  "classSubjectId": "…",
  "classSectionId": "…",      // optional: omit/null = teaches this subject to ALL sections
  "isClassTeacher": false
}
```

Rules:

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown teacher, classSubject, or classSection |
| `400 VALIDATION_ERROR` | `classSectionId` belongs to a different class than the subject |
| `400 VALIDATION_ERROR` | `isClassTeacher: true` without a `classSectionId` — a class teacher is for exactly one section |
| `409 CONFLICT` | Same teacher + subject + section (or same teacher + subject with no section) already assigned |
| `409 CONFLICT` | **That section** already has a class teacher (any subject) |

The same teacher can now hold e.g. "Math → Section A" and "Math → Section B" as two assignments, or one section-less "Math → all sections" row.

### Response shape — `TeacherAssignmentDto` (extended)

```json
{
  "id": "…", "teacherId": "…", "classSubjectId": "…", "academicClassId": "…",
  "subjectCode": "MATH",
  "classSectionId": null,        // null = all sections
  "sectionCode": null,
  "isClassTeacher": false
}
```

Related: `DELETE /api/academicclasses/{id}/sections/{classSectionId}` now also fails `409` while teacher assignments still target the section.

---

## 6. New-year setup in one call — clone structure

```
POST /api/academicyears/{targetYearId}/clone-structure
{ "sourceAcademicYearId": "…" }
```

Copies every class of the source year into the target year: the class (grade), its sections (codes + capacities), and its subject mappings (mandatory/optional, display order, section scoping remapped onto the cloned sections). Everything is created `Active`. **Enrollments and teacher assignments are NOT copied** — those are per-year facts entered fresh.

- Additive and re-runnable: a grade that already exists in the target year is **skipped** (reported in `skippedGradeCodes`), never merged or overwritten.
- All-or-nothing per call: one save at the end.
- `400` if source == target; `404` for an unknown year on either side.

Response:

```json
{
  "sourceAcademicYearId": "…", "targetAcademicYearId": "…",
  "classesCreated": 15, "sectionsCreated": 32, "subjectsCreated": 148,
  "skippedGradeCodes": ["LKG"]
}
```

Typical flow: create the new academic year → click "Copy structure from last year" → adjust the deltas (add a section, drop a subject). This is why per-year subject rows are kept: each year stays a historically accurate record (curricula, teachers, electives are all per-year facts), and the clone removes the re-entry cost. The row volume itself is trivial (~150 subject rows per year).

## New permissions (seeded to SuperAdmin; grant to other roles via `POST /api/roles/claims`)

| Code | Endpoint |
|---|---|
| `CLASS_SECTION_ADD` | `POST /api/academicclasses/{id}/sections` |
| `CLASS_SECTION_LIST` | `GET /api/academicclasses/{id}/sections` |
| `CLASS_SECTION_UPDATE` | `PUT /api/academicclasses/{id}/sections/{classSectionId}` |
| `CLASS_SECTION_REMOVE` | `DELETE /api/academicclasses/{id}/sections/{classSectionId}` |
| `YEAR_CLONE_STRUCTURE` | `POST /api/academicyears/{id}/clone-structure` |

Existing `CLASS_*`, `ENROLLMENT_*`, `STUDENT_*` permissions cover everything else (guardian onboarding rides on `STUDENT_CREATE`).

## UI migration checklist

- [ ] Classes screen: one row per grade; expand/inline-list its sections; "New Class" dialog = grade + repeatable section rows; add "Add Section" on the class row.
- [ ] Move capacity edit from the class edit form to the section edit form.
- [ ] Subjects dialog: open from the class (grade) row; title like "Subjects — LKG (all sections)".
- [ ] Enrollment form: replace the class picker with class → section cascading pickers; send `classSectionId`.
- [ ] Enrollment table: show flattened `gradeCode`/`sectionCode`; handle the new `409` "already has an active enrollment in this academic year" message.
- [ ] Student admission wizard: add the guardians step (pick existing / create inline, one primary); drop the separate link-guardian calls from the wizard.
- [ ] Student profile: render `guardians` from the detail response (no separate `GET …/guardians` call needed there anymore, though that endpoint still exists).
- [ ] Teacher assignment form: add an optional section picker (populated from the chosen class's `sections`); require it when "Class teacher" is toggled on; show `sectionCode` (or "All sections") in the assignments table.
- [ ] Subjects dialog: optional subjects get an optional section selector; show a section chip on scoped rows; the elective picker switches to `GET …/subjects?classSectionId=…`.
- [ ] Academic year screen: add a "Copy structure from…" action (year picker → clone call → show created/skipped counts).

## Scaling note (Nursery → Masters)

Grades are Config catalog options (typeCode `1001`), so higher levels are just new codes (`BACHELOR_YEAR_1`, `MASTER_YEAR_2`, or per-semester codes like `BSC_SEM_1`). Use `additionalValue1` as a numeric sort order so grade dropdowns stay in level order across school + college ranges.
