# Audit fields (`createdBy`/`createdTs`/`updatedBy`/`updatedTs`) + simplified generate response (2026-07-21)

Two unrelated changes shipped together because both came from the same feedback pass. All
responses use the standard envelope `{ responseCode, responseMessage, data }`.

## 1. `POST /api/feeinvoices/generate` — simplified skip reporting

**Problem**: regenerating an already-fully-generated billing period returned one row per skipped
enrollment (`skipped[{ enrollmentId, studentName, reason }]`) — for a school with hundreds of
students already invoiced, that's hundreds of near-identical rows the frontend had no real use
for ("Invoice for this month already exists with status 'Generated'." × 400).

**Fix**: `FeeGenerationResultDto` now returns skip reasons **grouped by count** instead of one row
per enrollment:

```json
{
  "feeGenerationRunId": "GUID",
  "billingYear": 2026,
  "billingMonth": 7,
  "generatedCount": 2,
  "skippedCount": 21,
  "generatedInvoiceIds": ["GUID", "GUID"],
  "skippedSummary": [
    { "reason": "Invoice Already Generated", "count": 18 },
    { "reason": "No Fee Structure Configured", "count": 3 }
  ]
}
```

- `skipped[]` is gone; `skippedSummary[]` replaces it — **breaking change** to the response shape
  (there is no dual-field transition period; the frontend was updated in the same change —
  `GenerateInvoicesModal.jsx`'s result table now renders `reason`/`count` columns instead of
  `studentName`/`reason`, and `FeeGenerationRunDetail.jsx`'s refresh snackbars read
  `skippedCount` directly instead of `skipped.length`).
- Reason strings are now a **fixed, short set** (no interpolated status/detail):
  - `"Invoice Already Generated"` — an invoice for this enrollment/period already exists and
    isn't a Draft (was: `"An invoice for this month already exists with status '...'."`).
  - `"Draft Invoice Already Exists"` — a Draft exists but `regenerateDrafts` wasn't set (was:
    `"A Draft invoice for this month already exists (pass regenerateDrafts to replace it)."`).
  - `"No Fee Structure Configured"` — the class has no `FeeStructure` row.
  - `"Fee Structure Not Active"` — the class's `FeeStructure.Status` isn't `Active`.
- `skippedCount` is still a plain `int` (sum of every bucket) — unchanged in meaning, just no
  longer literally `skipped.length`.
- **Not touched**: `POST /api/feeinvoices/adjustments/bulk` (`BulkFeeAdjustmentResultDto.Skipped`)
  still returns one row per skipped enrollment (`{ enrollmentId, studentName, reason }`) — that
  list is typically small (one class/section at a time) and the per-student breakdown is the
  point there (an admin needs to know *which* students didn't get the bulk charge). Only the
  `generate` endpoint's skip list was ballooning; the two DTOs (`FeeGenerationSkipDto` for bulk
  adjustments, new `FeeGenerationSkipSummaryDto` for generate) are intentionally separate types
  now so this doesn't regress if bulk-adjustment scope grows later.
- Also applies to the refresh endpoints from `Docs/fee_generation_run_and_carry_forward_implementation_guide.md`
  §9 (`POST /api/feegenerationruns/{id}/refresh` and the per-class variant) — both return the same
  `FeeGenerationResultDto`, since they're thin wrappers around the same `GenerateAsync`.

## 2. Audit fields on read DTOs

**Ask**: show who created/last-updated a record and when, on the APIs used for Fee Generation
Runs, Students, Employees, Users, and Payroll Runs — `createdBy`/`createdTs` on list rows, plus
`updatedBy`/`updatedTs` on the single-item ("detail"/GetById) shape.

