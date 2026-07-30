# CMSApp — Exam Management: Assessment Configuration, Exams, Marks, Results & Promotion (UI)

**What this is**: the full Exam/Result/Promotion design in
`Docs/Student_Management_System_Exam_Result_Promotion_Design.md`, implemented across three same-day
passes, **redesigned once** (merging `Exam`+`ExamSchedule` into one resource), **redesigned a
second time** from `Docs/Exam_Module_Design_Revised.md` (an `Exam` no longer pins a `ClassSection`,
`Name`, `WeightagePercent`, or `IsFinalExam` at all), and then had its optional examination
hall/seat-allocation engine (rooms, hall arrangements, automatic seating, exam roll numbers,
attendance) **removed entirely** (2026-07-30, per instruction — the module only needs simple
subject/date/time scheduling). This guide supersedes the previous version of itself — every section
below reflects the **current** backend. See "What changed, and when" near the end for the full
history if you need to reconcile against an older frontend build. The "set the whole routine at
once" batch-schedule endpoint and the whole-vs-divided marks-configuration defaulting added in the
same round have their own focused companion guide:
`Docs/exam_routine_and_marks_configuration_implementation_guide.md`.

> ⚠️ **Migration status — read before touching this module.** `dbo.exams` and its dependents have
> been through several shapes; the exact statements a given database needs depend on which
> migrations it has actually run:
>
> - `20260728171309_Added initial exam module.cs` — original shape (`class_section_id`, free-text
>   `room`, `name`, `weightage_percent`, `is_final_exam`; no `remarks`).
> - `20260729050427_Added update1 exam module.cs` / `20260729135723_Added update2 exam module.cs`
>   — the two follow-ups that drop the original columns, rename `description` → `remarks`, and
>   (update2) add `room_id` (FK → `exam_rooms`) plus create `exam_rooms`/`exam_hall_arrangements`/
>   `exam_hall_arrangement_classes`/`exam_seat_allocations` for the (now-removed) seating engine.
> - **A further migration is needed on top of whichever of the above has run** to reflect this
>   round's removal: drop `dbo.exam_rooms`, `dbo.exam_hall_arrangements`,
>   `dbo.exam_hall_arrangement_classes`, `dbo.exam_seat_allocations`, and `dbo.exams.room_id` (with
>   its FK/index) if update2 was applied; if only the initial migration (or update1) has run, those
>   tables/column never existed and there's nothing to drop for this round specifically — just the
>   pre-existing gap those two files already cover. **Check the actual schema before writing the
>   drop migration** rather than assuming a specific starting point.

---

## 1. Assessment configuration — extends the existing subject endpoints

**Configure this first, for every subject, before creating any exam for it.** No new endpoint.
`POST /api/academicclasses/{id}/subjects` (assign) and
`PUT /api/academicclasses/{id}/subjects/{classSubjectId}` (update) carry the full grading scheme —
this is the **only** place Full Marks/Pass Marks are entered anywhere in the exam module:

```json
{
  "subjectCode": "SCIENCE",
  "isMandatory": true,
  "displayOrder": 3,
  "creditHours": 4.0,
  "fullMarks": 100,
  "passMarks": 37,
  "theoryMarks": 75,
  "practicalMarks": 25,
  "hasTheory": true,
  "hasPractical": true,
  "theoryPassMarks": 27,
  "practicalPassMarks": 10
}
```

- `fullMarks`/`passMarks` are the subject's **overall** totals — this is what every exam for the
  subject is graded out of, and what marks-entry validation and result generation both read.
- `theoryMarks`/`practicalMarks` are each component's own full marks; `theoryPassMarks`/
  `practicalPassMarks` are each component's own pass threshold. A student must clear the overall
  `passMarks` **and** each enabled component's own pass mark.
- `hasTheory`/`hasPractical` say which components exist for this subject at all (default
  `hasTheory: true`, `hasPractical: false` — pure-theory is the common case). Marks entry (section
  5 below) rejects submitting a component the subject doesn't have.
- **Grade-wise and section-wise mapping**: `ClassSubject` is already keyed by `AcademicClassId`
  (the grade) with an optional `ClassSectionId` override — a class-wide row applies to every
  section of that grade; a section-scoped row (same subject code, non-null `ClassSectionId`)
  overrides it for just that section, and **who actually sits the exam** is resolved from this
  same mechanism at read/generation time (section 3 below) — the `Exam` itself never repeats it.

### Validation (both create and update)

