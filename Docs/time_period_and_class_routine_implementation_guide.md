# Time Periods & Class Routine Mapping

**2026-08-03.** Real, class-scoped period scheduling — replacing the short-lived
`ConfigTypeCodes.ClassPeriod` Config catalog (introduced and retired the same day). Full reference
for the UI team.

## Why this isn't a Config catalog

The first cut of "class period timing" (same day, earlier) put periods in the generic
`ConfigType`/`Config` dropdown catalog — `Code`/`Label` plus three free-form `AdditionalValue`
string slots. That works fine for a flat list of options nobody needs to relate to anything else
(document types, guardian relationships, ...). It does **not** work here, because the real
requirement is a relationship, not a list: **different classes run different period structures**
— Nursery might use six 30-minute periods, Grade Ten eight 45-minute periods with a different
lunch break. A Config option has no owner; it can't say "this period belongs to these classes and
not those." Modeling that in Config would mean smuggling a class-id list into a string field —
exactly the kind of hack this codebase's own conventions (see `LeaveType`/`GradeScale`'s doc
comments) already warn against.

So this is two real tables instead:

- **`TimePeriod`** (`dbo.time_periods`) — the definition: a named slot with a start time, an end
  time, and a `Kind` (`Period` or `Break`). Global, soft-deleted, not owned by any class.
- **`ClassTimePeriod`** (`dbo.class_time_periods`) — the mapping: which `TimePeriod` rows a given
  `AcademicClass` actually uses. This is the relationship Config couldn't express. Hard-deleted
  pure link row, same convention as `TeacherAssignment`/`ClassSubject`.

`Exam.TimePeriodId` and `TeacherAssignment.TimePeriodId` are both real foreign keys into
`TimePeriod` now (previously a Config-code string on each). Picking a period for either one is
validated against **both** tables: the period must be a `Period` (not a `Break`), and it must be
mapped to the specific class the exam/assignment belongs to.

## Entities

```
TimePeriod
  Id, Name, StartTime (HH:mm:ss), EndTime (HH:mm:ss), Kind (Period=0/Break=1), Order

ClassTimePeriod
  Id, AcademicClassId, TimePeriodId    -- unique (AcademicClassId, TimePeriodId)
```

`Kind` matters for two different things:
- **Building the routine**: a class's mapped periods should include its breaks too, so a
  timetable screen can render the whole day in order — mapping a `Break`-kind `TimePeriod` to a
  class is normal and expected.
- **Picking a period for an exam or a teaching assignment**: only `Period`-kind rows are valid —
  you can't schedule an exam or a teacher's lesson during a break. This check lives in
  `ExamService.ResolveExamTimesAsync` and `TeacherService.BuildAssignmentAsync`.

## Endpoints (`/api/timeperiods`)

Standard admin CRUD for the definitions:

| Method | Route | Notes |
|---|---|---|
| `POST` | `/api/timeperiods` | `{ name, startTime, endTime, kind, order }` — `kind`: `0` Period / `1` Break. `name` unique. |
| `GET` | `/api/timeperiods?page=&pageSize=` | Paged, ordered by `Order`. |
| `GET` | `/api/timeperiods/{id}` | |
| `PUT` | `/api/timeperiods/{id}` | Same shape as create. |
| `DELETE` | `/api/timeperiods/{id}` | `409 Conflict` while mapped to any class, or referenced by an `Exam`/`TeacherAssignment`. |

Class mapping — the bulk input the whole feature exists for:

| Method | Route | Notes |
|---|---|---|
| `POST` | `/api/timeperiods/map` | `{ academicClassIds: [...], timePeriodIds: [...] }` — creates the **cross-product** of every listed class × every listed period in one call. |
| `GET` | `/api/timeperiods/map/{academicClassId}` | Every period (and break) mapped to one class, ordered by the period's own `Order`. |
| `DELETE` | `/api/timeperiods/map/{academicClassId}/{timePeriodId}` | Unmap one pair. |

### The bulk mapping call, worked example

