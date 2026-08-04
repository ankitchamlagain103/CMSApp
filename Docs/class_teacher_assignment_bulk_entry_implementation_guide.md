# Class Teacher Assignment — Bulk Entry (from the Academic Class side)

2026-08-04, same day as `teacher_assignment_bulk_entry_implementation_guide.md`. Adds the
class-scoped counterpart to that guide's endpoint: instead of starting from one teacher's profile
and entering their routine, an admin can start from one **class**'s page and map every teacher who
teaches that class — across its subjects, sections, and time periods — in a single call.

**Same-day follow-up round: this endpoint picked up the same three validation tightenings as its
teacher-scoped sibling** (shared code, `TeacherAssignmentBuilder.BuildAsync`) — `classSectionId` is
now required for a class-wide subject, a teacher can't be double-booked into two different
classes/sections during the same `timePeriodId`, and "one class teacher per section" is unchanged
but now unconditionally checked since a section is always present. See the field reference and
failure table below for the class-scoped specifics (in particular: the in-request duplicate/
conflict keys here include `teacherId`, since several different teachers are in play in one call).

## Why a third assignment endpoint

There are now three ways to create `TeacherAssignment` rows in bulk. Pick whichever matches the
screen you're building:

| Endpoint | Scope (route id) | Varies per row | Best for |
|---|---|---|---|
| `POST /api/teachers/{id}/assignments/bulk` | one teacher | section only (subject/period fixed) | "assign this teacher to this subject across sections 6-A, 6-B, 6-C" |
| `POST /api/teachers/{id}/assignments/bulk-entry` | one teacher | subject/section/period | "enter this one teacher's whole routine" (teacher profile page) |
| `POST /api/academicclasses/{id}/teacher-assignments/bulk-entry` (this guide) | one class | teacher/subject/section/period | "who teaches this class" (class page) — several *different teachers* mapped in one screen |

The class-scoped endpoint is the only one where `teacherId` varies per row — it's the natural fit
for a "class routine" grid where each subject/section slot is taught by a different person. Every
row still runs through the exact same validation as the other two endpoints (same shared helper,
`TeacherAssignmentBuilder.BuildAsync`), so a row that would be rejected on the teacher-scoped
endpoint is rejected here too, for the same reason.

## Endpoint

```
POST /api/academicclasses/{id}/teacher-assignments/bulk-entry
```

`{id}` — the `AcademicClass`'s id (same id space as `/api/academicclasses/{id}`,
`/api/academicclasses/{id}/subjects`, `/api/academicclasses/{id}/sections`).

Permission: `CLASS_TEACHER_ASSIGNMENT_BULK_ENTRY_ADD` (seeded under `CLASS_LIST`, action
`AssignTeachersBulkEntry` on the `AcademicClasses` controller). SuperAdmin has it by default.

### Request body

```json
{
  "items": [
    {
      "teacherId": "b2c3...-teacher-guid-1",
      "classSubjectId": "6f1a...-subject-guid",
      "classSectionId": "3c2b...-section-guid",
      "isClassTeacher": false,
      "timePeriodId": "9e77...-period-guid"
    },
    {
      "teacherId": "a4d5...-teacher-guid-2",
      "classSubjectId": "a1b2...-subject-guid-2",
      "classSectionId": "3c2b...-section-guid",
      "isClassTeacher": false,
      "timePeriodId": "9e77...-period-guid-2"
    },
    {
      "teacherId": "b2c3...-teacher-guid-1",
      "classSubjectId": "6f1a...-subject-guid",
      "classSectionId": "3c2b...-section-guid",
      "isClassTeacher": true,
      "timePeriodId": null
    }
  ]
}
```

Field reference (each item):

| Field | Type | Required | Notes |
|---|---|---|---|
| `teacherId` | guid | yes | The teacher this row assigns. **New field vs. the teacher-scoped bulk-entry endpoint** — there the teacher comes from the route; here it must be named per row, since one class has several teachers. |
| `classSubjectId` | guid | yes | Must be a `ClassSubject` belonging to **this** `AcademicClass` (the route id) — a row naming a subject from a different class is skipped, not silently allowed. |
| `classSectionId` | guid \| null | **yes**, for a class-wide subject | A teacher must be assigned to one specific section — **a row can no longer cover "every section of the class"** (removed 2026-08-04). Only stays optional when `classSubjectId` itself is section-scoped (the section is derived automatically in that case). |
| `isClassTeacher` | bool | no (default `false`) | At most one class teacher per section — checked against the database **and** against every other row in this same request, regardless of which teacher each row names. |
| `timePeriodId` | guid \| null | no | Must be a `Period`-kind `TimePeriod` mapped to this `AcademicClass` via `POST /api/timeperiods/map`. **A teacher can only be in one class/section during a given period** (added 2026-08-04) — flagged if this *same* teacher already has another assignment (any class/subject/section) using this `timePeriodId`; two *different* teachers sharing a period is fine. Checked against the database and against every other row in this same request. |

### Response body

Same envelope shape as the teacher-scoped bulk-entry endpoint, skip-list style (not atomic) — a
bad row is reported in `skipped`, valid rows still save.

