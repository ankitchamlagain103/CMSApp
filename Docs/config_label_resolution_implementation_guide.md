# Config label resolution (2026-08-05)

Application-wide sweep: every GET endpoint that returns a Config-catalog-backed code (`GradeCode`,
`SectionCode`, `SubjectCode`, `JobPositionCode`, `ComponentCode`, ...) now also returns that code's
resolved, human-readable label in a sibling `...Label` field. The goal is to remove the frontend's
need to call `GET /api/configs/dropdown/{typeCode}` just to map codes to labels for display.

## What changed for the UI team

**Before**: a list/detail page loaded its data, then fired one or more
`GET /api/configs/dropdown/{typeCode}` calls per distinct type code appearing in the response, and
joined codes to labels client-side. A single page (e.g. Employee Profile) could fire 10-15 of these
dropdown calls on load.

**After**: every field named `<Something>Code` on a read DTO is now paired with a
`<Something>Label` field in the same response, already resolved server-side. No extra call is
needed to display a page — bind directly to the `Label` field.

**`GET /api/configs/dropdown/{typeCode}` still exists and is still correct to call** — but only for
one purpose now: **populating a dropdown/select control in a create or edit form**, where the UI
needs the *full list of options* for that catalog (to let the user pick one), not just the label
for a single already-known code. Do not call it to "look up a label for a code you already have" —
that label is already on the response.

### Rule of thumb for the UI team going forward

- Rendering a list, detail, or profile page with codes coming back from the API → use the paired
  `Label` field already on the DTO. Do not call the dropdown endpoint.
- Building/editing a form where the user needs to choose from an option list (grade, section,
  subject, job position, discount type, ...) → call `GET /api/configs/dropdown/{typeCode}` to
  populate the `<select>`, exactly as before.

## Pattern used (backend reference, for future features)

Every fix in this sweep follows the same shape, already established piecemeal in earlier rounds
(Fee/Payroll labels, 2026-07-19) and now applied application-wide:

1. **`Application/Common/Helpers/ConfigLabelHelper.cs`** is the shared utility:
   - `BuildLabelMap(IReadOnlyList<Config>)` → `Dictionary<string, string>` (`Code` → `Label`).
   - `MergeLabelMap(Dictionary<string,string>, IReadOnlyList<Config>)` — merges a second catalog's
     options into an existing map (used when a DTO needs codes from more than one `ConfigType`).
   - `Resolve(IReadOnlyDictionary<string,string> labelsByCode, string code)` — null-safe on both
     arguments; falls back to the raw code itself if the map is null or the code isn't found, so a
     `Label` field is never blank.
2. **DTO**: every `XxxCode` property gets a sibling `XxxLabel` property immediately after it.
3. **Mapper**: the static `ToDto`/`ToXxxDto` method gains an optional trailing
   `IReadOnlyDictionary<string, string> labelsByCode = null` parameter and calls
   `ConfigLabelHelper.Resolve(labelsByCode, code)` for each label field. Defaulting to `null` means
   an un-migrated caller still compiles and simply gets the raw code as the label (never breaks).
4. **Service**: a private `LoadXxxLabelMapAsync(CancellationToken)` helper builds/merges the
   relevant catalog(s) via `_unitOfWork.Configs.GetByTypeCodeAsync(typeCode, cancellationToken)`,
   called **once per request** (not once per row in a list) and passed into every mapper call in
   that method.

This mirrors the codebase's already-documented `LoadFeeLabelMapAsync`/`LoadPayrollLabelMapAsync`/
`LoadOrgLabelMapAsync`-style helpers — the sweep didn't introduce a new pattern, it extended the
existing one to every feature that hadn't adopted it yet.

## Full list of DTOs/endpoints fixed in this sweep

### Employees (`Application/Employees/`)
- `EmployeeDto` — `EmployeeCategoryLabel`, `JobPositionLabel`, `BranchLabel`, `ProvinceLabel`,
  `LevelLabel`, `DistrictLabel`, `LocalLevelLabel` (`GET /api/employees`, `GET /api/employees/{id}`,
  create/update responses).
- `EmployeeProfileDto` — `LevelLabel`, `JobPositionLabel`, `EmployeeCategoryLabel`, `BranchLabel`,
  `ProvinceLabel` (`GET /api/employees/{id}/profile`).
- `EmployeeQualificationDto` — `QualificationLabel` (`GET /api/employees/{id}/qualifications`).
- `EmployeeDocumentDto` — `DocumentTypeLabel` (`GET /api/employees/{id}/documents`).
- `PayslipDetailDto` — `JobPositionLabel` (`GET /api/employees/{id}/payslips/{fiscalYearId}/{monthIndex}`).

