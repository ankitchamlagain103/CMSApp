# Teacher Assignment — Bulk Entry (Class / Subject / Section / Time Period)

2026-08-04. Adds a general-purpose bulk-entry endpoint for `TeacherAssignment` rows, so a
teacher's whole routine — several different classes, subjects, sections, and time periods — can
be submitted in one API call instead of one `POST /api/teachers/{id}/assignments` call per row.

**Same-day follow-up round: three validation tightenings, applied to every assignment endpoint
(single, both bulk endpoints, and the class-scoped bulk-entry endpoint), not just this one.**
`Application/Teachers/TeacherAssignmentBuilder.BuildAsync` is the one shared place all four
endpoints' per-row validation goes through, so these apply uniformly:

1. **`classSectionId` is now required** for a class-wide subject — a teacher can no longer be
   assigned "to every section of the class" in one row. See the field reference below.
2. **Time-period double-booking is now blocked** — a teacher can't be assigned to two different
   classes/sections during the same `timePeriodId`.
3. **"One class teacher per section" was already enforced** (unchanged) — restated here since it's
   the invariant that made rule 1 necessary to state precisely: without a required section, "one
   class teacher per section" had no well-defined section to check against for the class-wide case.

## Why this is a new endpoint, not a change to the existing bulk endpoint

`POST /api/teachers/{id}/assignments/bulk` (2026-08-03, `AssignTeacherBulkCommand`) already
exists, but it fixes **one** `ClassSubjectId` and **one** `TimePeriodId` for the whole call and
only varies the list of sections — it answers "assign this teacher to this subject across
sections 6-A, 6-B, 6-C". It does not help when an admin wants to enter a teacher's full weekly
routine in one screen, where every row can be a different class, subject, section, and period.

This guide's endpoint is that general case: every row (`Items[i]`) carries its own
`ClassSubjectId` / `ClassSectionId` / `TimePeriodId` / `IsClassTeacher`. All three endpoints stay —
use whichever shape fits the screen you're building:

| | `.../assignments/bulk` | `.../assignments/bulk-entry` (this guide) | `academicclasses/{id}/teacher-assignments/bulk-entry` |
|---|---|---|---|
| Same subject across many sections | ✅ (built for this) | works, but each row repeats the subject | works, but each row repeats the subject |
| A teacher's whole routine (mixed classes/subjects/sections/periods) | not supported | ✅ (built for this) | not supported (single class only) |
| "Who teaches this class" (several different teachers, one class) | not supported | not supported (single teacher only) | ✅ (built for this) |
| Scope | one teacher (route id) | one teacher (route id) | one academic class (route id) |

Same scoping rule as every assignment endpoint in this codebase: there is still no
"logged-in-teacher" identity resolution anywhere, so a grid spanning *multiple teachers at once*
is out of scope for this endpoint specifically — the caller (frontend) already knows which
teacher's profile page it's submitting from and puts that id in the route, same as the
single-assignment endpoint. **If you need multiple teachers mapped in one call, see the
class-scoped sibling below.**