| Rule | Message |
|---|---|
| At least one of `hasTheory`/`hasPractical` must be `true` | "At least one of HasTheory/HasPractical must be enabled." |
| `hasTheory: false` ⇒ `theoryMarks`/`theoryPassMarks` must be omitted | "TheoryMarks/TheoryPassMarks cannot be set when HasTheory is false." |
| `hasPractical: false` ⇒ `practicalMarks`/`practicalPassMarks` must be omitted | "PracticalMarks/PracticalPassMarks cannot be set when HasPractical is false." |
| `theoryPassMarks` ≤ `theoryMarks` (when both given) | "TheoryPassMarks cannot exceed TheoryMarks." |
| `practicalPassMarks` ≤ `practicalMarks` (when both given) | "PracticalPassMarks cannot exceed PracticalMarks." |
| `passMarks` ≤ `fullMarks`, `theoryMarks + practicalMarks == fullMarks` when all given | — |

`ClassSubjectDto` (returned by every subject read) carries all of the above.

---

## 2. Exam Terms — `/api/examterms`

A macro examination period within an `AcademicYear` (First Terminal, Mid Term, Final Exam).
Soft-deleted, `code` unique (case-sensitive, reserved even when soft-deleted).

### Create

```
POST /api/examterms
{
  "academicYearId": "…",
  "code": "TERM-1",
  "name": "First Terminal",
  "sequence": 1,
  "startDate": "2026-08-01",
  "endDate": "2026-08-10",
  "publishResult": false,
  "status": 1
}
```

`status` is `ExamTermStatus`: `1` Draft, `2` Active, `3` Completed, `4` Closed.

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown `academicYearId` |
| `409 CONFLICT` | `code` already in use (possibly by a soft-deleted term) |
| `400 VALIDATION_ERROR` | `endDate` before `startDate` |

### Full endpoint table

| Method/Route | Body | Notes |
|---|---|---|
| `POST /api/examterms` | see above | |
| `GET /api/examterms?academicYearId=…&page=1&pageSize=20` | | `academicYearId` optional filter, ordered by `sequence` |
| `GET /api/examterms/{id}` | | |
| `PUT /api/examterms/{id}` | `{ name, sequence, startDate, endDate, publishResult, status }` | `academicYearId`/`code` immutable |
| `DELETE /api/examterms/{id}` | | Soft; `409` while it still has exams |

`ExamTermDto`: `id`, `academicYearId`, `code`, `name`, `sequence`, `startDate`, `endDate`,
`publishResult`, `status`.

---

## 3. Exams — `/api/exams`

**An `Exam` is one subject's single graded sitting within one exam term — nothing more.** It is
keyed by `(ExamTermId, ClassSubjectId)` only: **no `ClassSectionId`, no `Name`, no
`WeightagePercent`, no `IsFinalExam`.** There is exactly one `Exam` row per subject per term —
creating a second one for the same `(examTermId, classSubjectId)` pair is a `409 CONFLICT`, full
stop; unlike the previous design, there is no way to schedule "Quiz 1" and "Written" as two
separate sittings for the same subject in the same term anymore. Hard-deleted (pure child of
`ExamTerm`).

**An exam always covers the whole grade — never a single section.** `ClassSubjectId` already
implies the grade (`ClassSubject.AcademicClassId`); *which enrollments actually sit it* is resolved
dynamically at read/marks/result time by `Application/Exams/EligibleEnrollmentResolver`, the same
logic `ClassSubject`'s existing mandatory/elective/section-scoping rules already drive everywhere
else in this codebase:

- A **mandatory, class-wide** subject (`IsMandatory: true`, `ClassSectionId: null`) → every
  enrolled student in the grade, across every section.
- An **optional** subject → only the students who elected it (`EnrollmentSubject`), regardless of
  section.
- A **section-scoped** subject (`ClassSectionId` set on the `ClassSubject` — always optional, per
  the existing class/section rules) → only that section's electors.

This is why creating/listing an exam never asks for a section: there's nothing to ask, it falls
out of the subject's own configuration.

**Full Marks/Pass Marks are never accepted here.** They are read from the linked `ClassSubject`
(section 1) at marks-entry and result-generation time. Creating an exam does **not** require the
subject's marks to already be configured — schedule first, grade later; a subject still missing
Full/Pass Marks at result-generation time produces a per-student skip (section 6), not a blocked
exam creation.

### Create

```
POST /api/exams
{
  "examTermId": "…",
  "classSubjectId": "…",
  "examDate": "2026-08-03",
  "startTime": "10:00:00",
  "endTime": "12:00:00",
  "invigilatorEmployeeId": "…",
  "remarks": "Bring your own calculator"
}
```

