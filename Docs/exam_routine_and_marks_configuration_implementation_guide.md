# CMSApp — Exam Routine (Whole-Class Scheduling) & Whole-vs-Divided Marks Configuration (UI)

**What this is**: two additions to the existing Exam Management module (2026-07-30), companions to
the main reference, `Docs/exam_management_implementation_guide.md`:

1. **`POST /api/exams/routine`** — schedule an `Exam` for every subject of one class within one
   exam term, in a single call, instead of one `POST /api/exams` per subject.
2. **Whole-vs-divided assessment configuration** on `ClassSubject` — Full Marks/Pass Marks can be
   entered as one whole figure or split into Theory/Practical, and the split is now driven
   explicitly by `hasTheory`/`hasPractical`, with the "whole" case defaulting Theory's own
   full/pass marks to the subject's overall figures automatically.

Same round removed the exam-hall Room/seat-arrangement subsystem entirely (`ExamRoom`,
`ExamHallArrangement`, `ExamHallArrangementClass`, `ExamSeatAllocation`) — see the "Room removed"
note at the end of this guide and the migration-status callout at the top of
`Docs/exam_management_implementation_guide.md`.

---

## 1. Schedule a whole class's exam routine — `POST /api/exams/routine`

**The problem this solves**: a school doesn't schedule "one subject's exam" — it schedules the
whole term's routine for a grade at once (Math on Monday, Science on Tuesday, English on
Wednesday, ...). Doing that one `POST /api/exams` call per subject works but is tedious; this
endpoint takes the whole routine in one request.

```
POST /api/exams/routine
{
  "examTermId": "…",
  "academicClassId": "…",
  "items": [
    { "classSubjectId": "…", "examDate": "2026-08-03", "startTime": "10:00:00", "endTime": "12:00:00", "invigilatorEmployeeId": null, "remarks": null },
    { "classSubjectId": "…", "examDate": "2026-08-04", "startTime": "10:00:00", "endTime": "12:00:00", "invigilatorEmployeeId": "…", "remarks": "Bring calculator" },
    { "classSubjectId": "…", "examDate": "2026-08-05", "startTime": "10:00:00", "endTime": "12:00:00" }
  ]
}
```

- `examTermId`/`academicClassId` are shared by the whole call — one exam term, one grade.
- Each item in `items` still carries its **own** `examDate`/`startTime`/`endTime` (different
  subjects sit on different days/times) and its own optional `invigilatorEmployeeId`/`remarks` —
  exactly the same per-item shape `POST /api/exams` takes, minus `examTermId`/`classSubjectId`
  duplication (the term is shared, `classSubjectId` is per item).
- **Typical UI flow**: call `GET /api/academicclasses/{academicClassId}/subjects` first to list the
  class's subjects (see `Docs/student_management_implementation_guide.md`), render one row per
  subject with date/time/invigilator/remarks inputs, then submit the whole grid as `items` here.

### Validation

| Rule | Message |
|---|---|
| `examTermId` empty | "'Exam Term Id' must not be empty." |
| `academicClassId` empty | "'Academic Class Id' must not be empty." |
| `items` empty | "'Items' must not be empty." |
| Any item's `classSubjectId` empty | "'Class Subject Id' must not be empty." |
| Any item's `endTime` not after its `startTime` | "EndTime must be after StartTime." |
| Any item's `remarks` over 500 chars | length error |

Top-level `404 NOT_FOUND` only for an unknown `examTermId` or `academicClassId` — everything else
is **skip-list style**, same convention as `POST /api/studentexammarks/bulk`: a bad item is
reported in the response's `skipped` array and the call still returns `200`/`SUCCESS` for the rest.

| Skip reason | Cause |
|---|---|
| "Duplicate subject in this request -- only the first occurrence is used." | Two items name the same `classSubjectId` |
| "This class subject was not found on this class." | Unknown `classSubjectId`, or it belongs to a different class than `academicClassId` |
| "An exam for this subject already exists within this term." | Same `409 CONFLICT` rule `POST /api/exams` enforces, just non-fatal here |
| "Invigilator employee with id '…' was not found." | Unknown `invigilatorEmployeeId` |

### Response

```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Exam routine created: 3 scheduled, 1 skipped.",
  "data": {
    "examTermId": "…",
    "academicClassId": "…",
    "created": [
      { "id": "…", "examTermId": "…", "classSubjectId": "…", "subjectCode": "MATH", "gradeCode": "GRADE_9", "examDate": "2026-08-03T00:00:00", "startTime": "10:00:00", "endTime": "12:00:00", "invigilatorEmployeeId": null, "invigilatorName": null, "remarks": null, "marksLocked": false, "fullMarks": 100, "passMarks": 35, "hasTheory": true, "hasPractical": false, "theoryMarks": 100, "practicalMarks": null, "theoryPassMarks": 35, "practicalPassMarks": null }
    ],
    "skipped": [
      { "classSubjectId": "…", "subjectCode": "SCIENCE", "reason": "An exam for this subject already exists within this term." }
    ]
  }
}
```

`created` items are full `ExamDto`s (same shape `GET /api/exams/{id}` returns) — no follow-up call
needed to render the freshly-scheduled rows. Each created exam also gets its own linked
`CalendarEvent`, exactly like a single `POST /api/exams` call (see the main guide's section 3).

Not every item needs to succeed for the call to be useful — re-submitting the same `items` array
after fixing the flagged rows is safe: subjects that already got an exam the first time round are
skipped again (already exists), not duplicated.

---

## 2. Whole-vs-divided marks configuration — `ClassSubject`

**No new endpoint.** This is a behavior change on the existing subject endpoints,
`POST /api/academicclasses/{id}/subjects` (assign) and
`PUT /api/academicclasses/{id}/subjects/{classSubjectId}` (update) — see
`Docs/exam_management_implementation_guide.md` section 1 for the full field reference and
validation table; this section explains the whole-vs-divided default specifically.

### The two modes

A subject's marks scheme is either **whole** (one figure, Theory only) or **divided** (separate
Theory and Practical figures that add up to the whole):

