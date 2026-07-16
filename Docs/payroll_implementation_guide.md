# Fiscal Years & Tax Slabs (2026-07-15, salary moved to Employee same day)

Configurable income-tax slabs, scoped to a payroll-specific fiscal year. Follows every convention
in `UI-Implementation-Guide.md` (envelope, camelCase, Bearer token, permission-gated). **Needs a
migration** — see the note at the bottom.

**Salary structure, the compensation-plan model (components/deductions/insurance), and the full
tax calculation now live in `Docs/employee_management_implementation_guide.md`** — salary moved
from `Teacher` to `Employee` the same day this feature was built, so every staff member (not just
teachers) can be paid. This doc covers only `FiscalYear`/`TaxSlab`, which didn't change shape.

**Scope**: tax slab storage only. No payslip generation, no disbursement/payment tracking — see
the employee management guide for what "tax calculation" scope does cover.

## Why a separate Fiscal Year (not the existing Academic Year)

Nepal's government fiscal year (roughly mid-Shrawan to mid-Ashad, ~mid-July to mid-July) does not
line up with this school's `AcademicYear` (the seeded sample data runs ~April-March). Reusing
`AcademicYear` for tax scoping would silently misalign payroll with the real tax year the moment a
school's academic calendar differs from the government's fiscal calendar — so `FiscalYear` is its
own entity, structurally identical to `AcademicYear` (same single-`IsCurrent` invariant: promoting
one demotes all others) but scoped purely to payroll.

## Fiscal Years & Tax Slabs — `/api/fiscalyears`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/fiscalyears` | `{ "code": "2083/84", "name": "FY 2083/84", "startDate": "2026-07-16", "endDate": "2027-07-15", "isCurrent": true }` | `code` unique (≤20); `endDate` > `startDate`; `isCurrent: true` un-flags every other fiscal year |
| `GET /api/fiscalyears?page=1&pageSize=20` | | Newest `startDate` first |
| `GET /api/fiscalyears/{id}` | | |
| `PUT /api/fiscalyears/{id}` | `{ "name", "startDate", "endDate", "isCurrent", "status" }` | `code` immutable |
| `DELETE /api/fiscalyears/{id}` | | Soft delete; `409` while the year still has tax slabs |
| `POST /api/fiscalyears/{id}/taxslabs` | `{ "assessmentType": 1, "minAmount": 0, "maxAmount": 500000, "taxRate": 0.01, "slabOrder": 1 }` | `assessmentType` `1` Individual / `2` Couple. `taxRate` is a **fraction** (0.01 = 1%), not a percentage. `maxAmount` null = no upper bound (the top slab — only the last slab for an assessment type should omit it) |
| `GET /api/fiscalyears/{id}/taxslabs` | | Ordered by assessment type, then `slabOrder` |
| `PUT /api/fiscalyears/{id}/taxslabs/{taxSlabId}` | `{ "minAmount", "maxAmount", "taxRate", "slabOrder" }` | `assessmentType` immutable — remove and re-add to move a slab between Individual/Couple |
| `DELETE /api/fiscalyears/{id}/taxslabs/{taxSlabId}` | | Hard delete |

`FiscalYearDto`: `{ "id", "code", "name", "startDate", "endDate", "isCurrent", "status", "retirementExemptionCapAmount" }` — the last field is the configurable "C" in the retirement-fund "least of three" tax exemption rule (500,000 in the reference payslip example this feature was built from), used by the tax calculation described in the employee management guide.
`TaxSlabDto`: `{ "id", "fiscalYearId", "assessmentType", "minAmount", "maxAmount", "taxRate", "slabOrder" }`.

## How the slab calculation works

Standard Nepali payroll approach (`Application/Payroll/TaxCalculator.Calculate`, pure/static, no
I/O): walk the fiscal year's `TaxSlab` rows **in order** for the given assessment type, taxing
only the portion of income that falls within each slab's own `[minAmount, maxAmount)` range, sum
the per-slab tax into an annual total, then divide by 12 for the monthly figure. This is why
`TaxSlab.MaxAmount` is nullable — the top bracket has no ceiling — and why slabs must be added in
ascending, non-overlapping order per assessment type (nothing currently validates non-overlap
across slabs beyond the DB `CHECK (max_amount IS NULL OR max_amount > min_amount)` on each
individual row — be careful when adding slabs by hand). `TaxCalculator.CalculateFromSalary` builds
on this with the gross-income/retirement-exemption/insurance-deduction orchestration — see the
employee management guide.

## Seeded example data — verify before real use

`Infrastructure/Persistence/DataSeeder/PayrollSeeder.cs` seeds **one placeholder** fiscal year
(`FY-SAMPLE`) with an illustrative Individual/Couple slab set (~1/10/20/30/36% brackets, Couple
thresholds a step higher than Individual) plus a `retirementExemptionCapAmount` of 500,000, so the
feature has something to compute against out of the box. **These are not guaranteed to match the
current government of Nepal budget** — tax law changes yearly, and this project's own knowledge of
the exact current-fiscal-year figures may be stale by the time you run this. Treat it exactly like
the `Jwt:Key`/`Smtp:*`/`Seed:*` placeholders in `appsettings.json`: replace/verify before relying on
it for real payroll, via `POST/PUT/DELETE /api/fiscalyears/{id}/taxslabs` (and `PUT
/api/fiscalyears/{id}` for the retirement cap). This is the entire point of the feature being
configurable rather than hardcoded.

## Migration required

New tables: `dbo.fiscal_years` (unique index on `code`, gains `retirement_exemption_cap_amount`),
`dbo.tax_slabs` (FK to `fiscal_years`, `CHECK (max_amount IS NULL OR max_amount > min_amount)`,
unique index on `(fiscal_year_id, assessment_type, slab_order)`). Salary-related tables are
covered in `Docs/employee_management_implementation_guide.md`. Not created here — run
`dotnet ef migrations add` yourself.