"Nursery through Five run a shorter day; Six through Twelve run the full 8-period day" is two
calls:

```json
POST /api/timeperiods/map
{ "academicClassIds": ["<Nursery id>", "<LKG id>", "...", "<Five id>"], "timePeriodIds": ["<Period 1 id>", "...", "<Period 6 id>", "<Short Break id>"] }
```

```json
POST /api/timeperiods/map
{ "academicClassIds": ["<Six id>", "...", "<Twelve id>"], "timePeriodIds": ["<Period 1 id>", "...", "<Period 8 id>", "<Short Break id>", "<Lunch Break id>"] }
```

Response is skip-list style, same convention as every other bulk endpoint in this codebase
(`BulkUpsertStudentExamMarksAsync`, `CreateBulkFeeAdjustmentCommand`, ...) — never an
all-or-nothing reject:

```json
{
  "created": [
    { "id": "...", "academicClassId": "...", "gradeCode": "SIX", "timePeriodId": "...", "timePeriodName": "Period 1", "startTime": "08:00:00", "endTime": "08:45:00", "kind": 0 }
  ],
  "skipped": [
    { "academicClassId": "...", "timePeriodId": "...", "reason": "Already mapped." }
  ]
}
```

An unknown class id or period id in the request also lands in `skipped` with a `"was not found"`
reason, rather than failing the whole call — one bad id in a large bulk request shouldn't block
the rest.

## Consumers

### `Exam.TimePeriodId` (unchanged shape from the earlier Config-based cut, different underlying type)

`POST/PUT /api/exams` and each item in `PUT /api/exams/routine` still take **either**
`timePeriodId` (leave `startTime`/`endTime` null) **or** raw `startTime`+`endTime` (leave
`timePeriodId` null) — exactly one path, validator-enforced. What changed is the type:
`timePeriodId` is a `TimePeriod` id (`Guid`), not a Config code string.

```json
{ "examTermId": "…", "classSubjectId": "…", "examDate": "2026-08-10", "timePeriodId": "<Period 3 id>", "startTime": null, "endTime": null, "remarks": null }
```

| Failure | Message |
|---|---|
| Neither `timePeriodId` nor both `startTime`/`endTime` given | "Either TimePeriodId or both StartTime and EndTime must be provided." |
| `timePeriodId` doesn't match a real `TimePeriod` | "Time period with id 'X' was not found." |
| `timePeriodId` matches a `Break`-kind row | "'Lunch Break' is a break, not a teaching period." |
| `timePeriodId` isn't mapped to the exam's class | "'Period 3' is not mapped to this class -- map it first via the Time Periods screen." |
| Resolved `endTime` not after `startTime` | "EndTime must be after StartTime." |

`ExamDto` exposes `timePeriodId` + `timePeriodName` (resolved server-side, no second lookup
needed) alongside the always-populated `startTime`/`endTime`.

### `TeacherAssignment.TimePeriodId` (new)