```
Whole:                          Divided:
┌───────────┐                   ┌────────────────────┐
│ FullMarks │                   │      FullMarks      │
│    100    │                   │ TH: 75  │  PR: 25   │
├───────────┤                   ├─────────┼───────────┤
│ PassMarks │                   │      PassMarks       │
│    35     │                   │ TH: 27  │  PR: 10   │
└───────────┘                   └─────────┴───────────┘
```

The mode is `hasTheory`/`hasPractical` on `ClassSubject` — **divided** is `hasTheory: true,
hasPractical: true`; **whole** is `hasTheory: true, hasPractical: false` (the default, and by far
the common case — most subjects have no separate practical component).

### The default that matters: whole mode auto-fills Theory's own marks

**Assign/update a whole-marks subject with just `fullMarks`/`passMarks` — you do not need to also
repeat them as `theoryMarks`/`theoryPassMarks`:**

```json
POST /api/academicclasses/{id}/subjects
{
  "subjectCode": "ENGLISH",
  "isMandatory": true,
  "displayOrder": 1,
  "fullMarks": 100,
  "passMarks": 35,
  "hasTheory": true,
  "hasPractical": false
}
```

The backend now defaults `theoryMarks` to `fullMarks` and `theoryPassMarks` to `passMarks`
whenever `hasPractical` is `false` and you didn't explicitly set them — the response's
`ClassSubjectDto` comes back with `theoryMarks: 100, theoryPassMarks: 35` even though the request
never mentioned them. **Why this matters**: marks entry (`POST /api/studentexammarks`) caps
`theoryObtainedMarks` against the subject's `theoryMarks` — before this default, a whole-marks
subject left with `theoryMarks: null` had **no cap at all**, silently allowing a mark greater than
the subject's own `fullMarks` to be entered and accepted. Now the cap is always in effect, sourced
from whichever figure you actually configured.

You can still set `theoryMarks`/`theoryPassMarks` explicitly even in whole mode (e.g. if for some
reason they should differ slightly from the overall figures) — the default only fills in what you
left blank, it never overrides an explicit value.

### Divided mode: both components are entered explicitly, no default

```json
POST /api/academicclasses/{id}/subjects
{
  "subjectCode": "SCIENCE",
  "isMandatory": true,
  "displayOrder": 3,
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

When `hasPractical: true`, `theoryMarks`/`theoryPassMarks` are used exactly as submitted — there is
no auto-fill in divided mode, since there's no single "whole" figure to fall back to (the existing
validator still enforces `theoryMarks + practicalMarks == fullMarks` when both are given, and
`theoryPassMarks <= theoryMarks`/`practicalPassMarks <= practicalMarks`).

### Suggested UI: a single toggle, two rows

Render one toggle — "Whole marks" vs "Theory + Practical" — driving `hasPractical` (with
`hasTheory` always `true` in this UI; `hasTheory: false, hasPractical: true` — a practical-only
subject — is a valid combination the backend accepts but isn't a common case worth a dedicated
toggle state):

- **Whole marks** (`hasPractical: false`): show two inputs, `fullMarks`/`passMarks`. Leave
  `theoryMarks`/`theoryPassMarks`/`practicalMarks`/`practicalPassMarks` out of the request entirely
  — the backend fills Theory's figures from the whole ones automatically.
  Leave the whole thing null before the subject's marks scheme is decided — grading configuration
  is optional at exam-creation time (see the main guide's section 3).
- **Theory + Practical** (`hasPractical: true`): show the two-row table from this guide's opening
  diagram — `fullMarks`/`passMarks` (the row totals, still required) plus `theoryMarks`/
  `practicalMarks` and `theoryPassMarks`/`practicalPassMarks` (the per-column figures), and
  validate client-side that the Theory/Practical pair sums to the whole before submitting (the
  server re-validates regardless).

This mirrors exactly what marks entry (`Docs/exam_management_implementation_guide.md` section 5)
and result generation (section 6) already do with `hasTheory`/`hasPractical` — the UI toggle just
makes the same flag visible at configuration time instead of only at entry time.

---

## Room removed (context, not a new feature)

The exam-hall Room/seat-arrangement subsystem (`ExamRoom`, `ExamHallArrangement`,
`ExamHallArrangementClass`, `ExamSeatAllocation`, the `/api/examrooms` and
`/api/examhallarrangements` controllers, `Exam.RoomId`) was removed entirely in this same round —
the module only needs simple subject/date/time scheduling. If an older frontend build still sends
`roomId` on `POST`/`PUT /api/exams` or calls either removed controller, drop those calls; the field
no longer exists on `CreateExamCommand`/`UpdateExamCommand`/`ExamDto` and the two controllers are
gone. `InvigilatorEmployeeId` is unaffected — it was always a plain optional `Employee` reference
with no dedicated table, so it stays exactly as it was.

## New permission

`EXAM_CREATE_ROUTINE` (under the existing `EXAM_LIST` sub-menu, `Exams` controller /
`CreateExamRoutine` action) — seeded to SuperAdmin, grant to other roles via
`POST /api/roles/claims` like any other permission.