`startTime`/`endTime` are `HH:mm:ss` (TimeSpan). `invigilatorEmployeeId` is optional — the plain
"assign subject, date, time, invigilator" flow doesn't need anything else; there is no room concept
at all in this module (the room/seat-arrangement engine was removed 2026-07-30).

**Creating, updating, or deleting an exam automatically creates/updates/removes a linked
`CalendarEvent`** (`eventType: 5` / `CalendarEventType.Exam`) so it shows up on the existing
`GET /api/calendar/month-view`/`GET /api/calendar/events` with no separate call.

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown `examTermId`, `classSubjectId`, or `invigilatorEmployeeId` |
| `400 VALIDATION_ERROR` | `endTime` not after `startTime` |
| `409 CONFLICT` | An exam already exists for this subject within this term |

### Schedule a whole class's routine in one call — `POST /api/exams/routine`

The "set every subject's date/time for one class at once" flow, instead of one `POST /api/exams`
per subject. Full details, request/response shapes, and validation table live in the companion
guide, `Docs/exam_routine_and_marks_configuration_implementation_guide.md` (section 1) — summary:

```
POST /api/exams/routine
{
  "examTermId": "…",
  "academicClassId": "…",
  "items": [
    { "classSubjectId": "…", "examDate": "2026-08-03", "startTime": "10:00:00", "endTime": "12:00:00", "invigilatorEmployeeId": null, "remarks": null },
    { "classSubjectId": "…", "examDate": "2026-08-04", "startTime": "10:00:00", "endTime": "12:00:00" }
  ]
}
```

Skip-list style, same convention as `POST /api/studentexammarks/bulk` — a bad item (unknown
subject, subject already scheduled this term, unknown invigilator, duplicate subject in the same
request) is reported in the response's `skipped` array instead of failing the whole call.

### Full endpoint table

| Method/Route | Body | Notes |
|---|---|---|
| `POST /api/exams` | see above | |
| `POST /api/exams/routine` | see above | Whole-class batch create |
| `GET /api/exams?examTermId=…&classSubjectId=…&teacherId=…` | | All three filters optional/combinable; ordered by date then start time. `teacherId` narrows the list to exams for subjects that teacher is actually assigned (`TeacherAssignment.ClassSubjectId` match — an assignment scoped to one section still counts, since grading now happens at the whole-exam level) — the "which exams do I need to grade" worklist |
| `GET /api/exams/{id}` | | |
| `PUT /api/exams/{id}` | `{ examDate, startTime, endTime, invigilatorEmployeeId, remarks }` | `examTermId`/`classSubjectId` immutable — re-targeting means creating a new exam; the linked calendar event is updated in place |
| `DELETE /api/exams/{id}` | | Hard; `409` while it still has recorded marks (delete those first); also removes the linked calendar event |
| `POST /api/exams/{id}/lock` | | Closes the marks-entry window |
| `POST /api/exams/{id}/unlock` | | Reopens it |

