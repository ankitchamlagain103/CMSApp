# Class Teacher Assignments — List (GET, from the Academic Class side)

2026-08-04, same day as `class_teacher_assignment_bulk_entry_implementation_guide.md`. Adds the
missing **read** endpoint for that feature: `POST /api/academicclasses/{id}/teacher-assignments/bulk-entry`
(and the older `POST /api/teachers/{id}/assignments`/`/bulk`/`/bulk-entry` create endpoints) let an
admin *create* `TeacherAssignment` rows, but there was no way to ask "who is actually teaching
this class right now" without going teacher-by-teacher through `GET /api/teachers/{id}/assignments`.
This closes that gap with one class-scoped listing call.

## Endpoint

```
GET /api/academicclasses/{id}/teacher-assignments
GET /api/academicclasses/{id}/teacher-assignments?classSectionId={sectionId}
```

`{id}` — the `AcademicClass`'s id (same id space as `/api/academicclasses/{id}`,
`/api/academicclasses/{id}/subjects`, `/api/academicclasses/{id}/sections`,
`/api/academicclasses/{id}/teacher-assignments/bulk-entry`).

`classSectionId` (optional query param) — narrows the result to assignments for one specific
section of the class. Omit it to see every assignment across the whole class (every section, plus
every class-wide-subject assignment). If supplied, it must be a real section belonging to this
class or the call 404s (same "section must belong to this class" guard the other section-scoped
endpoints already use).

Permission: `CLASS_TEACHER_ASSIGNMENT_LIST` (seeded under `CLASS_LIST`, action
`GetTeacherAssignments` on the `AcademicClasses` controller). SuperAdmin has it by default.

## Response body

```json
{
  "responseCode": "Success",
  "responseMessage": null,
  "data": [
    {
      "id": "d4e5...",
      "teacherId": "b2c3...-teacher-guid-1",
      "teacherName": "Anita Sharma",
      "employeeCode": "EMP2026007",
      "classSubjectId": "6f1a...-subject-guid",
      "academicClassId": "7d8e...",
      "subjectCode": "MATH",
      "classSectionId": "3c2b...-section-guid",
      "sectionCode": "A",
      "scope": 1,
      "isClassTeacher": true,
      "timePeriodId": "9e77...-period-guid",
      "timePeriodName": "Period 3"
    },
    {
      "id": "f6a7...",
      "teacherId": "a4d5...-teacher-guid-2",
      "teacherName": "Ramesh Thapa",
      "employeeCode": "EMP2026012",
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
  ]
}
```

`data` is a flat, unpaged list (`ClassTeacherAssignmentDto`, `Application/AcademicClasses/Dtos/`)
— a class routine is a bounded, small dataset (a handful of subjects × sections per class), same
reasoning `GetClassSubjectsAsync`/`GetSectionsAsync` already use for their own unpaged lists.
Sorted by `subjectCode`, then teacher first name.

### Field reference

| Field | Notes |
|---|---|
| `id` | The `TeacherAssignment` row's own id — pass this to `DELETE /api/teachers/{teacherId}/assignments/{id}` to remove it. |
| `teacherId` / `teacherName` / `employeeCode` | **New vs. `Application.Teachers.Dtos.TeacherAssignmentDto`** — a class-scoped listing needs the teacher's name up front (the viewer is looking at the class, not a specific teacher's profile), so this DTO adds them instead of just `teacherId`. |
| `classSubjectId` / `subjectCode` / `academicClassId` | The subject this row is for. |
| `classSectionId` / `sectionCode` | The section this row is for. As of the 2026-08-04 validation round, every *new* assignment always has a section — but a legacy row created before that round may still show `null`/`"ClassWide"` here (`scope: 0`) if it predates the requirement. |
| `scope` | `SubjectScope` enum: `0` = ClassWide (legacy shape only, see above), `1` = Section. |
| `isClassTeacher` | Whether this row makes the teacher the class teacher of `classSectionId`. At most one `true` row per section. |
| `timePeriodId` / `timePeriodName` | Which routine slot this row occupies, if one was set. |

### Request-level failures

| HTTP | `responseCode` | Cause |
|---|---|---|
| 404 | `NotFound` | Academic class `{id}` doesn't exist. |
| 404 | `NotFound` | `classSectionId` was supplied but doesn't resolve to a section of this class ("Section was not found on this class."). |

## Typical UI use

The "Class Routine" grid described in `class_teacher_assignment_bulk_entry_implementation_guide.md`
now has a real data source to render on page load and after every bulk-entry submit: call this
endpoint (optionally scoped to the section currently selected in the UI) to populate the grid,
then use `id` on each row to wire up a per-row "remove" action against the existing
`DELETE /api/teachers/{teacherId}/assignments/{assignmentId}` endpoint. No new delete endpoint was
needed — that one already existed and already works from either side of the relationship, since it
only needs the assignment's own `id`.

## Implementation notes

- New `ITeacherRepository.GetAssignmentsByAcademicClassAsync(academicClassId, classSectionId)`
  (`TeacherRepository`) — a direct query filtered on `TeacherAssignment.ClassSubject.AcademicClassId`
  (and optionally `ClassSectionId`), `Include`ing `Teacher.Employee`/`ClassSubject`/`ClassSection`/
  `TimePeriod` so `AcademicClassMapper.ToTeacherAssignmentDto` can build a complete row without a
  second round-trip. Deliberately a new method rather than reusing the existing
  `GetAssignmentsByClassSubjectIdsAsync` (used by the student-profile "who teaches this subject"
  lookup) — that one's `Include` shape is tailored to its own caller and doesn't load
  `ClassSection`/`TimePeriod`.
- `AcademicClassMapper.ToTeacherAssignmentDto` (new) maps `TeacherAssignment` → `ClassTeacherAssignmentDto`,
  with its own small `BuildFullName` helper (same "mappers stay self-contained" convention every
  other mapper in this codebase already follows, rather than reaching into `TeacherService`'s or
  `EmployeeMapper`'s copies).
- `AcademicClassService.GetTeacherAssignmentsAsync` is the read-side sibling to
  `AssignTeachersBulkEntryAsync`, sitting right next to it in the file.

## No new migration

Read-only endpoint over existing data — no entity, column, or index changes.
