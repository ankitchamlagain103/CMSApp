# CMSApp — Exam Routine (Batch Scheduling), Marks Configuration & Marks Entry (UI)

**What this is**: additions to the existing Exam Management module (2026-07-30), companions to
the main reference, `Docs/exam_management_implementation_guide.md`:

1. **`PUT /api/exams/routine`** — the batch-scheduling workflow: build a whole class's exam
   timetable for one exam term in a grid and save it in a single atomic call, instead of one
   `POST`/`PUT /api/exams` per subject. **Redesigned same day**, from a create-only/skip-list
   endpoint into a full idempotent sync with term-boundary and overlap validation (see "What
   changed, same day" at the end of this section).
2. **Two-tier composite mark structure** on `ClassSubject` — Full Marks/Pass Marks are no longer
   entered directly at all; they're always computed server-side as the sum of Theory + Practical
   (`theoryMarks + practicalMarks` / `theoryPassMarks + practicalPassMarks`), so the two totals can
   never drift from the components that define them. **Redesigned same day** from an earlier cut
   that accepted `fullMarks`/`passMarks` as independent inputs cross-validated against the
   components (see section 2's "the optimization" for why that was replaced).
3. **Class period timing** — an `Exam`'s `startTime`/`endTime` can now be resolved from a picked
   class period (`PERIOD1`, `PERIOD2`, ...) instead of always being entered raw.
4. **Section-aware & student-wise marks entry** — the teacher-wise marks list/roster can now be
   scoped to one section (the same subject can be taught by different teachers in different
   sections), and a new endpoint gives admins a "one student, every subject" entry screen to
   complement the existing "one subject, every student" roster.

Same round removed the exam-hall Room/seat-arrangement subsystem entirely (`ExamRoom`,
`ExamHallArrangement`, `ExamHallArrangementClass`, `ExamSeatAllocation`) and, later the same day,
`Exam.InvigilatorEmployeeId` — see "Room and invigilator removed" at the end of this guide.
**Neither is coming back in this round**: the batch grid is subject + date/time only.

---

## 1. Batch scheduling workflow — `PUT /api/exams/routine`

**The problem this solves**: a school doesn't schedule "one subject's exam" — an administrator
selects a class and an exam term, sees every subject the class takes in a grid, fills in each
subject's date/start/end time, and saves the whole timetable in one action. This endpoint is
exactly that save.

### Request

```
PUT /api/exams/routine
{
  "examTermId": "…",
  "academicClassId": "…",
  "items": [
    { "classSubjectId": "…", "examDate": "2026-08-03", "startTime": "10:00:00", "endTime": "12:00:00", "remarks": null },
    { "classSubjectId": "…", "examDate": "2026-08-04", "startTime": "10:00:00", "endTime": "12:00:00", "remarks": "Bring calculator" },
    { "classSubjectId": "…", "examDate": "2026-08-05", "startTime": "10:00:00", "endTime": "12:00:00" }
  ]
}
```

- `examTermId`/`academicClassId` are shared by the whole call — one exam term, one grade.
- Each item carries its **own** `examDate`/`startTime`/`endTime`/`remarks` — different subjects sit
  on different days.
- **UI flow** (matches the spec's workflow exactly): (1) user picks an exam term + a class; (2) call
  `GET /api/academicclasses/{academicClassId}/subjects` to list every subject assigned to the class
  (see `Docs/student_management_implementation_guide.md`) and pre-fill the grid — for a subject
  that's already scheduled, look it up in `GET /api/exams?examTermId=…&classSubjectId=…` (or simply
  re-render from this endpoint's own previous response) and prefill its existing date/time/remarks;
  (3) the user edits date/start/end time (and remarks) per row; (4) submit the **complete** grid —
  every subject currently assigned to the class, not just the ones being changed — as `items` here.

### Idempotent sync semantics — read this before wiring the save button

`items` is the **complete, authoritative set** of exams for this `(examTermId, academicClassId)`
pair after the save. On each call, the backend loads whatever `Exam` rows already exist for this
term/class and reconciles them against `items`:

| Item vs. existing state | Result |
|---|---|
| `classSubjectId` in `items`, no existing exam for it | **Created** — same shape as `POST /api/exams` (auto-creates a linked `CalendarEvent`) |
| `classSubjectId` in `items`, an exam already exists for it | **Updated in place** — `examDate`/`startTime`/`endTime`/`remarks` overwritten; the linked `CalendarEvent` is updated to match; the exam's `id` and any already-recorded marks are untouched |
| An existing exam's `classSubjectId` is **not** in `items` | **Removed** — hard-deleted along with its `CalendarEvent`, **unless it already has recorded marks**, in which case the entire save is rejected instead (see the removal rule below) |

Re-submitting the exact same `items` twice in a row is a no-op the second time (every subject
matches an existing exam with identical values) — this is what "idempotent" means here; it does
**not** mean the response is byte-identical between the first and second call (the first call
reports `createdCount`, the second reports the same subjects as `updatedCount` since they now
already exist).

### Validation — the complete timetable is checked before anything is saved

Structural checks (FluentValidation, standard `400 VALIDATION_ERROR` with the usual joined
messages):

| Rule | Message |
|---|---|
| `examTermId` empty | "'Exam Term Id' must not be empty." |
| `academicClassId` empty | "'Academic Class Id' must not be empty." |
| `items` empty | "'Items' must not be empty." |
| Any item's `classSubjectId` empty | "'Class Subject Id' must not be empty." |
| Any item's `endTime` not after its `startTime` (Temporal Sequence) | "EndTime must be after StartTime." |
| Any item's `remarks` over 500 chars | length error |

Top-level `404 NOT_FOUND` only for an unknown `examTermId` or `academicClassId`.

**Everything past this point is validated as one batch, and any single issue fails the whole
request** (`400 VALIDATION_ERROR`, every issue found joined into one message — not just the first
one, so a resubmission after fixing issue #1 doesn't immediately fail on issue #2 the UI didn't
know about yet). **Nothing is partially saved** — this is the "single atomic transaction"
requirement: either the whole timetable is consistent and gets saved, or nothing changes:

| Validation | Rule | Example message |
|---|---|---|
| Duplicate subject | Two items name the same `classSubjectId` | "Duplicate subject '{id}' in the request." |
| Subject membership | `classSubjectId` doesn't exist, or belongs to a different class than `academicClassId` | "Class subject with id '{id}' was not found on this class." |
| Term Boundary Check | `examDate` must fall within `[examTerm.startDate, examTerm.endDate]` | "Exam date for subject 'MATH' (2026-09-15) falls outside the exam term's date range (2026-08-01 to 2026-08-10)." |
| Student Overlap Check | No two subjects for this class may be scheduled at overlapping times on the same date | "Subjects 'MATH' and 'SCIENCE' overlap on 2026-08-03." |
| Removal-blocks-on-marks | An existing exam whose subject is missing from `items` already has recorded `StudentExamMark` rows | "Subject 'ENGLISH' is no longer in the submitted routine but already has recorded marks -- remove its marks first, or include it in the routine." |

The overlap check is pairwise across the whole submitted grid (not just adjacent rows) and only
runs once every item has already passed the membership/term-boundary checks, so a genuinely broken
item doesn't also spam a dozen misleading overlap messages against it. **No Room/Invigilator
double-booking checks** — see "Room and invigilator removed" below.

### Response

```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Exam routine saved: 2 created, 1 updated, 1 removed.",
  "data": {
    "examTermId": "…",
    "academicClassId": "…",
    "createdCount": 2,
    "updatedCount": 1,
    "deletedCount": 1,
    "items": [
      { "id": "…", "examTermId": "…", "classSubjectId": "…", "subjectCode": "MATH", "gradeCode": "GRADE_9", "examDate": "2026-08-03T00:00:00", "startTime": "10:00:00", "endTime": "12:00:00", "remarks": null, "marksLocked": false, "fullMarks": 100, "passMarks": 35, "hasTheory": true, "hasPractical": false, "theoryMarks": 100, "practicalMarks": null, "theoryPassMarks": 35, "practicalPassMarks": null }
    ]
  }
}
```

`items` is every exam that now exists for this `(examTermId, academicClassId)` pair — full
`ExamDto`s (same shape `GET /api/exams/{id}` returns), so the UI can re-render the grid directly
from this response with no follow-up `GET`. There is no `skipped` list anymore — a validation
failure means `data` is absent entirely and `responseCode` is `VALIDATION_ERROR`.

### What changed, same day

The first cut of this endpoint (`POST /api/exams/routine`, `CreateExamRoutineCommand`) was
create-only and skip-list style: it only ever inserted new exams, and a bad item (already
scheduled, unknown subject) was reported in a `skipped` array while the rest of the batch still
saved. Redesigned the same day per the batch-scheduling spec above — the endpoint is now
`PUT /api/exams/routine` (`SaveExamRoutineCommand`/`SaveExamRoutineAsync`), a full idempotent sync
(create + update + remove) validated and saved as one atomic transaction, with no partial-success
skip list. The permission code changed too: `EXAM_CREATE_ROUTINE` is retired, replaced by
`EXAM_SAVE_ROUTINE`.

---

## 2. Two-tier composite mark structure — `ClassSubject`

**No new endpoint.** This is a behavior change on the existing subject endpoints,
`POST /api/academicclasses/{id}/subjects` (assign) and
`PUT /api/academicclasses/{id}/subjects/{classSubjectId}` (update) — see
`Docs/exam_management_implementation_guide.md` section 1 for the full field reference and
validation table; this section explains the composite-total design specifically.

### `fullMarks`/`passMarks` are computed, not entered — this is the optimization

**Redesigned same day** from an earlier version of this rule (which let the caller send
`fullMarks`/`passMarks` directly and cross-validated them against `theoryMarks`/`practicalMarks`).
That created a real correctness gap: `fullMarks` and the Theory/Practical figures were two
independent inputs that *should* always agree but weren't structurally forced to — a caller could
send `fullMarks: 100` with `theoryMarks: 75`/`practicalMarks: 20` (summing to 95) and the old
validator's `==` check would just reject it, but nothing stopped `fullMarks` from silently being
the *only* one edited on a later `PUT`, drifting out of sync with the components underneath it.

The fix removes the redundancy at the source: `fullMarks`/`passMarks` are **no longer request
fields at all** on `AssignClassSubjectCommand`/`UpdateClassSubjectCommand`. Theory and Practical
are the only inputs; the two totals are always computed server-side
(`AcademicClassService.ResolveCompositeMarks`) as their sum, so a mismatch is now structurally
impossible instead of merely rejected when caught:

```
┌───────────┐                   ┌────────────────────┐
│ FullMarks │  = TH_Full        │      FullMarks       │  = TH_Full + PR_Full
│ (computed)│    + PR_Full      │ TH: 75  │  PR: 25    │  = 75 + 25 = 100
├───────────┤    (PR_Full = 0   ├─────────┼────────────┤
│ PassMarks │     in theory-    │      PassMarks        │  = TH_Pass + PR_Pass
│ (computed)│     only mode)    │ TH: 27  │  PR: 10    │  = 27 + 10 = 37
└───────────┘                   └─────────┴────────────┘
     Whole                              Divided
```

The mode is still `hasTheory`/`hasPractical` on `ClassSubject` — **divided** is `hasTheory: true,
hasPractical: true`; **whole** is `hasTheory: true, hasPractical: false` (the default, and by far
the common case — most subjects have no separate practical component).

### Whole mode (theory-only) — the fallback rule

```json
POST /api/academicclasses/{id}/subjects
{
  "subjectCode": "ENGLISH",
  "isMandatory": true,
  "displayOrder": 1,
  "hasTheory": true,
  "hasPractical": false,
  "theoryMarks": 100,
  "theoryPassMarks": 35
}
```

`fullMarks`/`passMarks` are **not sent** — send only `theoryMarks`/`theoryPassMarks`. Response:

```json
{ "theoryMarks": 100, "theoryPassMarks": 35, "practicalMarks": 0, "practicalPassMarks": 0, "fullMarks": 100, "passMarks": 35, "hasTheory": true, "hasPractical": false, "...": "..." }
```

The **theory-only fallback rule**: `hasPractical: false` forces `practicalMarks`/
`practicalPassMarks` to `0` server-side (sending a non-null value for either yourself is rejected —
see the validation table in the main guide) — so `fullMarks = theoryMarks + 0 = theoryMarks` and
`passMarks = theoryPassMarks + 0 = theoryPassMarks` exactly. This is symmetric for a
practical-only subject (`hasTheory: false`, `hasPractical: true`) with the roles swapped.

### Divided mode — both components required together

```json
POST /api/academicclasses/{id}/subjects
{
  "subjectCode": "SCIENCE",
  "isMandatory": true,
  "displayOrder": 3,
  "hasTheory": true,
  "hasPractical": true,
  "theoryMarks": 75,
  "practicalMarks": 25,
  "theoryPassMarks": 27,
  "practicalPassMarks": 10
}
```

Response comes back with `fullMarks: 100` (`75 + 25`), `passMarks: 37` (`27 + 10`) — computed, not
echoed input. **When both `hasTheory` and `hasPractical` are `true`, `theoryMarks` and
`practicalMarks` must be supplied together** (both set, to get a meaningful total, or both left
unset until grading is configured) — supplying only one is a `400 VALIDATION_ERROR` now, since a
lone component would silently understate the total instead of correctly staying `null`
("ungraded").

### Ungraded stays ungraded — ungraded ≠ zero

Leaving the enabled component(s) unset keeps `fullMarks`/`passMarks` `null` on the response, not
`0` — grading configuration is optional at exam-creation time (main guide, section 3), and a
subject that hasn't been graded yet must never look identical to one that's been graded with a
zero-mark scheme. The sum is only computed once every *enabled* component actually has a value.

### Suggested UI: a single toggle, two rows

Render one toggle — "Whole marks" vs "Theory + Practical" — driving `hasPractical` (with
`hasTheory` always `true` in this UI; `hasTheory: false, hasPractical: true` — a practical-only
subject — is a valid combination the backend accepts but isn't a common case worth a dedicated
toggle state):

- **Whole marks** (`hasPractical: false`): show two inputs, `theoryMarks`/`theoryPassMarks` (labeled
  as the subject's Full/Pass Marks in the UI, since in this mode they *are* the total) — do **not**
  render separate `fullMarks`/`passMarks` inputs at all, and don't send them.
- **Theory + Practical** (`hasPractical: true`): show the two-row table from this guide's opening
  diagram — `theoryMarks`/`practicalMarks` and `theoryPassMarks`/`practicalPassMarks` as inputs,
  with `fullMarks`/`passMarks` displayed **read-only**, live-computed client-side as the sum for
  instant feedback (the server is still the authority — it recomputes and returns the real value on
  save, never trusts a client-computed figure).

This mirrors exactly what marks entry (`Docs/exam_management_implementation_guide.md` section 5)
and result generation (section 6) already do with `hasTheory`/`hasPractical` — the UI toggle just
makes the same flag visible at configuration time instead of only at entry time. The **independent
component passing rule** (a student must clear each enabled component's own pass mark
individually — passing the combined total alone is not enough if either component fails) is
unaffected by this round; it was already enforced in `ExamService.IsSubjectPassed` before this
change and needed no update.

---

## 3. Class period timing — `Domain/Entities/TimePeriod` (real table, replaced the Config catalog 2026-08-03)

**The problem this solves**: entering `startTime`/`endTime` by hand for every exam is error-prone
and doesn't match how schools actually think about scheduling — a school has a fixed bell schedule
(Period 1, Period 2, ...) and an exam is scheduled *into* a period, not given an arbitrary time.

**2026-08-03, redesigned twice the same day.** The first cut put this in the Config catalog
(`ConfigTypeCodes.ExamPeriod` then `ClassPeriod`, TypeCode `1024`). That was replaced the same
day by a real `TimePeriod` table plus a `ClassTimePeriod` class-mapping table — see
`Docs/time_period_and_class_routine_implementation_guide.md` for the full reference and the
reasoning (short version: "certain classes run different period structures" is a relationship a
flat Config option list can't express). `Exam.TimePeriodId`/`TeacherAssignment.TimePeriodId` are
real FKs into `TimePeriod` now, not Config-code strings, and a picked period must additionally be
**mapped to the exam/assignment's own class** via `ClassTimePeriod` — a check the Config-based cut
never had, because it had no class-scoping concept at all.

`TimePeriod.Kind` (`Period`/`Break`) still exists — breaks live in the same table as teaching
periods so a class's full-day routine can be built from one list, but **picking a `Break`-kind row
for `Exam.TimePeriodId` or `TeacherAssignment.TimePeriodId` is rejected** ("'Lunch Break' is a
break, not a teaching period.").

### Either a period or raw times — never both required

`POST/PUT /api/exams` and each item in `PUT /api/exams/routine` take `timePeriodId` alongside
`startTime`/`endTime` (`TimeSpan?`, not required):

```json
{ "examTermId": "…", "classSubjectId": "…", "examDate": "2026-08-10", "timePeriodId": "<Period 3 id>", "startTime": null, "endTime": null, "remarks": null }
```

```json
{ "examTermId": "…", "classSubjectId": "…", "examDate": "2026-08-10", "timePeriodId": null, "startTime": "10:00:00", "endTime": "12:00:00", "remarks": null }
```

**Exactly one path is required** — sending neither is a `400 VALIDATION_ERROR` ("Either
TimePeriodId or both StartTime and EndTime must be provided."). Sending `timePeriodId` makes the
backend resolve the concrete `startTime`/`endTime` from that period's own start/end time — any raw
`startTime`/`endTime` sent alongside a `timePeriodId` is ignored, not cross-validated, so don't
bother sending both. The response's `ExamDto` always carries concrete `startTime`/`endTime` either
way (the entity itself never stores a null time), plus `timePeriodId`/`timePeriodName` (both
`null` if raw times were used).

| Failure | Message |
|---|---|
| Neither `timePeriodId` nor both `startTime`/`endTime` given | "Either TimePeriodId or both StartTime and EndTime must be provided." |
| `timePeriodId` doesn't match a real `TimePeriod` | "Time period with id 'X' was not found." |
| `timePeriodId` matches a `Break`-kind row | "'Lunch Break' is a break, not a teaching period." |
| `timePeriodId` isn't mapped to the exam's class | "'Period 3' is not mapped to this class -- map it first via the Time Periods screen." |
| Resolved `endTime` not after `startTime` | "EndTime must be after StartTime." |

`ExamDto` resolves `timePeriodName` server-side (from the `TimePeriod` navigation) — no second
lookup needed, unlike the earlier Config-based cut which deliberately left label resolution to the
client.

### Suggested UI

A single-select dropdown per exam row, sourced from the class's own mapped periods
(`GET /api/timeperiods/map/{academicClassId}`, filtered to `kind: 0` Period rows client-side —
breaks show up in that list too, for rendering the day, but shouldn't appear in an exam/assignment
picker), defaulting to "Custom time" (which reveals the raw `startTime`/`endTime` inputs instead).
This slots directly into the batch-scheduling grid from section 1 — each subject's row picks its
own period independently, same as it picks its own date. **A class must be mapped to at least one
period first** (`POST /api/timeperiods/map`) before this dropdown has anything to show.

---

## 4. Section-aware & student-wise marks entry

Two related fixes to how marks entry is scoped, addressing a real gap: **an `Exam` always covers
the whole grade (no `ClassSectionId` of its own), but the same subject can legitimately be taught
by different teachers in different sections.** Nothing before this round stopped a section-A
teacher's marks list from showing section B's students too.

### `classSectionId` on the marks list and roster

`GET /api/studentexammarks` and `GET /api/studentexammarks/roster` both gained an optional
`classSectionId` query parameter:

```
GET /api/studentexammarks/roster?examId=…&classSectionId=…
GET /api/studentexammarks?examId=…&classSectionId=…
```

When given, only students whose `Enrollment.ClassSectionId` matches are returned — filtered
straight through the existing `Enrollment` relationship (`Enrollment.ClassSectionId`), no schema
change, no new column. Omit it for the full whole-grade list (the admin case, or a subject that
genuinely isn't split by teacher).

**There is still no "who is the currently logged-in teacher" resolution anywhere in this
codebase** (the same gap `GET /api/exams?teacherId=` already documents) — `classSectionId` is a
plain filter parameter the caller supplies, not something the backend infers from a JWT. The
intended client flow for a teacher-wise entry screen:

1. `GET /api/exams?teacherId={teacherId}` — the teacher's own exam worklist (unchanged, already
   existed).
2. `GET /api/teachers/{teacherId}` — read `serviceHistory` for this exam's `classSubjectId` to find
   that specific `TeacherAssignment`'s own `ClassSectionId`. `null` there means the assignment
   already covers every section (a genuinely grade-wide teaching assignment) — in that case, don't
   pass `classSectionId` either.
3. `GET /api/studentexammarks/roster?examId=…&classSectionId={theSectionFromStep2}` — the
   teacher's own, correctly-scoped roster.

An admin building an office-wide entry tool can simply omit `classSectionId` to see everyone, or
pass an explicit section to browse one at a time.

### Admin, student-wise marks entry — `GET /api/studentexammarks/student/{enrollmentId}`

The roster above answers "one subject, every student." This new endpoint answers the opposite
question — "one student, every subject" — the natural admin/office flow for reviewing or
completing a single student's whole term in one screen instead of hunting through every subject's
own roster:

```
GET /api/studentexammarks/student/{enrollmentId}?examTermId=…
```

Full request/response shape and field reference: `Docs/exam_management_implementation_guide.md`
section 5 ("Admin, student-wise marks entry"). Same rules as the roster: `mark: null` → the UI
renders an empty entry row and calls `POST /api/studentexammarks`; `mark` non-null → prefill and
`PUT /api/studentexammarks/{mark.id}`. Read-only worklist, not a new submission path — both entry
screens (teacher-wise roster, admin student-wise) ultimately save through the same
create/update/bulk-upsert endpoints that already existed.

New permission: `STUDENT_EXAM_MARK_BY_STUDENT` (under `STUDENT_EXAM_MARK_LIST`).

---

## Room and invigilator removed (context, not a new feature)

The exam-hall Room/seat-arrangement subsystem (`ExamRoom`, `ExamHallArrangement`,
`ExamHallArrangementClass`, `ExamSeatAllocation`, the `/api/examrooms` and
`/api/examhallarrangements` controllers, `Exam.RoomId`) was removed entirely in this same round —
the module only needs simple subject/date/time scheduling. A migration already exists for the
schema side of that removal — `Infrastructure/Migrations/20260730061558_changes in exam module3
update.cs` — apply it with `dotnet ef database update` if your database hasn't run it yet.

**Same round, follow-up**: `Exam.InvigilatorEmployeeId`/`InvigilatorEmployee` were also removed —
an `Exam` is now just subject + date/time + remarks (+ the `timePeriodId` from section 3),
nothing else. `invigilatorEmployeeId` is gone from
`CreateExamCommand`/`UpdateExamCommand`/`ExamRoutineItemInput`/`ExamDto` — drop it from any request
bodies and stop reading it off responses. **No migration exists for this part yet** —
`dbo.exams.invigilator_employee_id` (and its FK to `employees`) is still in the schema; see the
migration-status callout at the top of `Docs/exam_management_implementation_guide.md` for the exact
column to drop. This doesn't break anything today (EF simply never reads/writes that column), it's
just a cleanup gap. **`Exam.TimePeriodId` (section 3), by contrast, needs its migration written and
applied before this module works at all** — see
`Docs/time_period_and_class_routine_implementation_guide.md`'s migration section for the exact
statements (it supersedes an already-applied `period_code` column migration, not just adds one).

If an older frontend build still sends `roomId`/`invigilatorEmployeeId` on `POST`/`PUT /api/exams`
or calls either removed controller, drop those calls — none of those fields exist anymore.

**Deliberately out of scope for the batch-scheduling workflow (section 1)**: the original spec's
Room Double-Booking and Invigilator Collision validation rules are not implemented — confirmed with
the requester, both fields stay out of this module for now. `items` in `PUT /api/exams/routine`
carries only `classSubjectId`/`examDate`/`timePeriodId`/`startTime`/`endTime`/`remarks`. If
room/invigilator scheduling is needed later, it's new work, not a resurrection of the removed
`ExamRoom`/`ExamHallArrangement` entities verbatim — revisit the design against the actual
requirement then.

## Permissions

- `EXAM_SAVE_ROUTINE` (under the existing `EXAM_LIST` sub-menu, `Exams` controller /
  `SaveExamRoutine` action) — seeded to SuperAdmin, grant to other roles via
  `POST /api/roles/claims` like any other permission. Replaces `EXAM_CREATE_ROUTINE` (retired same
  day, see "What changed, same day" in section 1) — a role that had the old grant needs
  `EXAM_SAVE_ROUTINE` granted instead.
- `STUDENT_EXAM_MARK_BY_STUDENT` (under the existing `STUDENT_EXAM_MARK_LIST` sub-menu,
  `StudentExamMarks` controller / `GetStudentExamMarksByStudent` action, section 4) — new, no
  retired predecessor.