`ExamDto`: `id`, `examTermId`, `classSubjectId`, `subjectCode`, `gradeCode` (from the linked
subject's class — there is no `sectionCode`/`classSectionId` anymore), `examDate`, `startTime`,
`endTime`, `invigilatorEmployeeId`, `invigilatorName`, `remarks`, `marksLocked`, and — **read-only,
sourced from the linked `ClassSubject`, never settable here** — `fullMarks`, `passMarks`,
`hasTheory`, `hasPractical`, `theoryMarks`, `practicalMarks`, `theoryPassMarks`,
`practicalPassMarks`.

While `marksLocked: true`, every marks-entry endpoint (section 5) rejects for that exam, and result
generation (section 6) requires every exam contributing to a student's result to be locked before
it will compute that student.

**There is no `POST /api/exams/for-class` anymore.** The earlier (Round 2) "leave Section on 'All
sections'" convenience existed only because an exam used to be scoped to one section; now that an
exam already always covers the whole grade by construction, that endpoint has nothing left to do.
The new `POST /api/exams/routine` above answers a different question — "schedule every subject of
one class," not "clone one subject across sections."

---

## 4. Grade Scales — `/api/gradescales`

The letter-grade schema (A+, A, B+, ... F) with percentage bands and grade-point equivalents.
Soft-deleted, `grade` unique. No FK ever points at a `GradeScale` row — result generation (section
6) **copies** the matched row's `grade`/`gradePoint` onto each `StudentExamMark`, so editing or
deleting a grade band later never retroactively changes an already-computed mark.

### Create

```
POST /api/gradescales
{ "grade": "A+", "minPercent": 90, "maxPercent": 100, "gradePoint": 4.0, "remarks": "Outstanding" }
```

| HTTP / code | Cause |
|---|---|
| `400 VALIDATION_ERROR` | `maxPercent < minPercent`, either percent outside `[0, 100]`, `grade` empty/over 5 chars |
| `409 CONFLICT` | `grade` already in use (possibly by a soft-deleted row) |

### Full endpoint table

| Method/Route | Body | Notes |
|---|---|---|
| `POST /api/gradescales` | see above | |
| `GET /api/gradescales` | | Unpaged, ordered by `minPercent` descending |
| `GET /api/gradescales/{id}` | | |
| `PUT /api/gradescales/{id}` | `{ minPercent, maxPercent, gradePoint, remarks }` | `grade` itself is immutable — delete and recreate for a rename |
| `DELETE /api/gradescales/{id}` | | Soft |

`GradeScaleDto`: `id`, `grade`, `minPercent`, `maxPercent`, `gradePoint`, `remarks`.

**Set this up before generating any results** — result generation looks up a grade band by
percentage; a percentage falling in a gap between configured bands gets `grade: null`/
`gradePoint: null` for that subject (not an error), so configure full 0–100 coverage.

---

## 5. Marks Entry — `/api/studentexammarks`

Records one student's theory/practical/internal marks (plus grace marks and absence flags) for one
`Exam`. Hard-deleted. Unique per `(examId, enrollmentId)`.

### Create (single student)

```
POST /api/studentexammarks
{
  "examId": "…",
  "enrollmentId": "…",
  "theoryObtainedMarks": 62,
  "practicalObtainedMarks": null,
  "internalMarks": 8,
  "theoryGraceMarks": 0,
  "practicalGraceMarks": 0,
  "theoryAbsent": false,
  "practicalAbsent": false,
  "remarks": null
}
```

`TotalMarks`/`IsAbsent`/`Grade`/`GradePoint` are never sent by the client. `Grade`/`GradePoint`
stay `null` until results are generated for the term (section 6); `TotalMarks`/`IsAbsent` are
computed immediately on every create/update. `enrollmentId` must be one of the exam's **eligible**
enrollments (section 3's resolution rule) — an enrollment that doesn't take the subject at all is
rejected, not silently accepted.

**Which fields are settable depends on the exam's subject's `hasTheory`/`hasPractical` flags**
(from `ExamDto`, section 3) — the UI should hide the inapplicable component's inputs entirely,
since submitting them is a validation error:

| Rule | Message |
|---|---|
| Subject has no theory (`hasTheory: false`) but theory fields sent | "This subject has no theory component; TheoryObtainedMarks/TheoryGraceMarks/TheoryAbsent cannot be set." |
| Subject has no practical (`hasPractical: false`) but practical fields sent | "This subject has no practical component; PracticalObtainedMarks/PracticalGraceMarks/PracticalAbsent cannot be set." |
| Subject has theory, `theoryAbsent: false`, but `theoryObtainedMarks` omitted | "TheoryObtainedMarks is required unless TheoryAbsent is true." |
| Subject has practical, `practicalAbsent: false`, but `practicalObtainedMarks` omitted | "PracticalObtainedMarks is required unless PracticalAbsent is true." |
| `theoryObtainedMarks` exceeds the subject's `theoryMarks` | "TheoryObtainedMarks cannot exceed the subject's TheoryMarks (N)." |
| `practicalObtainedMarks` exceeds the subject's `practicalMarks` | "PracticalObtainedMarks cannot exceed the subject's PracticalMarks (N)." |

**Total/absent computation** (`TotalMarks = Theory + Practical + Internal + Grace`, absent
components contribute zero): `isAbsent: true` only when *every* component the subject actually has
was marked absent.

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown `examId` or `enrollmentId` |
| `400 VALIDATION_ERROR` | Exam is locked, enrollment isn't eligible for the subject, or a component rule above |
| `409 CONFLICT` | Marks for this student on this exam already exist — use `PUT` instead |

### Full endpoint table

| Method/Route | Body | Notes |
|---|---|---|
| `POST /api/studentexammarks` | see above | |
| `GET /api/studentexammarks?examId=…&enrollmentId=…` | | Both filters optional/combinable |
| `GET /api/studentexammarks/{id}` | | |
| `PUT /api/studentexammarks/{id}` | same body minus `examId`/`enrollmentId` (immutable) | `400 VALIDATION_ERROR` if the exam is locked |
| `DELETE /api/studentexammarks/{id}` | | `400 VALIDATION_ERROR` while the exam is locked |
| `POST /api/studentexammarks/bulk` | see below | The realistic teacher UX |

`StudentExamMarkDto`: `id`, `examId`, `subjectCode`, `enrollmentId`, `studentName`, `admissionNo`,
`theoryObtainedMarks`, `practicalObtainedMarks`, `internalMarks`, `theoryGraceMarks`,
`practicalGraceMarks`, `theoryAbsent`, `practicalAbsent`, `totalMarks`, `grade`, `gradePoint`,
`remarks`, `isAbsent`, `isPublished`.

### Bulk upsert (whole roster, one call)

```
POST /api/studentexammarks/bulk
{
  "examId": "…",
  "marks": [
    { "enrollmentId": "…", "theoryObtainedMarks": 62, "internalMarks": 8, "theoryGraceMarks": 0, "practicalGraceMarks": 0, "theoryAbsent": false, "practicalAbsent": false },
    { "enrollmentId": "…", "theoryAbsent": true, "practicalAbsent": false, "theoryGraceMarks": 0, "practicalGraceMarks": 0 }
  ]
}
```

An existing `(examId, enrollmentId)` row is updated in place; a missing one is created. Rows that
fail per-line validation (including "not eligible for this subject") are **skipped, not
rejected** — the whole call still 200s with a per-line breakdown:

```json
{
  "responseCode": "SUCCESS",
  "data": {
    "examId": "…",
    "upsertedCount": 27,
    "skipped": [
      { "enrollmentId": "…", "reason": "This enrollment is not eligible for this exam's subject." }
    ]
  }
}
```

The whole call 400s only if the exam itself doesn't exist or is locked.

### Search-and-select a student to enter marks for — `GET /api/studentexammarks/roster`

The realistic teacher UX for a single-student entry screen: pick an exam (optionally narrowed to
"my exams" via `GET /api/exams?teacherId=…`, section 3), then search for one student among the
exam's eligible enrollments and enter/edit their marks — without a separate call to list eligible
students and a second one to check whether they're already marked.

```
GET /api/studentexammarks/roster?examId=…&search=aarav
```

`search` is optional — omit it for the full eligible list; when given, it matches (case-
insensitive, substring) against the student's name **or** admission number. Returns one row per
enrollment eligible for the exam's subject (whole grade, electors only, or one section's electors
— per section 3's resolution rule), ordered by roll number:

```json
{
  "responseCode": "SUCCESS",
  "data": [
    {
      "enrollmentId": "…",
      "studentId": "…",
      "admissionNo": "ADM2026101",
      "studentName": "Aarav Sharma",
      "rollNumber": "1",
      "mark": null
    },
    {
      "enrollmentId": "…",
      "studentId": "…",
      "admissionNo": "ADM2026102",
      "studentName": "Aisha Gurung",
      "rollNumber": "2",
      "mark": { "id": "…", "examId": "…", "totalMarks": 78, "grade": null, "...": "..." }
    }
  ]
}
```

`mark: null` means the student hasn't been marked yet for this exam — the UI renders empty entry
fields and calls `POST /api/studentexammarks` with that `enrollmentId` on save. `mark` non-null
means a row already exists — prefill the form from it and call `PUT /api/studentexammarks/{mark.id}`
on save instead. This is a **read-only worklist**, not a new way to submit marks.

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown `examId` |

---

## 6. Exam Results — `/api/examresults`

Aggregates a student's exams within one `ExamTerm` into one `StudentResult` row per enrollment —
percentage, GPA (credit-hour-weighted), pass/fail/compartment status, and section rank.

**Every `Exam` in the term counts — there is no "final exam only" filter anymore.** The previous
design's `IsFinalExam` flag (which used to let a mid-term quiz be marks-entry-only and skip
rolling into the term result) no longer exists on `Exam` at all: since there is now exactly one
`Exam` per subject per term (section 3), every exam necessarily contributes. If you want a
practice quiz that doesn't affect the term result, don't create it as an `Exam` at all — track it
outside this module, or wait for it as a future addition.

### Generate

```
POST /api/examresults/generate
{ "examTermId": "…", "academicClassId": null, "classSectionId": "…" }
```

`academicClassId`/`classSectionId` are both optional and combinable: pass a `classSectionId` to
scope which enrollments are considered (an exam whose subject is section-scoped and doesn't match
this section contributes nothing for that call), an `academicClassId` for one grade, or omit both
to cover every exam in the term. Because an `Exam` no longer names a section, **eligibility is
resolved per exam, per enrollment**, the same rule marks entry uses (section 3) — a student's
result is built from however many exams they're actually eligible for (as many as the subjects
they take), not from a fixed per-section exam list. **Idempotent/regenerate-safe** — calling it
again for the same scope recomputes each enrollment's `StudentResult` in place, except a row
already `Withheld`, which is always skipped.

