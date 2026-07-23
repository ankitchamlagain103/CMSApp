# Employee Management & Component-Based Payroll (2026-07-15)

Generalizes HR from "teachers only" to every staff member (teacher, principal, accountant,
receptionist, librarian, IT officer, driver, security guard, office assistant, cleaner, office
help), and redesigns salary from two flat numbers into a real compensation-plan model with a full
Nepal-style tax computation. Follows every convention in `UI-Implementation-Guide.md` (envelope,
camelCase, Bearer token, permission-gated). **Needs a migration** — see the note at the bottom.

## Core idea: Employee first, Teacher is a thin profile

Every staff member is an `Employee` (`/api/employees`) — identity, HR fields (category, job
position, employment status), bank payment details, and (2026-07-23) "Accounts and Codes"
statutory identifiers (PAN/Provident Fund/SSF/CIT/Gratuity numbers — see below). `Teacher`
(`/api/teachers`, unchanged routes) is only teaching-specific data (`teachingLicenseNo`,
`experienceYears`, `specialization`) attached to an Employee via a **shared primary key**:
`Teacher.Id` is always equal to its owning `Employee.Id`. This is what let `TeacherAssignment`
stay **completely unchanged** — its `teacherId` FK still means exactly what it meant before, it
just now also happens to be an Employee id. **Qualifications and Documents moved off `Teacher`
entirely on 2026-07-23** (`Employee.Qualifications`/`Employee.Documents` — see
`employee_documents_and_qualifications_implementation_guide.md`) since neither was ever actually
teaching-specific; `TeacherAssignment` is the one child record that's genuinely about teaching and
stayed on `Teacher`.

Two ways to end up with a teacher profile:
- `POST /api/teachers` — the common "hire a teacher" path, creates the Employee row (category
  hardcoded `ACADEMIC`) and the Teacher profile row together in one call.
- `POST /api/employees/{id}/teacher-profile` — promotes an *existing* employee (e.g. an Office
  Assistant who becomes Vice Principal and now also teaches). Requires `employeeCategoryCode =
  ACADEMIC` and `jobPositionCode` one of `TEACHER`/`PRINCIPAL`/`VICE_PRINCIPAL`; `409` if a profile
  already exists.

## Employees — `/api/employees`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/employees` | `{ "employeeCode": null, "firstName", "middleName", "lastName", "gender": 0, "dateOfBirth", "email", "phone", "joinDate", "employeeCategoryCode": "ADMINISTRATION", "jobPositionCode": "RECEPTIONIST", "bankName", "bankAccountNumber", "paymentMode": 1, "panNumber", "providentFundNumber", "ssfNumber", "citNumber", "gratuityNumber" }` | `employeeCode` optional — blank = auto-generated `EMP{year}{seq}` (one sequence shared across every employee type, including teachers). `employeeCategoryCode`/`jobPositionCode` validated against catalogs `1011`/`1012`. `paymentMode`: `1` BankDeposit / `2` Cash / `3` Cheque. The five "Accounts and Codes" fields (2026-07-23) are all optional free-form strings, ≤50 chars, no format enforced |
| `GET /api/employees?page=1&pageSize=20&search=&phone=&employeeCategoryCode=&jobPositionCode=&employmentStatus=&gender=&dateField=&fromDate=&toDate=` | | All filters optional, same shape as the Student/Teacher list filters |
| `GET /api/employees/{id}` | | `hasTeacherProfile` in the response tells you whether `/api/teachers/{id}` also resolves |
| `PUT /api/employees/{id}` | same body plus `employmentStatus` (see enum below); `employeeCode` immutable | |
| `DELETE /api/employees/{id}` | | Soft delete; `409` if the employee has a teacher profile with class assignments, or has any salary records |
| `POST /api/employees/{id}/teacher-profile` | `{ "teachingLicenseNo", "experienceYears", "specialization" }` | See eligibility rule above |
| `POST/DELETE/GET /api/employees/{id}/qualifications` | `{ "qualificationCode": "MASTERS", "courseName", "institution", "completionYear", "score", "remarks" }` | Catalog `1005`. Full reference: `employee_documents_and_qualifications_implementation_guide.md` |
| `POST/GET /api/employees/{id}/documents` · `GET …/documents/{documentId}/download` · `DELETE …/documents/{documentId}` | Upload is **multipart/form-data** | PDF/JPG/JPEG/PNG ≤10 MB, type from catalog `1006`. Full reference: `employee_documents_and_qualifications_implementation_guide.md` |

`EmploymentStatus` enum: `1` Active, `2` OnLeave, `3` Suspended, `4` Resigned, `5` Terminated, `6`
Retired.