```json
{
  "responseCode": "Success",
  "responseMessage": "2 assignment(s) created, 1 skipped.",
  "data": {
    "created": [
      {
        "id": "d4e5...",
        "teacherId": "b2c3...-teacher-guid-1",
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
        "teacherId": "a4d5...-teacher-guid-2",
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
        "teacherId": "b2c3...-teacher-guid-1",
        "classSubjectId": "6f1a...-subject-guid",
        "classSectionId": "3c2b...-section-guid",
        "reason": "This section already has a class teacher. Remove that assignment first."
      }
    ]
  }
}
```

`created[]` is the existing `TeacherAssignmentDto` shape (unchanged — this endpoint creates
ordinary `TeacherAssignment` rows, same as every other assignment endpoint). `skipped[].itemIndex`
is the 0-based index into the submitted `items` array; use it, not the teacher/subject/section
combination alone, to map a skip back to its grid row.

### Failure reasons that land in `skipped[].reason`

Everything the teacher-scoped bulk-entry endpoint reports, plus two that are unique to entering
from the class side:

| Reason | Cause |
|---|---|
| `TeacherId and ClassSubjectId are required.` | Row is missing one or both. |
| `Teacher with id '...' was not found.` | `teacherId` doesn't resolve to a real teacher. |
| `Class subject with id '...' was not found.` | `classSubjectId` doesn't resolve. |
| `That class subject does not belong to this academic class.` | The subject exists but belongs to a *different* `AcademicClass` than the route id. |
| `Subject '...' is only offered in a different section.` | Section-scoped subject, wrong section named. |
| `ClassSectionId is required -- a teacher must be assigned to one specific section, not every section of the class at once.` | **2026-08-04** — the subject is class-wide and the row left `classSectionId` blank. |
| `Class section with id '...' was not found.` | `classSectionId` doesn't resolve. |
| `That section belongs to a different class than the subject.` | Section/subject mismatch. |
| `This teacher is already assigned to that class subject for that section.` | Duplicate against an existing database row. |
| `This section already has a class teacher. Remove that assignment first.` | Duplicate class-teacher against an existing database row. |
| `Time period with id '...' was not found.` | Bad `timePeriodId`. |
| `'...' is a break, not a teaching period.` | `timePeriodId` pointed at a `Break`-kind row. |
| `'...' is not mapped to this class...` | Period not mapped to this `AcademicClass`. |
| `This teacher is already assigned to another class/section during '...'.` | **2026-08-04** — this **same teacher** already has a different, already-saved assignment (any class/subject/section) using the same `timePeriodId`. Two different teachers sharing a period is not flagged. |
| `Duplicate of an earlier item in this same request.` | Two rows in the same submission resolve to the same `(teacherId, classSubjectId, effective section)` triple. Note this key **includes** `teacherId` here (unlike the teacher-scoped endpoint, where it's implicit) — the same subject/section pair taught by two *different* teachers (co-teaching) is fine and not flagged. |
| `Another item earlier in this same request already makes a teacher the class teacher for that section.` | Two rows in the same submission both set `isClassTeacher: true` for the same section — **not** keyed by teacher, since a section can have at most one class teacher regardless of who. |
| `Another item earlier in this same request already assigns this teacher to that time period.` | **2026-08-04** — two rows in the same submission both use the same `timePeriodId` for the same `teacherId`. **Is** keyed by teacher here — two *different* teachers sharing a period across two rows is fine, only the same teacher named twice for one period conflicts. |

### Request-level failures (whole call rejected, nothing created)

| HTTP | `responseCode` | Cause |
|---|---|---|
| 404 | `NotFound` | Academic class `{id}` doesn't exist. |
| 400 | `ValidationError` | `items` missing or empty. |

## Typical UI use

A "Class Routine" grid on the academic class detail page: rows for every subject × section slot
that needs a teacher, with a teacher picker per row (populated from
`GET /api/academicclasses/{id}/subjects` for subject/section options and `GET /api/teachers` for
the teacher picker), plus a period column and one "class teacher" checkbox. Submit the whole grid
in one call; re-render from `created`/`skipped` the same way the teacher-scoped grid does.

**Loading the grid's existing state** (on page load, and after every submit) uses the read-side
sibling added the same day: `GET /api/academicclasses/{id}/teacher-assignments` — see
`Docs/class_teacher_assignments_list_implementation_guide.md`. This endpoint didn't exist before
that round; only the bulk-create POST here did.

## Shared implementation note (for anyone maintaining both endpoints)

`Application/Teachers/TeacherAssignmentBuilder.BuildAsync` is the one place the actual
(teacher, subject, section, period) validation lives — both `TeacherService`
(`AssignClassSubjectAsync`/`AssignClassSubjectBulkAsync`/`AssignClassSubjectBulkEntryAsync`) and
`AcademicClassService.AssignTeachersBulkEntryAsync` call it. A rule change (a new invariant, a
different error message) only needs to happen once.

## No new migration

Same as the teacher-scoped bulk-entry endpoint — this only creates ordinary `TeacherAssignment`
rows through the existing `AddAssignmentAsync` repository method. No new entity, no new column.
