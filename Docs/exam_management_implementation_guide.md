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
history if you need to reconcile against an older frontend build. The batch-scheduling workflow
(`PUT /api/exams/routine`) and the whole-vs-divided marks-configuration defaulting added in the
same round have their own focused companion guide:
`Docs/exam_routine_and_marks_configuration_implementation_guide.md`.

> ⚠️ **Migration status — read before touching this module.** `dbo.exams` and its dependents have
> been through several shapes:
>
> - `20260728171309_Added initial exam module.cs` — original shape (`class_section_id`, free-text
>   `room`, `name`, `weightage_percent`, `is_final_exam`; no `remarks`).
> - `20260729050427_Added update1 exam module.cs` / `20260729135723_Added update2 exam module.cs`
>   — the two follow-ups that drop the original columns, rename `description` → `remarks`, and
>   (update2) add `room_id` (FK → `exam_rooms`) plus create `exam_rooms`/`exam_hall_arrangements`/
>   `exam_hall_arrangement_classes`/`exam_seat_allocations` for the (now-removed) seating engine.
> - **`20260730061558_changes in exam module3 update.cs`** — drops `dbo.exam_rooms`,
>   `dbo.exam_hall_arrangements`, `dbo.exam_hall_arrangement_classes`, `dbo.exam_seat_allocations`,
>   and `dbo.exams.room_id` (FK + index), reflecting the Room/seat-arrangement removal. This
>   migration already exists in `Infrastructure/Migrations/`. Apply it with
>   `dotnet ef database update` if your database hasn't run it yet.
> - **Still pending, not yet written**: `Exam.InvigilatorEmployeeId`/`InvigilatorEmployee` were
>   also removed from the entity (same round, follow-up instruction) — `dbo.exams.remarks` is now
>   genuinely the last column, but `dbo.exams.invigilator_employee_id` (and its FK to `employees`)
>   is still in the schema per the migration above, unmapped by the current entity. A further
>   migration is needed: `ALTER TABLE dbo.exams DROP CONSTRAINT "FK_exams_employees_invigilator_employee_id"; DROP INDEX "IX_exams_invigilator_employee_id"; ALTER TABLE dbo.exams DROP COLUMN invigilator_employee_id;`
>   (exact constraint/index names may differ — check the actual schema). Every `SELECT`/`INSERT`
>   against `Exam` still works today regardless (EF just never reads/writes that column), so this
>   is a cleanup gap, not a runtime error, unlike the `remarks` gap the update1/update2 pair fixed.
> - **`20260730093356_changes in in exams for period code.cs`** — added
>   `dbo.exams.period_code varchar(100) NULL` for the original (string, Config-code) class period
>   timing cut. Already applied.
> - **Superseded the same day (2026-08-03)**: `Exam.PeriodCode` (string) was replaced by
>   `Exam.TimePeriodId` (`Guid?`, a real FK into the new `dbo.time_periods` table) — see
>   `Docs/time_period_and_class_routine_implementation_guide.md`. A further migration is needed:
>   drop `dbo.exams.period_code` (added by the migration above), add
>   `dbo.exams.time_period_id uuid NULL` with an FK to `time_periods.id`, plus create
>   `dbo.time_periods`/`dbo.class_time_periods` and make the matching change on
>   `dbo.teacher_assignments` (its own `period_code` column, added by
>   `20260803083606_update in setup for class assignment.cs`, is superseded the same way). **This
>   is a runtime error until applied** — `TimePeriodId` is a mapped property EF selects on every
>   read, so any `Exam`/`TeacherAssignment`/`SaveExamRoutine` query fails until the column exists.
>   See the new guide's migration section for the exact statements.

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
  "theoryMarks": 75,
  "practicalMarks": 25,
  "hasTheory": true,
  "hasPractical": true,
  "theoryPassMarks": 27,
  "practicalPassMarks": 10
}
```

**Two-tier composite mark structure (2026-07-30): `fullMarks`/`passMarks` are NOT request fields
anymore.** They're computed server-side and returned read-only on `ClassSubjectDto`:

| Metric | Sub-component | Formula |
|---|---|---|
| Full Marks | Theory (`theoryMarks`) | Admin-entered — max score in the written exam |
| Full Marks | Practical (`practicalMarks`) | Admin-entered — max score in lab/project/skill assessment |
| Full Marks | **Total (`fullMarks`)** | `theoryMarks + practicalMarks` — computed, never sent |
| Pass Marks | Theory (`theoryPassMarks`) | Admin-entered — minimum to pass the theory component |
| Pass Marks | Practical (`practicalPassMarks`) | Admin-entered — minimum to pass the practical component |
| Pass Marks | **Total (`passMarks`)** | `theoryPassMarks + practicalPassMarks` — computed, never sent |

This means the request above (Theory 75 + Practical 25) comes back with `fullMarks: 100` and
`passMarks: 37` on the response — computed, not something you also had to type in and keep in
sync. `theoryMarks`/`practicalMarks` are each component's own full marks; `theoryPassMarks`/
`practicalPassMarks` are each component's own pass threshold. Per the **independent component
passing rule**, a student must clear **each enabled component's own pass mark individually** —
passing the combined total alone is not sufficient if either component fails (already enforced at
marks-entry/result time, see sections 5–6; unaffected by this round).

- `hasTheory`/`hasPractical` say which components exist for this subject at all (default
  `hasTheory: true`, `hasPractical: false` — pure-theory is the common case). Marks entry (section
  5 below) rejects submitting a component the subject doesn't have.
- **Theory-only fallback**: when `hasPractical: false`, the backend automatically forces
  `practicalMarks`/`practicalPassMarks` to `0` (not `null`) — sending them yourself is rejected
  (see the validation table). `fullMarks` then reduces to exactly `theoryMarks` (`theoryMarks + 0`)
  and `passMarks` to exactly `theoryPassMarks` — the "theory-only fallback" rule. The mirror case
  (`hasTheory: false`, a practical-only subject) works the same way with the roles swapped.
- **Ungraded stays ungraded**: if the subject's marks scheme isn't configured yet, leave
  `theoryMarks`/`theoryPassMarks` (and, in divided mode, `practicalMarks`/`practicalPassMarks`)
  unset — `fullMarks`/`passMarks` come back `null`, not `0`, so "not graded yet" and "graded with
  zero marks" are never confused. Grading is still optional at exam-creation time (section 3).
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
| Both `hasTheory` and `hasPractical` enabled ⇒ `theoryMarks`/`practicalMarks` must be supplied together (both set, or both left unset) | "When both HasTheory and HasPractical are enabled, TheoryMarks and PracticalMarks must be provided together..." |

`passMarks` ≤ `fullMarks` is no longer a separate rule to violate — since both are now sums of the
same Theory/Practical pairs, `theoryPassMarks ≤ theoryMarks` and `practicalPassMarks ≤
practicalMarks` together already guarantee it. `ClassSubjectDto` (returned by every subject read)
carries `fullMarks`/`passMarks`/`theoryMarks`/`practicalMarks`/`theoryPassMarks`/
`practicalPassMarks`, all of the above.

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
  "examDate": "2026-08-10",
  "timePeriodId": "<Period 3 id>",
  "startTime": null,
  "endTime": null,
  "remarks": "Bring your own calculator"
}
```