**Where the data already lived**: every one of these entities already implements
`IAuditableEntity` (`Student`/`Employee`/`PayrollRun`/`FeeGenerationRun` via
`SoftDeleteAuditableEntity`, `ApplicationUser` via `ISoftDeleteAuditableEntity` directly — see
`CLAUDE.md`'s Domain layer conventions) and `ApplicationDbContext.SaveChangesAsync` already
auto-stamps all four columns on every save (never set manually in a handler). This change is
**pure DTO/mapper exposure** — no new columns, no migration, no service logic changes beyond
reading four properties that were already there.

| Feature | List/summary DTO | Detail DTO | Fields added |
|---|---|---|---|
| Fee Generation Runs | `FeeGenerationRunDto` | `FeeGenerationRunDetailDto : FeeGenerationRunDto` | List: `createdBy`, `createdTs` (who first ran generate for the period, and when — same instant as the existing `generatedTs`). Detail adds: `updatedBy`, `updatedTs` (who/when last regenerated/refreshed — `null` until the first regenerate/refresh) |
| Students | `StudentDto` (one shape, shared by list and detail — detail-only fields like `guardians[]` are just left empty on the list) | same class | All four: `createdBy`, `createdTs`, `updatedBy`, `updatedTs` |
| Employees | `EmployeeDto` (shared) | same class | All four |
| Users | `UserDto` (shared) | same class | All four |
| Payroll Runs | `PayrollRunDto` (shared — `slips[]` filled only on detail/generation responses) | same class | All four |

**Why some features get 2 fields on the list and others get all 4**: Fee Generation Runs already
has a genuinely separate list DTO vs. detail DTO (`FeeGenerationRunDetailDto` inherits and adds
fields), so the ask maps directly: base class gets the "created" pair, subclass adds the "updated"
pair. Students/Employees/Users/Payroll Runs all use **one shared DTO class** for both the paged
list and the single-item GetById response (the detail-only fields, where they exist, are just
left empty/default on list rows — an existing pattern in this codebase, not something introduced
here). Forking each of those into separate List/Detail DTO classes just to gate 2 fields would be
a much bigger, riskier change for no real benefit — so for these four, all four audit fields are
added to the one shared DTO and always populated by the mapper, list or detail alike. A list row
carrying `updatedBy`/`updatedTs` it didn't strictly ask for is harmless; splitting a widely-used
shared DTO is not a trivial change.

**Mapper changes** (all follow the same shape — read four properties off the entity, no new
parameters, no new queries):

```csharp
// StudentMapper.ToDto, EmployeeMapper.ToDto, UserMapper.ToDto, PayrollRunMapper.ToDto
CreatedBy = entity.CreatedBy,
CreatedTs = entity.CreatedTs,
UpdatedBy = entity.UpdatedBy,
UpdatedTs = entity.UpdatedTs
```

```csharp
// FeeGenerationRunMapper.ToDto (list) — CreatedBy/CreatedTs only
CreatedBy = run.CreatedBy,
CreatedTs = run.CreatedTs

// FeeGenerationRunMapper.ToDetailDto (detail) — adds UpdatedBy/UpdatedTs from the entity directly
// (not from the already-built summaryDto, which doesn't carry them)
UpdatedBy = run.UpdatedBy,
UpdatedTs = run.UpdatedTs
```

**Type note**: `IAuditableEntity.CreatedTs` is `DateTimeOffset` (not nullable) and `UpdatedTs` is
`DateTimeOffset?` — DTOs use the same types, not `DateTime`, to match exactly what the DB column
stores (avoids a silent UTC-vs-local misread at the API boundary).

## 3. Explicitly out of scope (not touched in this pass)

"All possible APIs" is broad — this pass covered exactly the five named areas above plus their
GetById endpoints. Not touched, and not assumed to be wanted without asking first:

- Every other entity in the system that also has audit columns (Menus, Roles, Configs,
  AppConfigs, AcademicYears/Classes/Sections, Guardians, Teachers, FeeStructures, FeeInvoices
  themselves, Enrollments, Discounts/Scholarships, Calendar events/meetings, ...) — same pattern
  would apply identically (read `CreatedBy`/`CreatedTs`/`UpdatedBy`/`UpdatedTs` off the entity,
  add matching DTO fields, one mapper edit each), but each is its own small PR-sized change and
  there are dozens of them. Ask for a specific feature and it's a fast, mechanical follow-up using
  this same recipe.
- Rendering `createdBy`/`updatedBy` as a resolved display name instead of the raw username string
  that's stored — `CreatedBy`/`UpdatedBy` are `ICurrentUserService.UserName` snapshots (or
  `"system"` for unauthenticated/seeded writes), not a `Guid` FK, so there's no join to a display
  name; if that's wanted, it's a separate, bigger decision (store a name snapshot at write time,
  or resolve it via a lookup at read time).
- No frontend table-column wiring beyond the two `generate`-response call sites that would have
  broken otherwise (`GenerateInvoicesModal.jsx`, `FeeGenerationRunDetail.jsx`'s refresh
  snackbars). The new audit fields are on the wire but no list/detail page renders them yet — add
  columns/rows where wanted.

## 4. Migration

None. No new columns — every field already existed on the entity via `IAuditableEntity`/
`ISoftDeleteAuditableEntity`.