**Ranking is per the enrollment's own section** (`Enrollment.ClassSectionId`) regardless of how
many/which exams contributed to a given student's result — every enrollment generated in *this*
call for the same section is ordered by percentage descending (ties broken by raw obtained marks)
and assigned `1, 2, 3, ...`. A partial-scope regenerate only re-ranks the enrollments included in
that call, not the whole section.

Per-enrollment requirements, each producing a skip (not a failure of the whole call) when unmet:

| Skip reason | Cause |
|---|---|
| "The exam for subject 'X' is not locked yet." | Lock every exam contributing to this student's result first (section 3's lock action) |
| "Full Marks/Pass Marks are not configured for subject 'X'." | Configure the subject first (section 1) |
| "Marks are missing for subject 'X'." | A `StudentExamMark` row doesn't exist yet for that student/subject |
| "Result is withheld; lift the hold before regenerating." | See withhold/lift below |

```json
{
  "responseCode": "SUCCESS",
  "data": {
    "examTermId": "…",
    "generatedCount": 42,
    "skipped": [
      { "enrollmentId": "…", "studentName": "Aarav Sharma", "reason": "Marks are missing for subject 'MATH'." }
    ]
  }
}
```

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown `examTermId` |
| `400 VALIDATION_ERROR` | No exams found for the given scope at all |