**"Accounts and Codes"** (2026-07-23): `panNumber` (Permanent Account Number, e.g.
`"119175732"`), `providentFundNumber`, `ssfNumber`, `citNumber`, `gratuityNumber` — statutory/
scheme identifiers distinct from `bankName`/`bankAccountNumber` (payment routing). All optional,
no format validated.

`EmployeeDto`: `{ "id", "userId", "employeeCode", "firstName", "middleName", "lastName", "gender", "dateOfBirth", "email", "phone", "joinDate", "employeeCategoryCode", "jobPositionCode", "employmentStatus", "bankName", "bankAccountNumber", "paymentMode", "hasTeacherProfile" }`. `userId` is reserved for a future employee-login feature — nothing sets it today (same "records, not accounts" stance as students/teachers always had).

## Compensation plan — `/api/employees/{id}/salaries`

Salary is no longer two flat numbers (`basicSalary`/`allowances`). It's a set of **named line
items** on each salary revision:

- **Components** (income) — `componentCode` (Config catalog `1013`: `BASIC`, `SSF_CONTRIBUTION`,
  `COMMUNICATION_ALLOWANCE`, `DEARNESS_ALLOWANCE`, `TRAVEL_ALLOWANCE`, `OTHER_ALLOWANCE`,
  `FESTIVAL_BONUS`, `LEAVE_ENCASHMENT`, admin-extensible), `valueType` (`1` Fixed / `2`
  Percentage — a Percentage component resolves against the sibling `BASIC` component's own
  amount, e.g. "SSF Contribution = 20% of Basic"), `value`, `frequencyType` (`1` Monthly / `2`
  Annual / `3` OneTime — admin-set per row, not fixed by code, since schools bill things
  differently), `isTaxable` (feeds gross income), `isRetirementContribution` (feeds the tax
  exemption below).
- **Deductions** — same shape (`deductionCode`, catalog `1014`: `SSF_DEDUCTION`, `LOAN`,
  `ADVANCE`, `OTHER`), minus `isTaxable` (deductions reduce take-home, they were never gross).
