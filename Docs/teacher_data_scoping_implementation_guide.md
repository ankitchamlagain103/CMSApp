# CMSApp — Teacher Data Scoping: My Students & My Marks Entry (UI)

**2026-08-07.** Per instruction: an employee who is a teacher should see **only their own
students**, and when entering exam marks, **only the subjects they actually teach**. This is a
different kind of "configurable" from the same day's menu-level work (Parts 1-4 in
`role_privilege_escalation_guard_implementation_guide.md`) — those control *which menus/API
routes* a user can reach; this controls *which rows of data* a call returns, scoped off the
caller's own `TeacherAssignment` rows. Nothing here is a new permission — it's server-side data
filtering layered under the existing self-service ("Me") pattern.

## How scope is derived (same everywhere)

A teacher's "scope" is computed fresh on every call from their own `TeacherAssignment` rows
(`IEmployeeRepository.GetAssignmentsAsync`), resolved from the JWT the same way every other "Me"
endpoint in this codebase does (`ResolveCurrentEmployeeIdAsync` — `ICurrentUserService.UserId` →
`Employee.UserId`). Two shapes of scope are used depending on the endpoint:

- **Section scope** (for "my students"): the distinct set of `ClassSectionId`s across all the
  teacher's assignments.
- **Subject scope** (for marks entry): whether the teacher has *any* assignment for the specific
  `ClassSubjectId` an exam/mark belongs to.

A non-teaching employee, or one with no assignments yet, gets an **empty result**, not an error —
same "expected, not exceptional" treatment as every other self-service edge case in this codebase.

## My Students

```
GET /api/students/me            paginated list, same query params as GET /api/students
GET /api/students/me/{id}       single student detail, scoped
```

`GET /api/students/me` accepts the exact same `GetStudentsQuery` params as the admin
`GET /api/students` (search, gradeCode, gender, dateField, etc.) — they still all apply, layered
**on top of** the section restriction, not instead of it. The restriction itself is a new
server-computed-only field, `StudentFilter.ClassSectionIds`, which a caller can never set directly
(there's no query param for it) — `StudentService.GetMyStudentsAsync` is the only code path that
populates it, from the caller's own assignments.

`GET /api/students/me/{id}` reuses the existing `GetStudentByIdAsync` response shape byte-for-byte,
then checks the returned `data.currentEnrollment.classSectionId` against the caller's scope —
`403 FORBIDDEN` `"This student is not in one of your assigned sections."` if it isn't (or if the
student has no current enrollment at all). Looking up a student outside your scope by id, not just
filtering them out of the list, is refused the same way — this isn't just a list-page filter.

Both routes are `DefaultEnabledMenu`-gated (no permission row) — same "self access to your own
scope isn't a privilege a role should need granted" reasoning as every other "Me" endpoint. New
`MY_STUDENTS` sub-menu under `MY_WORKSPACE` (order 6) for sidebar visibility (grant it to a role
via `POST /api/roles/claims` the normal way — the API access itself already works without the
grant, see the "How access control works" note in `employee_self_service_implementation_guide.md`).

## My Marks Entry

```
GET  /api/exams/me                                                     my exams (subject-scoped)
GET  /api/studentexammarks/me/roster?examId=&search=&classSectionId=   my roster for one exam
GET  /api/studentexammarks/me?examId=&enrollmentId=&classSectionId=    my recorded marks
POST /api/studentexammarks/me                                          record a mark (scoped)
PUT  /api/studentexammarks/me/{id}                                     edit a mark (scoped)
POST /api/studentexammarks/me/bulk                                     bulk upsert (scoped)
```

`GET /api/exams/me` is the existing `GET /api/exams?teacherId=` filter (already existed, see
`exam_management_implementation_guide.md` Round 3), just no longer needing the caller to pass
`teacherId` themselves — it's resolved from the JWT instead of trusted from the query string.

The other five are new. Each one loads the target `Exam` (from `examId`, or — for update — from
the `StudentExamMark` row's own `ExamId`) and checks the caller has a `TeacherAssignment` for that
exam's `ClassSubjectId`; if not, `403 FORBIDDEN` `"You are not assigned to teach this subject."`
and the underlying read/write never runs. This is enforced on the **write path**, not just the
read/roster path — a teacher can't record or edit a mark for a subject outside their own
assignments even by guessing/crafting an `examId`, not merely have it hidden from their own
roster view. `examId` is **required** on `GET /api/studentexammarks/me` (unlike the admin
`GET /api/studentexammarks`, where it's optional) since it's what the check runs against.

Request/response bodies for all five are byte-for-byte identical to their admin counterparts
(`CreateStudentExamMarkCommand`, `UpdateStudentExamMarkCommand`, `BulkUpsertStudentExamMarksCommand`,
`ExamMarkRosterItemDto`, `StudentExamMarkDto`) — see `exam_management_implementation_guide.md` and
`exam_routine_and_marks_configuration_implementation_guide.md` for those shapes; nothing new to
look up. All `DefaultEnabledMenu`-gated, no permission row.

## What this does *not* change

- The **admin routes** (`GET /api/students`, `GET /api/studentexammarks`, `POST /api/studentexammarks`,
  etc.) are completely unchanged — still unscoped, still require their existing permission grants.
  This round is additive: a new, narrower self-service surface, not a behavior change to the
  existing admin one.
- **No "deny" beyond scope** — a teacher who somehow holds a broader permission grant (e.g. the
  full `STUDENT_LIST` permission on top of being a teacher) can still use the unscoped admin
  routes; this feature only constrains the *new* `me/...` routes. If a role should never see the
  admin routes at all, that's a grant decision (don't give that role `STUDENT_LIST`), unrelated to
  this feature.
- **Section-scoped subjects, unaffected**: a `ClassSubject` that's section-scoped (see
  `filters_update.md`) was already only teachable by an assignment naming that section — this
  feature doesn't change subject-eligibility rules at all, it only adds the *caller-resolution*
  layer on top of checks that already existed.

## Backend notes

- `StudentService`/`ExamService` both gained a new `ICurrentUserService` constructor dependency
  (neither injected it before this round) — already registered in DI, no `DependencyInjection.cs`
  change needed.
- `StudentFilter.ClassSectionIds` (new `List<Guid>`, server-computed only) and the matching
  `StudentRepository.GetPagedByFilterAsync` predicate are additive — every existing caller that
  never sets it behaves exactly as before.
- `ExamService.IsTeacherAssignedToClassSubjectAsync` (new private helper) mirrors the existing
  `IsTeacherAssignedToExam` static helper's logic, just callable against a bare `classSubjectId`
  (needed since the write-path checks run before the fuller `Exam` object is always in scope, e.g.
  for update it's read off `mark.Exam.ClassSubjectId` after loading the mark by id).

## No migration needed

Pure service/repository-layer logic over existing `TeacherAssignment`/`Enrollment`/`Exam`/
`StudentExamMark` data — no new columns or tables.