**Pass/Fail/Compartment**: a subject is failed if the student was absent, or fell short of either
enabled component's own pass mark (`theoryPassMarks`/`practicalPassMarks`) or the subject's own
overall `passMarks`. `resultStatus` is `Pass` (0 failed subjects), `Compartment` (1–2 failed
subjects — `Domain/Constants/ExamResultRules.CompartmentMaxFailedSubjects`, currently `2`), or
`Fail` (3+).

**GPA** is the credit-hour-weighted average of each subject's grade point (`ClassSubject.creditHours`,
falling back to a weight of `1` when unconfigured).

### Publish

```
POST /api/examresults/publish/{examTermId}
```

Stamps `publishedDate` on every non-withheld `StudentResult` for the term, flips
`ExamTerm.publishResult` to `true`, and marks every contributing `StudentExamMark.isPublished`
`true`. Withheld results are **not** published — lift the hold first.

### List / detail

```
GET /api/examresults?examTermId=…&classSectionId=…&enrollmentId=…&page=1&pageSize=20
GET /api/examresults/{id}
```

All three filters optional/combinable; ordered by rank (nulls last), then percentage descending.
`StudentResultDto` (list): `id`, `enrollmentId`, `studentName`, `admissionNo`, `gradeCode`,
`sectionCode`, `examTermId`, `totalMarks`, `obtainedMarks`, `percentage`, `gpa`, `rank`,
`resultStatus`, `publishedDate`, `remarks`. Detail (`StudentResultDetailDto`) adds `subjects`, one
row per contributing exam's subject: `classSubjectId`, `subjectCode`, `fullMarks`, `passMarks`,
`obtainedMarks`, `grade`, `gradePoint`, `isAbsent`, `passed`.

`resultStatus`: `1` Pass, `2` Fail, `3` Compartment, `4` Withheld.

### Withhold / lift

```
POST /api/examresults/{id}/withhold
{ "remarks": "Fee dues outstanding" }

POST /api/examresults/{id}/lift-withhold
```

Withholding flips `resultStatus` to `4` and records why. A withheld result is excluded from
`publish` and skipped by any later `generate` call. **Lifting removes the row entirely**
(soft-delete) rather than restoring previous figures — the next `generate` call recomputes it
fresh. `remarks` is required (max 500 chars) on withhold.

---

## 7. Student Promotions — `/api/studentpromotions`

An `Enrollment` is **never edited** to reflect grade progression — every promotion, retention, or
transfer creates a brand-new `Enrollment` (through the existing enrollment-creation validation) and
closes the old one, logging the transition in an immutable `StudentPromotion` audit row. **No
update or delete endpoint** — by design. This section is unaffected by the section-less `Exam`
redesign (it consumes `StudentResult`, not `Exam`, directly).