### Teachers (`Application/Teachers/`)
- `TeacherDto` — `JobPositionLabel` (`GET /api/teachers`, `GET /api/teachers/{id}`).
- `TeacherServiceHistoryDto` — `GradeLabel`, `SectionLabel`, `SubjectLabel` (nested in
  `GET /api/teachers/{id}`'s `ServiceHistory`).
- `TeacherAssignmentDto` — `SubjectLabel`, `SectionLabel` (`GET /api/teachers/{id}/assignments`,
  bulk-assignment responses). The ID-card preview template's `JobPositionCode` placeholder value
  now also uses the resolved label instead of the raw code.

### Students (`Application/Students/`)
- `StudentDocumentDto` — `DocumentTypeLabel` (`GET /api/students/{id}/documents`).

### AcademicClasses (`Application/AcademicClasses/`)
- `AcademicClassDto` — `GradeLabel`.
- `ClassSectionDto` — `SectionLabel`.
- `ClassSubjectDto` — `SubjectLabel`, `SectionLabel`.
- `ClassTeacherAssignmentDto` — `SubjectLabel`, `SectionLabel` (class-scoped bulk teacher
  assignment listing, `GET /api/academicclasses/{id}/teacher-assignments`).

### Enrollments (`Application/Enrollments/`)
- `EnrollmentDto` — `GradeLabel`, `SectionLabel`.
- `EnrollmentSubjectDto` — `SubjectLabel`.
- `EnrollmentFeeStructureDto` — `GradeLabel`.
- `AwardSummaryDto` — `TypeLabel` (discount/scholarship summary endpoints).

### Fees / Fee Rules / Fee Invoices / Fee Generation Runs
- `FeeRuleDto` — `AcademicClassGradeLabel`, `FeeCategoryLabel`.
- `FeeStructureDto` — `GradeLabel`.
- `FeeInvoiceDto` — `GradeLabel`, `SectionLabel` (now resolved on **every** list/get/statement
  endpoint, including the main paged `GET /api/feeinvoices` list, which previously resolved no
  labels at all on this DTO).
- `FeeStatementDto`, `FeeAccountStatementDto`, `FeeStudentSearchResultDto` — `GradeLabel`,
  `SectionLabel`.
- `FeeAdjustmentDto` — `GradeLabel`, `SectionLabel` (in addition to its pre-existing
  `AdjustmentTypeLabel`/`FeeCategoryLabel`).
- `FeeGenerationClassGroupDto`, `FeeGenerationClassSummaryDto` — `GradeLabel`.
- `FeeGenerationStudentGroupDto` — `SectionLabel`.

### Payroll Runs (`Application/PayrollRuns/`)
- `SalarySlipLineDto` — `ComponentLabel`, resolved from the merged
  SalaryComponentType/DeductionType/SalaryAdjustmentType/InsuranceType catalog (the loader was
  extended to include InsuranceType, matching Employees' equivalent). Threaded through every
  `PayrollRunDto`/`SalarySlipDto` read and write endpoint (generate, refresh, list, get, approve,
  mark-paid, cancel, get slip, cancel/approve/regenerate slip, add/update/remove slip line).

### Exams (`Application/Exams/`)
- `ExamDto` — `SubjectLabel`, `GradeLabel`.
- `StudentResultDto` / `StudentResultDetailDto` — `GradeLabel`, `SectionLabel`.
- `ExamResultSubjectDto` — `SubjectLabel`.
- `StudentExamMarkDto` / `StudentExamMarkByStudentItemDto` — `SubjectLabel`.
- Also resolved inside `ExamMarkRosterItemDto.Mark` (nested `StudentExamMarkDto`).

### Promotions & Time Periods
- `StudentPromotionDto` — `FromGradeLabel`, `FromSectionLabel`, `ToGradeLabel`, `ToSectionLabel`.
- `ClassTimePeriodDto` — `GradeLabel`.

## Known gap, deliberately not resolved in this sweep: Calendar `ProvinceCode`/`BranchCode`

`CalendarEventDto.ProvinceCode`/`BranchCode` (used to scope a `PublicHoliday` event to a province
or branch) look like Config-backed codes by naming convention (matching the `Province`/`Branch`
catalogs used elsewhere, e.g. on `Employee`), **but they are not actually validated against any
Config catalog anywhere in the codebase** — `CreateCalendarEventCommandValidator`/
`UpdateCalendarEventCommandValidator` only enforce `MaximumLength(100)`, and `CalendarService` never
calls `CodeExistsAsync` for either field. This is a pre-existing gap unrelated to this sweep, not
something introduced by it.

**This was deliberately left alone rather than wired up to `ConfigTypeCodes.Province`/`Branch`**:
doing so would imply these fields are validated the same way `Employee.ProvinceCode`/`BranchCode`
are, which isn't true — a caller could currently set an arbitrary string here that resolving against
that catalog would silently either (a) fail to find, degrading to the raw string with no visible
difference, or (b) coincidentally match a real Province/Branch code for the wrong reason. Adding
real validation is a separate, larger change (touching the validators and `CalendarService`) that
should be a deliberate decision, not a side effect of a label-resolution sweep. **Flagging this
explicitly rather than silently leaving it inconsistent with every other Province/Branch field in
the system** — if/when `CalendarEvent.ProvinceCode`/`BranchCode` should be real Config-validated
fields, that's a follow-up task, and label resolution can be added at the same time.

## Endpoints intentionally not touched

- `GET /api/configs/dropdown/{typeCode}` itself (and its `parentCode`/`search` params) — unchanged,
  still the correct call for populating a dropdown.
- Admin CRUD on `ConfigType`/`Config` themselves (`GET /api/configs/types`, `GET /api/configs/{id}`,
  etc.) — these already return the config's own `Code`/`Label` directly, there's nothing to resolve.
- Fields that were already resolved before this sweep (established in the 2026-07-19 Fee/Payroll
  labels round: `FeeStructureItemDto.FeeCategoryLabel`, `StudentDiscountDto.DiscountTypeLabel`,
  `StudentScholarshipDto.ScholarshipTypeLabel`, `SalaryComponentDto.ComponentLabel`,
  `SalaryDeductionDto.DeductionLabel`, `InsurancePremiumDto.InsuranceTypeLabel`,
  `EmployeeLoanDto.LoanTypeLabel`, `SalaryAdjustmentDto.AdjustmentTypeLabel`,
  `MonthlyLineItemDto.Label`) — untouched, already correct.