`startTime`/`endTime` are `HH:mm:ss` (`TimeSpan?`). There is no room and no invigilator concept at
all in this module (both removed 2026-07-30) — an `Exam` is just subject + date/time + remarks.

**Class period timing (2026-07-30, moved off a Config catalog onto a real `TimePeriod` table
2026-08-03 — see `Docs/time_period_and_class_routine_implementation_guide.md`)**: send **either**
`timePeriodId` (a `TimePeriod` id — must not be a `Break`-kind row, and must be mapped to this
exam's class via `POST /api/timeperiods/map` first; list a class's mapped periods via
`GET /api/timeperiods/map/{academicClassId}`) and leave `startTime`/`endTime` `null` — the backend
resolves the concrete times from the period's own start/end — **or** send `startTime`/`endTime`
directly and leave `timePeriodId` `null`. Exactly one of the two is required; sending both is fine
too (the period wins, raw times are ignored) but there's no reason to. The response's `ExamDto`
always carries concrete `startTime`/`endTime` either way, plus `timePeriodId`/`timePeriodName`
(both `null` if raw times were used) — `timePeriodName` is resolved server-side, no second lookup
needed.

**Creating, updating, or deleting an exam automatically creates/updates/removes a linked
`CalendarEvent`** (`eventType: 5` / `CalendarEventType.Exam`) so it shows up on the existing
`GET /api/calendar/month-view`/`GET /api/calendar/events` with no separate call.

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown `examTermId` or `classSubjectId` |
| `400 VALIDATION_ERROR` | Neither `timePeriodId` nor both `startTime`/`endTime` given; `timePeriodId` not a known period, a `Break`-kind row, or not mapped to this class; the resolved `endTime` not after `startTime` |
| `409 CONFLICT` | An exam already exists for this subject within this term |