`POST /api/teachers/{id}/assignments` and `POST /api/teachers/{id}/assignments/bulk` both take an
optional `timePeriodId` — which routine slot this teacher teaches this class/subject in. Same two
checks as the exam side (must be a `Period`, must be mapped to the assignment's class), applied in
the shared `TeacherService.BuildAssignmentAsync` helper both endpoints call.

```json
POST /api/teachers/{id}/assignments
{ "classSubjectId": "…", "classSectionId": "…", "isClassTeacher": false, "timePeriodId": "<Period 4 id>" }
```

`TeacherAssignmentDto` gained `timePeriodId`/`timePeriodName`, same resolved-server-side
convention as the exam DTO.

**Not built**: a day-of-week timetable. `TimePeriodId` names one slot ("Period 4"), not "Period 4
on Mondays and Wednesdays" — this codebase has no day-of-week concept for assignments. If a real
weekly grid (subject/teacher varying by day) is ever needed, that's a separate, larger feature on
top of this one, not a replacement for it.

## Seed data

`TimePeriodSeeder` (runs after `LeaveTypeSeeder` in `Program.cs`) seeds one illustrative full
school day, create-if-missing by `Name`:

| Name | Start | End | Kind |
|---|---|---|---|
| Period 1 | 08:00 | 08:45 | Period |
| Period 2 | 08:45 | 09:30 | Period |
| Period 3 | 09:30 | 10:15 | Period |
| Period 4 | 10:15 | 11:00 | Period |
| Short Break | 11:00 | 11:15 | Break |
| Period 5 | 11:15 | 12:00 | Period |
| Period 6 | 12:00 | 12:45 | Period |
| Lunch Break | 12:45 | 13:15 | Break |
| Period 7 | 13:15 | 14:00 | Period |
| Period 8 | 14:00 | 14:45 | Period |

**Deliberately does not seed any `ClassTimePeriod` mapping** — which classes use which periods is
a real school-specific decision (the whole point of this feature), made by the admin via
`POST /api/timeperiods/map`, not assumed by a seeder.

## Menu

New `TIME_PERIOD_LIST` sub-menu under the existing `SETUP` main menu (`/apps/time-period/list`),
alongside `YEAR_LIST`/`CLASS_LIST`/`FEE_STRUCTURE_LIST`/etc. Permissions:
`TIME_PERIOD_CREATE`/`_DETAIL`/`_UPDATE`/`_DELETE`/`_MAP`/`_CLASS_LIST`/`_UNMAP`.

## What replaced what (for anyone who saw the earlier Config-based cut)

| Before (same-day, since removed) | Now |
|---|---|
| `ConfigTypeCodes.ClassPeriod` (TypeCode `1024`), seeded as Config options | `Domain/Entities/TimePeriod`, a real table |
| `Exam.PeriodCode` / `TeacherAssignment.PeriodCode` (string, Config code) | `Exam.TimePeriodId` / `TeacherAssignment.TimePeriodId` (`Guid?`, real FK) |
| No class-scoping concept at all | `ClassTimePeriod` mapping, bulk-created via `POST /api/timeperiods/map` |
| `Domain/Constants/ClassPeriodTypeCodes` (`"PERIOD"`/`"BREAK"` strings in `Config.AdditionalValue3`) | `Domain/Enums/PeriodKind` (real enum column on `TimePeriod`) |

An already-seeded database that ran the Config-based cut has orphaned `ConfigType`/`Config` rows
under TypeCode `1024` — harmless, nothing reads them anymore; delete them by hand if you want the
Config admin screen tidy.

## Migration

**Needs a migration** — this supersedes, not adds to, the migration already applied for the
previous (Config-based) cut of this feature
(`Infrastructure/Migrations/20260803083606_update in setup for class assignment.cs`, which added
`dbo.teacher_assignments.period_code varchar(100)`). A dev database that already ran that
migration needs a follow-up migration, not a fresh one from scratch:

- New table `dbo.time_periods` (`id`, `name` unique, `start_time`, `end_time`, `kind` int,
  `order`, plus the usual soft-delete audit columns).
- New table `dbo.class_time_periods` (`id`, `academic_class_id` FK → `academic_classes.id`
  Restrict, `time_period_id` FK → `time_periods.id` Restrict, unique
  `(academic_class_id, time_period_id)`, plus audit columns).
- `dbo.teacher_assignments`: **drop** `period_code varchar(100)` (from the migration above), **add**
  `time_period_id uuid NULL` with an FK to `time_periods.id` (`ON DELETE RESTRICT`).
- `dbo.exams`: **drop** `period_code varchar(100)` (from the earlier
  `20260730093356_changes in in exams for period code.cs` migration), **add**
  `time_period_id uuid NULL` with an FK to `time_periods.id` (`ON DELETE RESTRICT`).

Until this migration is applied, every `TimePeriod`/`ClassTimePeriod` read/write 500s (tables
don't exist), and every `Exam`/`TeacherAssignment` create/update 500s (EF selects/writes columns
that no longer match the database's `period_code` shape).