See also: `Docs/class_teacher_assignment_bulk_entry_implementation_guide.md` — the class-scoped
sibling to this endpoint, for mapping several different teachers onto one class (e.g. "who teaches
this class") from the class's own page instead of one teacher's profile.

## Endpoint

```
POST /api/teachers/{id}/assignments/bulk-entry
```

`{id}` — the Teacher's id (same id space as `/api/teachers/{id}`, `/api/teachers/{id}/assignments`).

Permission: `TEACHER_ASSIGNMENT_BULK_ENTRY_ADD` (seeded under `EMPLOYEE_LIST`, action
`AssignClassSubjectBulkEntry` on the `Teachers` controller — grant it the same way every other
teacher-management permission is granted, via `POST /api/roles/claims`). SuperAdmin has it by
default.

### Request body

```json
{
  "items": [
    {
      "classSubjectId": "6f1a...-subject-guid",
      "classSectionId": "3c2b...-section-guid",
      "isClassTeacher": false,
      "timePeriodId": "9e77...-period-guid"
    },
    {
      "classSubjectId": "a1b2...-subject-guid-2",
      "classSectionId": "3c2b...-section-guid",
      "isClassTeacher": false,
      "timePeriodId": "9e77...-period-guid-2"
    },
    {
      "classSubjectId": "6f1a...-subject-guid",
      "classSectionId": "3c2b...-section-guid",
      "isClassTeacher": true,
      "timePeriodId": null
    }
  ]
}
```

Field reference (each item, identical semantics to `POST /api/teachers/{id}/assignments`'
`AssignTeacherCommand`):

| Field | Type | Required | Notes |
|---|---|---|---|
| `classSubjectId` | guid | yes | The `ClassSubject` row (from `GET /api/academicclasses/{id}/subjects`) this row assigns the teacher to. |
| `classSectionId` | guid \| null | **yes**, for a class-wide subject | A teacher must be assigned to one specific section — **a single row can no longer cover "every section of the class"** (removed 2026-08-04: a teacher physically can't teach every section at once). Only stays optional when `classSubjectId` points at a subject that is *itself* section-scoped — in that case the section is derived from the subject automatically and doesn't need to be repeated. Omitting it for a class-wide subject is a `ValidationError`. |
| `isClassTeacher` | bool | no (default `false`) | Marks this teacher as the class teacher of the named section. **At most one class teacher per section** — checked against the database and against every other row in this same request. |
| `timePeriodId` | guid \| null | no | A `TimePeriod` id (`GET /api/timeperiods`) — which routine slot this row occupies. Must be a `Period`-kind row (not a `Break`) mapped to the row's own `AcademicClass` via `POST /api/timeperiods/map`. **A teacher can only be in one class/section during a given period** (added 2026-08-04) — if this teacher already has *any other* assignment (any class, subject, or section) sharing this same `timePeriodId`, the row is rejected as a scheduling conflict. Checked against the database and against every other row in this same request. |

There is no top-level `teacherId` field — the teacher comes from the route, same as every other
`/api/teachers/{id}/...` endpoint.

### Response body

`200 OK` with the standard envelope. Like every bulk endpoint in this codebase, this is
**skip-list style, not atomic** — a bad row is reported in `skipped`, it never fails rows that are
otherwise valid. Only a request-shape problem (missing/empty `items`, teacher not found) fails the
whole call.

```json
{
  "responseCode": "Success",
  "responseMessage": "2 assignment(s) created, 1 skipped.",
  "data": {
    "created": [
      {
        "id": "d4e5...",
        "teacherId": "b2c3...",
        "classSubjectId": "6f1a...-subject-guid",
        "academicClassId": "7d8e...",
        "subjectCode": "MATH",
        "classSectionId": "3c2b...-section-guid",
        "sectionCode": "A",
        "scope": 1,
        "isClassTeacher": false,
        "timePeriodId": "9e77...-period-guid",
        "timePeriodName": "Period 3"
      },
      {
        "id": "f6a7...",
        "teacherId": "b2c3...",
        "classSubjectId": "a1b2...-subject-guid-2",
        "academicClassId": "7d8e...",
        "subjectCode": "SCIENCE",
        "classSectionId": "3c2b...-section-guid",
        "sectionCode": "A",
        "scope": 1,
        "isClassTeacher": false,
        "timePeriodId": "9e77...-period-guid-2",
        "timePeriodName": "Period 5"
      }
    ],
    "skipped": [
      {
        "itemIndex": 2,
        "classSubjectId": "6f1a...-subject-guid",
        "classSectionId": "3c2b...-section-guid",
        "reason": "This section already has a class teacher. Remove that assignment first."
      }
    ]
  }
}
```

`scope` is the existing `SubjectScope` enum (`0` = ClassWide, `1` = Section) — same field
`TeacherAssignmentDto` already carries everywhere else.

`skipped[].itemIndex` is the 0-based index into the `items` array you submitted — use it (not
`classSubjectId`/`classSectionId` alone) to map a skip back to its row in the UI grid, since two
rows can legitimately name the same subject/section pair when one of them is itself the reason
for the skip (a duplicate row within the same submission).

### Failure reasons that land in `skipped[].reason`

Same validation `POST /api/teachers/{id}/assignments` already applies, per row, plus the
in-batch-only checks:

| Reason | Cause |
|---|---|
| `ClassSubjectId is required.` | Row's `classSubjectId` was empty/missing. |
| `Class subject with id '...' was not found.` | `classSubjectId` doesn't resolve to a real `ClassSubject`. |
| `Subject '...' is only offered in a different section.` | The subject is section-scoped and the row named a different section than the one it's actually offered in. |
| `ClassSectionId is required -- a teacher must be assigned to one specific section, not every section of the class at once.` | **2026-08-04** — the subject is class-wide and the row left `classSectionId` blank. |
| `Class section with id '...' was not found.` | `classSectionId` doesn't resolve to a real section. |
| `That section belongs to a different class than the subject.` | Section/subject mismatch. |
| `This teacher is already assigned to that class subject for that section.` | Duplicate against an **existing** database row. |
| `This section already has a class teacher. Remove that assignment first.` | Duplicate class-teacher against an existing database row. |
| `Time period with id '...' was not found.` | `timePeriodId` doesn't resolve. |
| `'...' is a break, not a teaching period.` | `timePeriodId` pointed at a `Break`-kind `TimePeriod`. |
| `'...' is not mapped to this class...` | The period isn't mapped (via `POST /api/timeperiods/map`) to the row's `AcademicClass`. |
| `This teacher is already assigned to another class/section during '...'.` | **2026-08-04** — this teacher already has a different, already-saved assignment (any class/subject/section) using the same `timePeriodId`. |
| `Duplicate of an earlier item in this same request.` | Two rows in the **same submission** resolve to the same `(classSubjectId, effective section)` pair. |
| `Another item earlier in this same request already makes this teacher the class teacher for that section.` | Two rows in the **same submission** both set `isClassTeacher: true` for the same section. |
| `Another item earlier in this same request already assigns this teacher to that time period.` | **2026-08-04** — two rows in the **same submission** share the same `timePeriodId` for this teacher (any class/subject/section). |

### Request-level failures (whole call rejected, nothing created)

| HTTP | `responseCode` | Cause |
|---|---|---|
| 404 | `NotFound` | Teacher `{id}` doesn't exist. |
| 400 | `ValidationError` | `items` missing or empty. |

## Typical UI use

A "Teacher Routine" grid on the teacher profile page: one row per period the teacher is being
assigned, columns for Class → Subject (drives `classSubjectId`), Section, Period, and an
"is class teacher" checkbox on at most one row. Submit the whole grid as one `items` array;
re-render the grid from `created` + `skipped` so the admin sees exactly which rows saved and
which need a fix, without re-entering the rows that already succeeded.

## No new migration

This endpoint only creates ordinary `TeacherAssignment` rows through the same repository method
(`AddAssignmentAsync`) the existing single/bulk-sections endpoints already use — no new entity,
no new column.