### Batch-schedule a whole class's routine in one call — `PUT /api/exams/routine`

The batch-scheduling workflow: build a whole class's timetable for one exam term and save it in
one atomic, idempotent call, instead of one `POST`/`PUT /api/exams` per subject. Full details
(sync semantics, term-boundary/overlap validation, response shape) live in the companion guide,
`Docs/exam_routine_and_marks_configuration_implementation_guide.md` (section 1) — summary:

```
PUT /api/exams/routine
{
  "examTermId": "…",
  "academicClassId": "…",
  "items": [
    { "classSubjectId": "…", "examDate": "2026-08-10", "timePeriodId": "<Period 1 id>", "startTime": null, "endTime": null, "remarks": null },
    { "classSubjectId": "…", "examDate": "2026-08-11", "timePeriodId": null, "startTime": "10:00:00", "endTime": "12:00:00" }
  ]
}
```

Each item independently picks `timePeriodId` or raw `startTime`/`endTime` — same either/or rule as
`POST /api/exams` above, validated per item.

`items` is the **complete** set of exams this class/term should have after saving — a subject
already scheduled gets updated in place, a new subject gets created, and a previously-scheduled
subject missing from `items` gets removed (unless it already has recorded marks, which fails the
whole save instead of silently discarding them). The whole request is validated as one atomic unit
— any term-boundary violation, time overlap, or blocked removal fails the entire call, nothing is
partially saved.

### Full endpoint table

| Method/Route | Body | Notes |
|---|---|---|
| `POST /api/exams` | see above | |
| `PUT /api/exams/routine` | see above | Whole-class batch save (idempotent sync) |
| `GET /api/exams?examTermId=…&classSubjectId=…&teacherId=…` | | All three filters optional/combinable; ordered by date then start time. `teacherId` narrows the list to exams for subjects that teacher is actually assigned (`TeacherAssignment.ClassSubjectId` match — an assignment scoped to one section still counts, since grading now happens at the whole-exam level) — the "which exams do I need to grade" worklist |
| `GET /api/exams/{id}` | | |
| `PUT /api/exams/{id}` | `{ examDate, timePeriodId, startTime, endTime, remarks }` | `examTermId`/`classSubjectId` immutable — re-targeting means creating a new exam; the linked calendar event is updated in place |
| `DELETE /api/exams/{id}` | | Hard; `409` while it still has recorded marks (delete those first); also removes the linked calendar event |
| `POST /api/exams/{id}/lock` | | Closes the marks-entry window |
| `POST /api/exams/{id}/unlock` | | Reopens it |