### Manual, single-student promotion/retention/transfer

```
POST /api/studentpromotions
{
  "fromEnrollmentId": "…",
  "toClassSectionId": "…",
  "rollNumber": null,
  "promotionDate": "2026-08-01",
  "promotionType": 1,
  "remarks": null
}
```

`promotionType`: `1` Promoted, `2` Retained, `3` Transferred — all three use the same shape; only
the resulting old-`Enrollment.status` differs (`Transferred` for type `3`, `Completed` otherwise).
**The destination `toClassSectionId` must already exist.**

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown `fromEnrollmentId` |
| `400 VALIDATION_ERROR` | Source enrollment isn't currently `Enrolled`, already processed once, destination section doesn't exist, or an underlying enrollment-creation check fails (capacity full, duplicate roll number, ...) — the failure message is forwarded verbatim |

### Bulk process (whole-section, end-of-year workflow)

```
POST /api/studentpromotions/bulk-process
{
  "examTermId": "…",
  "fromClassSectionId": "…",
  "promotedToClassSectionId": "…",
  "retainedToClassSectionId": "…",
  "promotionDate": "2026-08-01",
  "remarks": null
}
```

Every currently-`Enrolled` student in `fromClassSectionId` is evaluated against their
`StudentResult` for `examTermId` — `Pass` → `promotedToClassSectionId` (`Promoted`);
`Fail`/`Compartment` → `retainedToClassSectionId` (`Retained`); `Withheld` or no result at all →
skipped. Both destination sections must already exist.

```json
{
  "responseCode": "SUCCESS",
  "data": {
    "examTermId": "…",
    "fromClassSectionId": "…",
    "promotedCount": 34,
    "retainedCount": 3,
    "skipped": [
      { "enrollmentId": "…", "studentName": "Priya Thapa", "reason": "No exam result found for this exam term." }
    ]
  }
}
```

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown `examTermId`, `fromClassSectionId`, `promotedToClassSectionId`, or `retainedToClassSectionId` |

### List / detail

```
GET /api/studentpromotions?studentId=…&page=1&pageSize=20
GET /api/studentpromotions/{id}
```

`studentId` optional. `StudentPromotionDto`: `id`, `studentId`, `studentName`, `admissionNo`,
`fromEnrollmentId`, `fromGradeCode`, `fromSectionCode`, `toEnrollmentId`, `toGradeCode`,
`toSectionCode`, `promotionDate`, `promotionType`, `remarks`.

---

## New permissions (seeded to SuperAdmin; grant to other roles via `POST /api/roles/claims`)

One `EXAM_MANAGEMENT` main menu:

| Code | Endpoint |
|---|---|
| `EXAM_TERM_LIST` (visible) + `EXAM_TERM_CREATE`/`_DETAIL`/`_UPDATE`/`_DELETE` | `ExamTerms` controller |
| `EXAM_LIST` (visible) + `EXAM_CREATE`/`_DETAIL`/`_UPDATE`/`_DELETE`/`_LOCK`/`_UNLOCK`/`_CREATE_ROUTINE` | `Exams` controller |
| `GRADE_SCALE_LIST` (visible) + `GRADE_SCALE_CREATE`/`_DETAIL`/`_UPDATE`/`_DELETE` | `GradeScales` controller |
| `STUDENT_EXAM_MARK_LIST` (visible) + `STUDENT_EXAM_MARK_CREATE`/`_DETAIL`/`_UPDATE`/`_DELETE`/`_BULK_UPSERT`/`_ROSTER` | `StudentExamMarks` controller |
| `EXAM_RESULT_LIST` (visible) + `EXAM_RESULT_GENERATE`/`_PUBLISH`/`_DETAIL`/`_WITHHOLD`/`_LIFT_WITHHOLD` | `ExamResults` controller |
| `STUDENT_PROMOTION_LIST` (visible) + `STUDENT_PROMOTION_CREATE`/`_DETAIL`/`_BULK_PROCESS` | `StudentPromotions` controller |

`EXAM_CREATE_FOR_CLASS` (from an earlier version of this guide) **no longer exists** — the
endpoint it gated was removed (section 3). `EXAM_ROOM_LIST`/`EXAM_HALL_ARRANGEMENT_LIST` and their
children **also no longer exist** (2026-07-30) — the whole room/seat-arrangement subsystem was
removed; those codes are retired via `MenuSeeder.BuildRetiredMenuCodes`, so any role that had them
granted loses them on next boot.