- **Insurance premiums** — `insuranceTypeCode` (catalog `1015`: `LIFE`/`HEALTH`/`HOUSING`, each
  with a configured tax-deduction cap) + `annualPremiumAmount`.

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/employees/{id}/salaries` | `{ "effectiveFromDate", "assessmentType": 1, "components": [ { "componentCode": "BASIC", "valueType": 1, "value": 18000, "frequencyType": 1, "isTaxable": true, "isRetirementContribution": false }, { "componentCode": "SSF_CONTRIBUTION", "valueType": 2, "value": 20, "frequencyType": 1, "isTaxable": true, "isRetirementContribution": true } ], "deductions": [ { "deductionCode": "SSF_DEDUCTION", "valueType": 2, "value": 31, "frequencyType": 1, "isRetirementContribution": true } ], "insurancePremiums": [] }` | One row per revision — a raise is a **new** row with a later `effectiveFromDate`, never an edit to the old one. `assessmentType`: `1` Individual / `2` Couple. `409` if a row already exists for that exact date |
| `GET /api/employees/{id}/salaries` | | Full history with line items, newest `effectiveFromDate` first |
| `POST/DELETE /api/employees/{id}/salaries/{salaryId}/components[/{componentId}]` | `{ "componentCode", "valueType", "value", "frequencyType", "isTaxable", "isRetirementContribution" }` | Edit an existing revision's line items one at a time instead of recreating it |
| `POST/DELETE /api/employees/{id}/salaries/{salaryId}/deductions[/{deductionId}]` | `{ "deductionCode", "valueType", "value", "frequencyType", "isRetirementContribution" }` | |
| `POST/DELETE /api/employees/{id}/salaries/{salaryId}/insurance-premiums[/{premiumId}]` | `{ "insuranceTypeCode", "annualPremiumAmount" }` | |

`/api/teachers/{id}/salaries` still works — a thin alias (`TeacherService` forwards to the same
`IEmployeeService` methods using the teacher's own id, which via the shared-PK trick *is* its
Employee id).

## Tax calculation

**`GET /api/employees/{id}/salaries/tax-calculation?fiscalYearId=` was removed 2026-07-23** — use
`GET /api/employees/{id}/salaries/tax-planning?fiscalYearId=` instead (`Docs/investment_and_tax_planning_implementation_guide.md`),
which returns this exact same `taxCalculation` block alongside the retirement-fund/insurance
breakdown in one call. The Teachers alias, `GET /api/teachers/{id}/salaries/tax-calculation`, is
**unaffected and still works** (kept by request) — it forwards to the same
`EmployeeService.GetCurrentSalaryTaxCalculationAsync` this section describes, which was not
removed, only its direct Employees-side route. That method also still backs the computation below
internally.

`GET .../salaries/tax-calculation?fiscalYearId=` (optional — defaults to whichever `FiscalYear` is
`isCurrent`) runs the full "Investment and Tax Planning" computation:

1. **Gross annual income** — every `isTaxable` component, annualized by its `frequencyType`
   (Monthly × 12, Annual/OneTime × 1). Percentage components resolve against `BASIC` first.
2. **Retirement-fund exemption** ("least of three", Nepal Income Tax Act) — `A` = every
   component/deduction flagged `isRetirementContribution`, annualized the same way; `B` = gross
   annual income ÷ 3; `C` = the fiscal year's configured `retirementExemptionCapAmount`. Exemption
   = `min(A, B, C)`.
3. **Insurance deduction** — each declared premium capped at its `InsuranceType`'s configured
   limit, summed.
4. **Taxable income** = gross − exemption − insurance deduction (floored at 0) → the existing
   progressive `TaxSlab` bracket walker.

Response (`EmployeeTaxCalculationDto`):
```json
{
  "employeeId": "…", "salaryId": "…", "fiscalYearId": "…", "fiscalYearCode": "FY-SAMPLE",
  "grossMonthly": 50000,
  "taxCalculation": {
    "grossAnnualIncome": 600000, "retirementContributionAnnual": 66065.63,
    "retirementExemption": 66065.63, "insuranceDeduction": 0,
    "annualTaxableIncome": 533934.37, "annualTax": …, "monthlyTax": …,
    "breakdown": [ { "minAmount": 0, "maxAmount": 500000, "taxRate": 0.01, "taxableAmountInSlab": 500000, "taxForSlab": 5000 }, … ]
  },
  "netMonthly": …
}
```

**This is a faithful structural translation of the rule, not a guaranteed byte-exact match to any
specific payslip** — which components/deductions an admin flags `isTaxable`/
`isRetirementContribution` is a real per-school modeling choice, same as every other
"configurable, not hardcoded" rule in this codebase.

## Deliberately out of scope

- Payslip generation / PDF export, actual payment disbursement tracking.
- Remote-area tax exemption (Nepal gives an additional exemption for staff in designated remote
  areas) — narrow, employee-specific, not modeled yet.
- Insurance deduction caps and the retirement exemption cap are seeded illustrative values — see
  `Docs/payroll_implementation_guide.md`'s "verify before real use" note, same caution as the tax
  slabs.

## Payslip preview

`GET /api/employees/{id}/salaries/payslip-preview?fiscalYearId=` (and the
`/api/teachers/{id}/salaries/payslip-preview` alias) renders an admin-configurable HTML template
using this employee's latest salary revision + tax calculation — see
`Docs/document_preview_implementation_guide.md` for the full mechanism and placeholder-token
catalog.

## Migration required

New table `dbo.employees` (unique `employee_code`; partial-unique `user_id` where not null).
`dbo.teachers` loses `employee_no`/`first_name`/`middle_name`/`last_name`/`email`/`phone`/
`joining_date`/`status`/its own soft-delete columns, gains `teaching_license_no`/
`experience_years`/`specialization`, and its PK becomes FK-linked to `employees.id` (shared
primary key — no separate identity column). `dbo.teacher_salaries` is replaced by
`dbo.employee_salaries` (FK to `employees`, not `teachers`) plus three new child tables:
`dbo.employee_salary_components`, `dbo.employee_salary_deductions`,
`dbo.employee_insurance_premiums`. `dbo.fiscal_years` gains `retirement_exemption_cap_amount`.
`dbo.teacher_assignments` is **unchanged**. `dbo.teacher_qualifications`/`dbo.teacher_documents`
were originally unchanged too, but **as of 2026-07-23 are renamed** to
`dbo.employee_qualifications`/`dbo.employee_documents` (column `teacher_id` → `employee_id`, FK
repointed at `employees.id`) — see `employee_documents_and_qualifications_implementation_guide.md`
for that migration's exact shape. Also as of 2026-07-23: `dbo.employees` gains five nullable
`varchar(50)` columns (`pan_number`, `provident_fund_number`, `ssf_number`, `cit_number`,
`gratuity_number`).

**Data migration, not just schema DDL**: existing `teachers` rows need a matching `employees` row
inserted first (reusing their data), with the **same id** preserved as the new `employees.id`, so
`teacher_assignments` keeps resolving with zero data changes to it, and the renamed
`employee_qualifications`/`employee_documents` tables' existing `teacher_id` values are already
correct `employee_id` values (same shared-PK reasoning) — only the column/constraint needs
renaming, not the data itself. Not created here, per this repo's convention — run
`dotnet ef migrations add` yourself.