`ExamDto`: `id`, `examTermId`, `classSubjectId`, `subjectCode`, `gradeCode` (from the linked
subject's class — there is no `sectionCode`/`classSectionId` anymore), `examDate`,
`timePeriodId`/`timePeriodName`, `startTime`, `endTime`, `remarks`, `marksLocked`, and —
**read-only, sourced from the linked
`ClassSubject`, never settable here** — `fullMarks`, `passMarks`, `hasTheory`, `hasPractical`,
`theoryMarks`, `practicalMarks`, `theoryPassMarks`, `practicalPassMarks`.

While `marksLocked: true`, every marks-entry endpoint (section 5) rejects for that exam, and result
generation (section 6) requires every exam contributing to a student's result to be locked before
it will compute that student.

**There is no `POST /api/exams/for-class` anymore.** The earlier (Round 2) "leave Section on 'All
sections'" convenience existed only because an exam used to be scoped to one section; now that an
exam already always covers the whole grade by construction, that endpoint has nothing left to do.
The new `PUT /api/exams/routine` above answers a different question — "schedule every subject of
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
| `GET /api/studentexammarks?examId=…&enrollmentId=…&classSectionId=…` | | All three filters optional/combinable. `classSectionId` (2026-07-30) narrows to marks whose enrollment belongs to that section — since an `Exam` always covers the whole grade, this is what keeps a teacher-wise list scoped to just the section that teacher actually teaches (see section 5's roster note below for the full "same subject, different teachers per section" rationale) |
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
GET /api/studentexammarks/roster?examId=…&search=aarav&classSectionId=…
```

`search` is optional — omit it for the full eligible list; when given, it matches (case-
insensitive, substring) against the student's name **or** admission number. Returns one row per
enrollment eligible for the exam's subject (whole grade, electors only, or one section's electors
— per section 3's resolution rule), ordered by roll number.

**`classSectionId` (2026-07-30, optional)**: since an `Exam` always covers the whole grade, the
roster for a grade-wide subject would otherwise include *every* section's students — wrong when
**the same subject is taught by different teachers in different sections**. Pass the calling
teacher's own section for this subject to scope the roster down to just their students. There's no
"who is the logged-in teacher" resolution in this codebase yet (same gap `GET /api/exams?teacherId=`
already documents) — resolve it client-side: call `GET /api/teachers/{teacherId}` and read
`serviceHistory` for this `classSubjectId`'s `ClassSectionId` (`null` in the response means that
particular assignment already covers every section, so omit `classSectionId` here too), then pass
it through. Omitting `classSectionId` entirely (the admin case) keeps the full whole-grade roster.

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

### Admin, student-wise marks entry — `GET /api/studentexammarks/student/{enrollmentId}`

The counterpart to the roster above: instead of "one subject, every student," this is "one
student, every subject" — pick a student, see and enter every one of their subjects' marks for a
term in a single screen. This is the intended **admin** flow (an office/admin user reviewing or
completing one student's whole term); teachers still use the roster above, scoped to their own
subject/section.

```
GET /api/studentexammarks/student/{enrollmentId}?examTermId=…
```

Returns one row per `Exam` within `examTermId` the enrollment is actually eligible for (resolved
the same way result generation resolves it — mandatory class-wide subjects, electives the student
picked, and section-scoped subjects only when they match the student's own section), each row
carrying the same read-only grading fields `ExamDto` exposes plus the student's existing mark (or
`null`):

```json
{
  "responseCode": "SUCCESS",
  "data": [
    {
      "examId": "…",
      "classSubjectId": "…",
      "subjectCode": "MATH",
      "examDate": "2026-08-03T00:00:00",
      "marksLocked": false,
      "fullMarks": 100,
      "passMarks": 35,
      "hasTheory": true,
      "hasPractical": false,
      "theoryMarks": 100,
      "practicalMarks": null,
      "theoryPassMarks": 35,
      "practicalPassMarks": null,
      "mark": null
    },
    {
      "examId": "…",
      "classSubjectId": "…",
      "subjectCode": "SCIENCE",
      "examDate": "2026-08-05T00:00:00",
      "marksLocked": false,
      "fullMarks": 100,
      "passMarks": 37,
      "hasTheory": true,
      "hasPractical": true,
      "theoryMarks": 75,
      "practicalMarks": 25,
      "theoryPassMarks": 27,
      "practicalPassMarks": 10,
      "mark": { "id": "…", "examId": "…", "totalMarks": 82, "grade": null, "...": "..." }
    }
  ]
}
```

Same "`mark: null` → `POST`, `mark` non-null → `PUT /api/studentexammarks/{mark.id}`" rule as the
roster — this is also a **read-only worklist**, not a new submission path. Ordered by `subjectCode`.

| HTTP / code | Cause |
|---|---|
| `404 NOT_FOUND` | Unknown `enrollmentId` or `examTermId` |

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
| `EXAM_LIST` (visible) + `EXAM_CREATE`/`_DETAIL`/`_UPDATE`/`_DELETE`/`_LOCK`/`_UNLOCK`/`_SAVE_ROUTINE` | `Exams` controller |
| `GRADE_SCALE_LIST` (visible) + `GRADE_SCALE_CREATE`/`_DETAIL`/`_UPDATE`/`_DELETE` | `GradeScales` controller |
| `STUDENT_EXAM_MARK_LIST` (visible) + `STUDENT_EXAM_MARK_CREATE`/`_DETAIL`/`_UPDATE`/`_DELETE`/`_BULK_UPSERT`/`_ROSTER`/`_BY_STUDENT` | `StudentExamMarks` controller |
| `EXAM_RESULT_LIST` (visible) + `EXAM_RESULT_GENERATE`/`_PUBLISH`/`_DETAIL`/`_WITHHOLD`/`_LIFT_WITHHOLD` | `ExamResults` controller |
| `STUDENT_PROMOTION_LIST` (visible) + `STUDENT_PROMOTION_CREATE`/`_DETAIL`/`_BULK_PROCESS` | `StudentPromotions` controller |

`EXAM_CREATE_FOR_CLASS` (from an earlier version of this guide) **no longer exists** — the
endpoint it gated was removed (section 3). `EXAM_ROOM_LIST`/`EXAM_HALL_ARRANGEMENT_LIST` and their
children **also no longer exist** (2026-07-30) — the whole room/seat-arrangement subsystem was
removed; those codes are retired via `MenuSeeder.BuildRetiredMenuCodes`, so any role that had them
granted loses them on next boot. **`EXAM_CREATE_ROUTINE` was retired the same day too** (the
routine endpoint's first, create-only cut) — replaced by `EXAM_SAVE_ROUTINE` above.

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
4. **2026-07-30, first pass**: the Exam Rooms + Hall Arrangement/Seat Allocation engine from step 3
   is **removed entirely**, per instruction — `Exam.RoomId`/`Room` are gone. New:
   `POST /api/exams/routine` (create-only, skip-list style — schedule every subject of one class in
   one call, a different convenience than the removed `for-class`, which cloned one subject across
   sections rather than covering every subject of a class) and whole-vs-divided
   `TheoryMarks`/`TheoryPassMarks` defaulting on `ClassSubject` (section 1).
5. **2026-07-30, same day, follow-up**: `Exam.InvigilatorEmployeeId`/`InvigilatorEmployee` are also
   removed, per instruction — an `Exam` is now just subject + date/time + remarks, nothing else.
   `CreateExamCommand`/`UpdateExamCommand`/`ExamRoutineItemInput`/`ExamDto` all lost the field.
6. **2026-07-30, same day, redesigned per a formal batch-scheduling spec**: the routine
   endpoint from step 4 is redesigned from create-only/skip-list into a full idempotent sync —
   `PUT /api/exams/routine` (`SaveExamRoutineCommand`/`SaveExamRoutineAsync`) now creates, updates,
   and removes exams for a `(examTerm, class)` pair in one atomic call, validated for term-boundary
   and time-overlap conflicts up front (Room Double-Booking/Invigilator Collision from the spec
   were explicitly declined — see the companion guide). `EXAM_CREATE_ROUTINE` is retired, replaced
   by `EXAM_SAVE_ROUTINE`. See
   `Docs/exam_routine_and_marks_configuration_implementation_guide.md` for the full sync/validation
   contract.
7. **Current (2026-07-30, same day, formalized two-tier composite mark structure)**: `fullMarks`/
   `passMarks` on `AssignClassSubjectCommand`/`UpdateClassSubjectCommand` (section 1) are
   **removed as inputs** — they're now always computed server-side as
   `theoryMarks + practicalMarks` / `theoryPassMarks + practicalPassMarks`
   (`AcademicClassService.ResolveCompositeMarks`, replacing the narrower `ResolveTheoryDefaults`
   from step 4), which is what "optimizing" the whole-vs-divided flow meant here: the two totals
   can no longer drift from their own component figures, and the disabled component in theory-only
   (or practical-only) mode is forced to `0` rather than left `null`. `passMarks <= fullMarks` is
   now guaranteed by construction instead of separately validated.
8. **2026-07-30, same day: section-aware marks entry + class period timing + admin
   student-wise marks entry**. `GET /api/studentexammarks`/`GET /api/studentexammarks/roster`
   gained `classSectionId` (a section-taught teacher's marks list/roster now stays scoped to their
   own section instead of the whole grade). `POST`/`PUT /api/exams` and
   `PUT /api/exams/routine`'s items gained `periodCode` as an alternative to raw
   `startTime`/`endTime`, originally resolved from a Config catalog. New
   `GET /api/studentexammarks/student/{enrollmentId}` — admin, student-wise marks entry.
9. **Current (2026-08-03, same day, superseding step 8's period field): class period timing moved
   off the Config catalog onto a real `TimePeriod` table** — `periodCode` (string) became
   `timePeriodId` (`Guid?`, a real FK), now also shared with `TeacherAssignment.TimePeriodId`, and
   a picked period must be mapped to the exam's own class via the new `ClassTimePeriod` table
   (`POST /api/timeperiods/map`) — a check the Config-based cut had no way to express. See
   `Docs/time_period_and_class_routine_implementation_guide.md` for the full reference and
   `Docs/exam_routine_and_marks_configuration_implementation_guide.md` for the marks-entry/period
   detail together.

If you're reconciling an older frontend build against this guide, anywhere it sends
`roomId`/`invigilatorEmployeeId`/`classSectionId`/`name`/`weightagePercent`/`isFinalExam` on an
exam create/update, calls `/api/exams/for-class`, `POST /api/exams/routine` (now `PUT`, and now a
sync rather than create-only), any `/api/examrooms`/`/api/examhallarrangements` endpoint, sends
`fullMarks`/`passMarks` on a subject assign/update call, or sends raw `startTime`/`endTime` as
non-nullable on an exam create/update (they're `TimeSpan?` now), it needs updating to match this
version.

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