---

## What changed, and when

1. **2026-07-28, first cut**: `Exam` (term-wide container: name/weightage/isFinal, no
   subject/section) + `ExamSchedule` (the actual per-subject-per-section sitting:
   date/time/room/invigilator/marks) as two tables.
2. **2026-07-28, same day, first redesign**: merged into one `Exam` table — one resource covering
   both, keyed by `(examTermId, classSectionId, classSubjectId, name)`, still with
   `Name`/`WeightagePercent`/`IsFinalExam`, still section-scoped, `Room` as a free-text string.
   `POST /api/exams/for-class` existed to schedule every section of a class at once. This is what
   the *previous version of this guide* documented.
3. **2026-07-29, second redesign (per `Docs/Exam_Module_Design_Revised.md`)**: `ClassSectionId`,
   `Name`, `WeightagePercent`, and `IsFinalExam` are all removed from `Exam` — it's now keyed by
   `(examTermId, classSubjectId)` only, always covers the whole grade, and "who sits it" is
   resolved dynamically via `EligibleEnrollmentResolver` (section 3). `Room` (string) became
   `RoomId` (a real FK to a new `ExamRoom` entity). `POST /api/exams/for-class` was removed
   (nothing left for it to do). A full Exam Rooms + Hall Arrangement/Seat Allocation engine was
   added. Every "final exam" reference in result generation (section 6) was dropped along with
   `IsFinalExam` — every exam now contributes.
4. **Current (2026-07-30)**: the Exam Rooms + Hall Arrangement/Seat Allocation engine from step 3
   is **removed entirely**, per instruction — `Exam.RoomId`/`Room` are gone (`InvigilatorEmployeeId`
   stays, it's a real `Employee`). New: `POST /api/exams/routine` (schedule every subject of one
   class in one call — a different convenience than the removed `for-class`, which cloned one
   subject across sections rather than covering every subject of a class) and whole-vs-divided
   `TheoryMarks`/`TheoryPassMarks` defaulting on `ClassSubject` (section 1). See
   `Docs/exam_routine_and_marks_configuration_implementation_guide.md` for both.

If you're reconciling an older frontend build against this guide, anywhere it sends
`roomId`/`classSectionId`/`name`/`weightagePercent`/`isFinalExam` on an exam create/update, calls
`/api/exams/for-class` or any `/api/examrooms`/`/api/examhallarrangements` endpoint, it needs
updating to match this version.

## Deviations from the source design docs (deliberate, not oversights)

- **`Exam.MaximumMarks`/`PassMarks` (as `Student_Management_System_Exam_Result_Promotion_Design.md`'s
  `ExamSchedule` table specified them) do not exist as stored columns.** They are read from
  `ClassSubject.FullMarks`/`PassMarks` at marks-entry and result-generation time instead. `ExamDto`
  still exposes `fullMarks`/`passMarks` for the UI's convenience, just as a read-only pass-through.
- **`CalendarEvent` was reused as-is, not redesigned** to match `Exam_Module_Design_Revised.md`'s
  sketch of one. This codebase already has a `CalendarEvent` (Dual Calendar module) with an
  incompatible, AD/BS-dual shape; `Exam` carries its own `CalendarEventId` (a plain scalar lineage
  column, no FK) pointing the other direction instead.
- **`Exam` is hard-deleted, `ExamTerm` is soft-deleted** — the usual "identity-bearing parent
  soft-deletes, pure child hard-deletes" convention.
- **No room/seat-arrangement concept at all** — the design doc's optional hall/seat-allocation
  engine (`ExamRoom`, hall arrangements, automatic seating, exam roll numbers, attendance) was
  built (2026-07-29) and then removed again the same week (2026-07-30), per instruction. If this
  is needed again later, treat it as new work rather than resurrecting the deleted code verbatim —
  revisit the design against whatever the actual requirement turns out to be at that point.
- **No `AcademicYear`/`ExamTerm` date-range cross-check**, **no attendance-integrated absence
  flags** on `StudentExamMark` (entered by hand), and **no parent/student portal notification on
  publish**.
- **`StudentPromotion` is a standalone repository/service**, and **promotion is not one atomic
  database transaction** (`PromotionService` calls `IEnrollmentService.CreateEnrollmentAsync`,
  which commits its own save, before writing the audit row in a second save) — unaffected by, and
  unchanged since before, the exam redesigns above.
