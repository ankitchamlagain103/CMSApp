# Filters update (2026-07-15)

Adds richer filtering to every paginated list endpoint in the Student Management module (and the
Menus catalog), and redesigns part of the class/subject/teacher-assignment scoping model that had
grown awkward. This doc is the changelog + reference for both; the per-endpoint query strings are
also folded into `student_management_implementation_guide.md` and `UI-Implementation-Guide.md`.

**Needs a migration** (user-owned, not run from here) — see [Migration required](#migration-required) at the bottom.

## Why a shared filter object instead of more query parameters

Every list repository method used to take its filters as bare positional parameters
(`GetPagedByFilterAsync(string search, int pageNumber, int pageSize, ...)`). Students and Teachers
now need 7-10 filter fields each, which would make an unreadable, error-prone parameter list (easy
to swap two `Guid?` arguments of the same type by accident). Since `Domain/Interfaces` repository
contracts can't reference Application's `Get*Query` classes (Domain has zero dependencies on
Application), each entity that needed more than 2-3 filters got a small Domain-owned filter class
under `Domain/Common/Filters/`:

```
Domain/Common/Filters/
├── StudentFilter.cs
├── TeacherFilter.cs
├── AcademicClassFilter.cs
├── EnrollmentFilter.cs
├── GuardianFilter.cs
├── AcademicYearFilter.cs
└── MenuFilter.cs
```

Each `Application/<Feature>/Queries/Get<Feature>sQuery.cs` builds one of these before calling the
repository (`Application/<Feature>Service.cs`, in the `Get<Feature>sAsync` method) — the query
class is still the HTTP-facing shape (`[FromQuery]` model), the filter class is the
repository-facing shape. This is the same "give it a class once a bare parameter list gets
unwieldy" rule the codebase already applies to Commands/Queries, just extended to repository
filter parameters.

## Students — `GET /api/students`

`Application/Students/Queries/GetStudentsQuery.cs`:

| Field | Type | Behavior |
|---|---|---|
| `search` | string | unchanged — matches `firstName`/`lastName`/`admissionNo`, case-insensitive |
| `phone` | string | new — partial, case-insensitive match on `phone` |
| `gradeCode` | string | new — student has an **Enrolled**-status enrollment in this grade |
| `academicYearId` | Guid? | new — student has an Enrolled-status enrollment in this academic year |
| `classSectionId` | Guid? | new — student has an Enrolled-status enrollment in this exact section |
| `status` | RecordStatus? | new — `1` Active / `2` Inactive |
| `gender` | Gender? | new |
| `dateField` | StudentDateField | new — `0` CreatedDate (default) / `1` EnrollmentDate, picks which column `fromDate`/`toDate` applies to |
| `fromDate` / `toDate` | DateTime? | new — inclusive range on whichever column `dateField` selects |

**`gradeCode`/`academicYearId`/`classSectionId` are a "who is currently in Grade X of year Y"
snapshot** — they match against the student's `Enrolled`-status enrollment only, not full
enrollment history (a transferred/withdrawn/completed enrollment in that grade won't match). If
you need history-wide grade/year filtering, query `GET /api/enrollments` instead, which returns
one row per enrollment regardless of status.

`EnrollmentDate` as a `dateField` option matches if **any** of the student's enrollments falls in
the range (not just the active one) — it answers "who enrolled somewhere between these dates,"
which is useful for admissions reporting across years.

## Teachers — `GET /api/teachers`

`Application/Teachers/Queries/GetTeachersQuery.cs`:

| Field | Type | Behavior |
|---|---|---|
| `search` | string | unchanged — `firstName`/`lastName`/`employeeNo` |
| `phone` | string | new — partial match |
| `qualificationCode` | string | new — teacher has a `TeacherQualification` row with this code (catalog 1005) |
| `status` | RecordStatus? | new |
| `dateField` | TeacherDateField | new — `0` CreatedDate (default) / `1` JoiningDate |
| `fromDate` / `toDate` | DateTime? | new |

`Teacher.JoiningDate` already existed as a real column, so unlike the original ask ("teacher
filters should be the same shape as student filters"), the date-field choice for teachers is
`CreatedDate`/`JoiningDate` rather than an enrollment-style date — `JoiningDate` is the teacher
equivalent of a student's `EnrollmentDate`.

## Other list endpoints

| Endpoint | New filters |
|---|---|
| `GET /api/academicclasses` | `gradeCode`, `status` (existing: `academicYearId`) |
| `GET /api/enrollments` | `academicYearId`, `status`, `dateField` (`0` EnrollmentDate default / `1` CreatedDate) + `fromDate`/`toDate` (existing: `studentId`, `academicClassId`, `classSectionId`) |
| `GET /api/guardians` | `search` (first/last name), `phone`, `fromDate`/`toDate` (on `createdTs`). **Breaking**: was `GetGuardiansAsync(int page, int pageSize, ...)` with no query class at all — now `GetGuardiansAsync(GetGuardiansQuery query, ...)`, matching every other list endpoint's shape |
| `GET /api/academicyears` | `search` (code/name), `isCurrent`, `status`. **Breaking**: same signature change as Guardians — was raw `(int page, int pageSize)`, now `GetAcademicYearsQuery` |
| `GET /api/menus` | `search` (code/displayName), `parentId` (exact, direct children only), `isHidden` (existing: `menuType`, `menuFor`) |

`IGuardianService.GetGuardiansAsync` and `IAcademicYearService.GetAcademicYearsAsync` changed
signature (query object instead of `(page, pageSize)`) — anything calling these interfaces
directly (there are no other callers in this codebase besides the controllers, which were updated)
needs to change too.

## Class/subject/teacher-assignment scoping redesign

### The problem

`ClassSubject.ClassSectionId` (nullable: null = offered to every section of the class, set =
offered only in that section) and `TeacherAssignment.ClassSectionId` (nullable, independently:
null = assignment covers every section, set = one specific section) are two separate nullable
fields that have to agree with each other, cross-checked only in application code
(`TeacherService.AssignClassSubjectAsync`). Concretely:

1. **A real bug**: the old validation only rejected an assignment whose `ClassSectionId` named a
   *different* section than the subject's own scope — it did **not** require the assignment to
   name the *same* section. So assigning a teacher to a section-scoped subject (e.g. "Chess Club,
   offered only in Section A") with `classSectionId: null` silently succeeded, producing an
   assignment that reads as "teaches every section" for a subject that structurally can't be
   taught outside Section A.
2. **No DB backing** for two rules that were app-only: "a mandatory subject is always class-wide"
   and "a subject appears once class-wide OR once per section, never mixed" (the existing unique
   index can't enforce either, since Postgres treats `NULL` as distinct in unique indexes).
3. **Implicit state**: API consumers had to infer "is this class-wide or section-scoped" from
   `classSectionId`'s nullability rather than reading an explicit field.

A more drastic fix (splitting `ClassSubject` into two tables — one strictly class-wide, one
strictly section-scoped — and making `TeacherAssignment` reference one or the other via a
DB-enforced exclusive-or FK) was considered and rejected for this pass: it would ripple into
`Enrollment`/`EnrollmentSubject` and `StudentService.BuildCurrentEnrollmentAsync` (which today
treats a student's effective subject list as one flat set regardless of which table a row came
from), turning this into a much larger, riskier change for marginal extra safety. It's recorded
here as a future option if the current fix ever proves insufficient.

### What changed

1. **`TeacherService.AssignClassSubjectAsync` now derives the assignment's section from the
   subject whenever the subject is section-scoped**, instead of trusting the caller to repeat it
   correctly: if `classSubject.ClassSectionId` is set, the assignment's effective section is
   always that value (a caller-supplied `classSectionId` is only checked for *mismatch*, no longer
   required or trusted as the source of truth). This closes bug #1 above by construction — there
   is no longer a code path that produces a class-wide-looking assignment for a section-scoped
   subject.
2. **`Infrastructure/Persistence/EntityConfigurations/ClassSubjectConfiguration.cs`** adds:
   - A `CHECK` constraint (`ck_class_subjects_mandatory_classwide`): `is_mandatory = false OR
     class_section_id IS NULL`. DB-enforces "mandatory subjects are always class-wide."
   - A partial unique index (`ix_class_subjects_classwide_unique`) on `(academic_class_id,
     subject_code) WHERE class_section_id IS NULL`. DB-enforces "at most one class-wide row per
     subject per class" — the half of the exclusivity rule a plain index can express. The other
     half ("this subject code has zero rows in the other partition") still can't be expressed as a
     single index/CHECK without an `EXCLUDE` constraint or trigger, so
     `AcademicClassService.AssignSubjectAsync`'s existing `GetClassSubjectRowsByCodeAsync`
     pre-check remains the enforcement for that part.
3. **`Domain/Enums/SubjectScope.cs`** (`ClassWide = 0`, `Section = 1`) — a new, explicit
   discriminator. Not a stored column (computed by the mapper from `ClassSectionId.HasValue`), so
   no schema impact of its own. Added to `ClassSubjectDto.Scope`, `TeacherAssignmentDto.Scope`, and
   `TeacherServiceHistoryDto.Scope` — closes gap #3 above.

### What deliberately did not change

- `TeacherAssignment.ClassSectionId` still exists as its own nullable column, because it serves a
  real, distinct purpose beyond mirroring `ClassSubject`'s scope: when a subject **is** class-wide,
  different teachers can still be assigned per section (e.g. Math is class-wide, but Section A and
  Section B have different Math teachers). Removing the field would remove that feature. The fix
  above only forces the field's value when the underlying subject is itself section-scoped, which
  is the situation where two independent sources of truth for the same fact caused a real bug.
- The "subject code exists once class-wide XOR N times per-section, never mixed" cross-partition
  rule is now half DB-enforced (see above) but still relies on the service pre-check for full
  coverage — documented as a known, accepted gap rather than silently claiming it's fully solved.

## Migration required

`ClassSubjectConfiguration` changes require a new EF Core migration (not created here — migrations
are user-owned in this repo). It needs to add:

- `CHECK (is_mandatory = false OR class_section_id IS NULL)` on `dbo.class_subjects`
- A unique index on `(academic_class_id, subject_code)` filtered `WHERE class_section_id IS NULL`

Verified via `dotnet ef migrations has-pending-model-changes` (read-only — confirms the model
changed since the last migration without creating one). No other change in this pass touches the
schema — every new filter field reads existing columns.

Before applying against a populated database: any existing `class_subjects` rows that already
violate either new constraint (a mandatory row with a `class_section_id` set, or more than one
class-wide row for the same `(academic_class_id, subject_code)`) will make the migration fail to
apply. Per the existing seed data / service validation, these shouldn't exist, but worth a
one-time check on a live DB before running `database update`.
