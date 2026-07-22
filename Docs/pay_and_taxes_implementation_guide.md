# Pay & Taxes implementation guide

Adds three pieces on top of the existing Employee/Teacher compensation plan
(`employee_management_implementation_guide.md`): a **monthly** tax breakdown, a
structured **payslip** list/detail (separate from the existing HTML preview), and a
**loans and advances** workflow. Built for the admin-facing employee/teacher profile
pages; the same endpoints work unchanged if/when employee self-service login exists.
**2026-07-21 addendum**: a standardized "Common Salary Components" catalog, a
**Salary Forecast** endpoint (next month's estimated pay), and a composite
**Investment & Tax Planning** endpoint were added on top — see their own
sections below (the last one has its own dedicated guide,
`Docs/investment_and_tax_planning_implementation_guide.md`, since it needed a
full worked example).

All three reuse `TaxCalculator`'s existing annual computation and add
`Application/Payroll/MonthlyBreakdownCalculator.cs`, a pure static helper (same shape
as `TaxCalculator`) that turns a salary's components/deductions into 12 fiscal-month
rows. **Two deliberate simplifications, both flagged in code comments:**

- **Fiscal-month boundaries are approximated** — `FiscalYear.StartDate`..`EndDate` is
  split into 12 equal-length *Gregorian* segments and labeled with the fixed Nepali
  month names in order (Shrawan, Bhadra, Ashwin, Kartik, Mangsir, Poush, Magh, Falgun,
  Chaitra, Baishakh, Jestha, Ashad). There is no Bikram Sambat calendar library in this
  codebase, so a real BS month (29–32 days) isn't computed exactly.
- **Monthly TDS is flat** — `monthTax = annualTax / 12`, every month, not a cumulative
  rest-of-year re-projection recomputed at each month (the way real Nepali payroll
  withholding works). Same "structural, not byte-exact" trade-off `TaxCalculator`
  itself already documents.

Within a month, a `Monthly`-frequency component/deduction repeats at its resolved
period amount every row; an `Annual`-frequency one is spread evenly across all 12
rows; a `OneTime` one (festival bonus, leave encashment, ...) is placed **only** in
the row containing the salary's `EffectiveFromDate` — there's no "date this was
actually paid" field to key off instead.

## Tax Details — monthly breakdown

`GET /api/employees/{id}/salaries/tax-calculation/monthly?fiscalYearId=` (+
`/api/teachers/{id}/...` alias) → `EmployeeMonthlyTaxBreakdownDto`:

```
{ employeeId, salaryId, fiscalYearId, fiscalYearCode,
  taxCalculation: { ...same shape as GET .../salaries/tax-calculation... },
  months: [
    { monthIndex, monthName, periodStartDate, periodEndDate, monthDays,
      incomeLines: [{ code, amount }], deductionLines: [{ code, amount }],
      monthGrossIncome, monthTax, monthNet },
    ... 12 rows
  ] }
```

`monthGrossIncome` sums every income line regardless of `isTaxable` (it's a pay
figure, not a taxable-income figure — different purpose than `taxCalculation.
grossAnnualIncome`, which only sums `isTaxable` components).

## Investment & Tax Planning (2026-07-21)

`GET /api/employees/{id}/salaries/tax-planning?fiscalYearId=` (+
`/api/teachers/{id}/...` alias) → `TaxPlanningDto` — the single composite
response behind the Investment & Tax Planning tab (income lines, the
retirement-fund a/b/c breakdown, insurance lines, assessment type, and the
annual tax calculation with its slab table), replacing whatever combination
of `GET .../salaries/tax-calculation` + `GET .../salaries` + `GET
/api/fiscalyears/{id}` the frontend was previously assembling by hand.
**Full field-by-field reference, a complete worked JSON example, and the
exact UI-element-to-field mapping live in their own dedicated guide:
`Docs/investment_and_tax_planning_implementation_guide.md`.**

## Payslip — list + detail

Separate, structured (non-HTML) path from the existing
`GET .../salaries/payslip-preview` (which stays as-is, HTML template, latest
revision only, still a forward-looking projection built at read time).

**2026-07-21: these two endpoints only ever surface an actually-approved
payslip now.** A month shows up here **only** once its `PayrollRun`/`SalarySlip`
status is `Approved` or `Paid` — a month with no run generated yet, or one still
sitting in `Draft`, returns nothing (list) or `404` (detail) instead of the old
read-time projection. The read-time projection fallback that used to fill in
for un-generated months was removed entirely from this pair of endpoints (it
was also the source of a decimal-precision bug — see below); a forward-looking
estimate is still available from the dedicated `GET .../salary-forecast`
endpoint (see "Salary Forecast" below), which is unaffected. The Tax Details
tab that used to surface `GET .../salaries/tax-calculation/monthly` in the UI
was removed from `PayAndTaxesPanel.jsx` — the endpoint and its
`useGetEmployeeMonthlyTaxBreakdown`/`useGetTeacherMonthlyTaxBreakdown` hooks
still exist, just unused by any tab now.

- `GET /api/employees/{id}/payslips?fiscalYearId=` (+ `Teachers` alias) →
  `List<PayslipSummaryDto>`, one entry per fiscal month with an Approved-or-Paid
  slip: `{ monthIndex, monthLabel ("Bhadra/FY-SAMPLE"), periodStartDate,
  periodEndDate, monthDays, payDays, upl, isProjection }`. `isProjection` is
  always `false` now (kept on the DTO for shape stability, not removed).
  `payDays`/`upl` come from the persisted slip's `PayDays`/`UnpaidLeaveDays`
  (rounded to whole days) — this codebase has no attendance/leave module to
  source finer-grained figures from.
- `GET /api/employees/{id}/payslips/{fiscalYearId}/{monthIndex}` (+ `Teachers`
  alias) → `PayslipDetailDto`: `{ employeeName, employeeCode, jobPositionCode,
  payMonthLabel, monthDays, payDays, upl, incomeLines, grossIncome,
  deductionLines, totalDeduction, netPaid, isProjection }` built from the
  persisted `SalarySlip`'s own lines (`isProjection` always `false`).
  `404` ("No payslip is available for that month yet — payroll must be
  generated and approved first") for any month without an Approved/Paid slip.

**Decimal precision fix (2026-07-21):** every payroll amount is now rounded to
2 decimal places at the point it's computed — `TaxCalculator`
(`ResolveAmount`, per-slab tax, `MonthlyTax`, the retirement exemption, and the
insurance-premium deduction) and `MonthlyBreakdownCalculator` (`MonthTax` and
the Annual-frequency per-month split) all call `Math.Round(value, 2)` instead
of leaving a raw division result (e.g. `annualTax / 12m`) to propagate long
repeating decimals into API responses. `SalarySlipLine`/`SalarySlip` amount
columns were already `decimal(12,2)` at the database level, so persisted
payslips were never affected — the bug only ever showed up in the now-removed
projection path and in the Tax Details/`GrossMonthly`/`NetMonthly` figures,
which are fixed by the same rounding change.

## Loans and Advances

`Domain/Entities/EmployeeLoan.cs` (`SoftDeleteAuditableEntity`, financial-audit
record like `StudentDiscount`/`StudentScholarship`): `EmployeeId`, `LoanTypeCode`
(a Config `DeductionType` code — restricted in the service to `LOAN`/`ADVANCE` only,
via `Domain/Constants/LoanTypeCodes.cs`, not any `DeductionType` code),
`PrincipalAmount`, `EmiAmount`, `RequestedDate`, `StartDate`, `Status`
(`Domain/Enums/LoanStatus`: `PendingApproval`/`Approved`/`Rejected`/`Cancelled`/
`Closed`), `Remarks`. Table `dbo.employee_loans` (FK to `employees`) is created
by migration `20260716094450_added feature of enrollment , fees, payroll etc`.

**No separate repayment ledger table.** Repayment progress is computed, not stored
(`Application/Payroll/LoanCalculator.cs`): `installmentsElapsed` = whole calendar
months between `StartDate` and today, capped at `⌈Principal / EMI⌉`;
`amountRepaid = installmentsElapsed × EMI` (capped at `Principal`).
`EmployeeLoanDto` exposes `amountRepaid`/`remainingBalance`/`isFullyRepaid` as
computed fields. `GET .../loans` opportunistically persists `Status = Closed` for
any `Approved` loan it finds fully repaid — self-healing on every read, no
scheduled job needed.

**"Auto-deduction" is also computed, not a persisted `EmployeeSalaryDeduction`
row** — at payroll-run generation time, `PayrollRunService` folds in any
`Approved` loan's EMI for a given month by checking
`LoanCalculator.IsDueInPeriod(loan, month.periodStartDate)` and writes a
`SalarySlipLine` (`LineType.LoanEmi`) onto the generated slip.
`GetPayslipDetailAsync` itself just reads that persisted line back — it does
not call `IsDueInPeriod` directly. This means a later salary revision (a raise)
needs **no special handling** to "carry forward" the deduction — the loan
lives independently of any `EmployeeSalary` row. **`GetSalaryForecastAsync`
(below) is the one exception that does call `IsDueInPeriod` directly** — a
forecast has no persisted slip to read the EMI line back from, so it resolves
the due-that-month loans itself, the same way the old (now-removed) payslip
projection used to.

Endpoints (`Employees` + `Teachers` aliases throughout):

- `POST /api/employees/{id}/loans` — `{ loanTypeCode, principalAmount, emiAmount,
  startDate, remarks? }` → created `PendingApproval`. Validation: `principalAmount
  > 0`; `0 < emiAmount <= principalAmount`; `loanTypeCode` must be `LOAN` or
  `ADVANCE`.
- `GET /api/employees/{id}/loans` → `List<EmployeeLoanDto>`, every status, newest
  `requestedDate` first.
- `POST /api/employees/{id}/loans/{loanId}/approve` — `{ remarks? }`, only from
  `PendingApproval`.
- `POST /api/employees/{id}/loans/{loanId}/reject` — `{ remarks? }`, only from
  `PendingApproval`.
- `POST /api/employees/{id}/loans/{loanId}/cancel` — `{ remarks? }`, from
  `PendingApproval` or `Approved`.

## Common Salary Components (2026-07-21)

The Compensation Plan form's "Code" dropdowns are sourced entirely from the
Config catalog (`SalaryComponentType` = 1013 for earnings, `DeductionType` =
1014 for deductions — see `Docs/employee_management_implementation_guide.md`);
there's no hardcoded list. Before this date, `HOUSE_RENT_ALLOWANCE`/
`MEDICAL_ALLOWANCE`/`OVERTIME`/`BONUS` weren't seeded options, which pushed
admins toward mistyping them under the catch-all `OTHER_ALLOWANCE` code (visible
in the screenshots that prompted this change). `ConfigCatalogSeeder` now seeds
the standard set an admin actually needs, create-if-missing like every other
option it seeds (won't touch a database that already has its own rows under
these type codes — a dev DB needs the four new rows added by hand, e.g. via
`POST /api/configs`, or by re-running the seeder against a fresh database):

**Earnings (`SalaryComponentType`, catalog 1013)** — `BASIC` (Basic Salary),
`DEARNESS_ALLOWANCE`, `HOUSE_RENT_ALLOWANCE` *(new)*, `TRAVEL_ALLOWANCE`,
`MEDICAL_ALLOWANCE` *(new)*, `FESTIVAL_BONUS` (Festival/Dashain Bonus),
`OVERTIME` *(new)*, `BONUS` *(new)*, plus the pre-existing
`SSF_CONTRIBUTION`/`COMMUNICATION_ALLOWANCE`/`OTHER_ALLOWANCE`/
`LEAVE_ENCASHMENT` (kept — real plans use them).

**Deductions (`DeductionType`, catalog 1014)** — already covered the requested
set with no change needed: `SSF_DEDUCTION` (SSF/PF), `CIT_DEDUCTION` (Citizen
Investment Trust), `LOAN`, `ADVANCE`, `OTHER`. Income Tax (TDS) is
deliberately **not** a `DeductionType` option — it's never a manually-added
line; `TaxCalculator`/`MonthlyBreakdownCalculator`/`PayrollRunService` compute
and attach it automatically (see the Tax Details/Payslip sections above),
exactly like every payslip in this system already worked.

**The net-salary formula was already exactly this** — nothing to change in
`TaxCalculator`/`MonthlyBreakdownCalculator`/`PayrollRunService`, since
"gross − SSF/PF − TDS − other deductions = net" is just "sum every earning
line, subtract every deduction line (which already includes the auto-attached
TDS line and any due loan EMI)," which is what `MonthGrossIncome −
totalMonthDeductions = MonthNet` (and the payroll-run/forecast equivalents)
already computed. Standardizing the catalog is what was actually needed —
admins now build a plan from the same well-known components a Nepali payslip
expects instead of freehand-typing allowance names.

## Salary Forecast (2026-07-21)

`GET /api/employees/{id}/salary-forecast?fiscalYearId=` (+
`/api/teachers/{id}/salary-forecast` alias) → `SalaryForecastDto` — "what will
next month's pay look like," off the employee's **current** salary structure.
Unlike the Payslip endpoints above, this never requires an Approved/Paid
`PayrollRun` to exist — it's an estimate, not a record, so it's built the same
read-time way the payslip projection used to be (reusing
`MonthlyBreakdownCalculator`'s month rows, the flat TDS share, and any due
`Approved` `EmployeeLoan` EMI), just under an honest name instead of
masquerading as a real payslip.

"Next month" = the first fiscal-month row (of the given or current fiscal
year) whose `periodStartDate` hasn't started yet — i.e. the row right after
the one containing today. If today falls inside the fiscal year's last month,
there's no further month left to forecast within it: `404`
("... has no further month to forecast -- pass the next fiscal year's id.") —
pass the next fiscal year's id explicitly once it's been created, same as
every other `fiscalYearId`-scoped endpoint in this feature.

```
{ employeeId, salaryId, fiscalYearId, fiscalYearCode,
  monthIndex, monthLabel ("Bhadra/FY-SAMPLE"), periodStartDate, periodEndDate,
  monthDays,
  incomeLines: [{ code, label, amount }],
  grossSalary,
  deductionLines: [{ code, label, amount }],  // includes "TDS" and any due loan EMI
  totalDeductions,
  netSalary }
```

All amounts are rounded to 2 decimal places (same fix as the Decimal precision
section above — this endpoint is built from the same already-rounded
`MonthlyBreakdownCalculator` rows). Permission: `EMPLOYEE_SALARY_FORECAST` /
`TEACHER_SALARY_FORECAST`, same seeding pattern as every other permission row
in this feature.

## Frontend

`sections/apps/common/CompensationPanel.jsx` was renamed to `PayAndTaxesPanel.jsx`,
each sibling tab its own component file in `sections/apps/common/`. The Tax
Details tab (originally one of these siblings) was later removed from the UI,
then its slot (right after Payslip) was reused for the new Salary Forecast tab
(`SalaryForecastTab.jsx` — see "Salary Forecast" below) — current tabs are
Payslip, Salary Forecast, Compensation Plan, Salary Adjustments, Accounts and
Taxes, Investment & Tax Planning, Loans and Advances. The outer profile-page tab
label changed from "Compensation & Tax" (employee) / "Salary & Tax" (teacher) to
"Pay & Taxes" in `employee-profile.jsx`/`teacher-profile.jsx`. "Accounts and
Taxes" is a read-only display of `Employee.bankName`/`bankAccountNumber`/
`paymentMode` — those fields already existed on `EmployeeDto`/`TeacherDto`, so
this tab needed zero backend change.

**Payslip export**: PDF uses `@react-pdf/renderer` (already a project dependency —
see `sections/apps/invoice/export-pdf/` for the precedent), a real downloaded
file. "Excel" export is a client-side generated **CSV** (no `xlsx`/`exceljs`
dependency exists in this project) — Excel opens a `.csv` directly, so this covers
the common case; ask for a real `.xlsx` via the `xlsx` package if that distinction
matters.

**Salary Forecast**: `useGetEmployeeSalaryForecast`/`useGetTeacherSalaryForecast`
(`api/employee-service.js`/`api/teacher-service.js`, thin `silentError: true` SWR
hooks over `GET .../salary-forecast`) feed their own `SalaryForecastTab.jsx` —
its own fiscal-year selector (independent of the Payslip tab's), a "Forecast —
not yet generated" chip next to the month label, and the same income/deduction
line breakdown style as `PayslipDetailModal`. A 404 (fiscal year's last month
already started) renders as an inline message rather than an error state. Both
hooks are invalidated by `revalidateSalaryAndTax` alongside the tax-calculation
and payslip caches, so approving/rejecting a loan or editing a salary revision
refreshes the forecast too.

**Investment & Tax Planning** (`investment_and_tax_planning_implementation_guide.md`):
`useGetEmployeeTaxPlanning`/`useGetTeacherTaxPlanning` (same
`silentError: true` SWR pattern, `GET .../salaries/tax-planning`) feed the new
`InvestmentTaxPlanningTab.jsx`, which replaces the tab's old hand-assembled
version (three separate hooks — `useGetEmployeeTaxCalculation` + the raw
`salaries` list + the fiscal year's `retirementExemptionCapAmount` — stitched
together inside `PayAndTaxesPanel.jsx` itself). The old
`useGetEmployeeTaxCalculation`/`useGetTeacherTaxCalculation` hooks are still
called, but now only for the Compensation Plan tab's Gross/Net Monthly summary
cards — `PayAndTaxesPanel.jsx` no longer reads their loading/error state itself.
Renders `taxPlanning.fiscalYearCode`, never `fiscalYearId` (the raw-GUID
display the dedicated guide flags as a pre-existing bug), and binds the
Income table's "Type" column to `incomeLines[].valueType` via
`SALARY_VALUE_TYPE_OPTIONS` instead of a hardcoded "Fixed Income" string.

## Known gaps

- No employee self-service login exists yet (`Employee.UserId` unpopulated, no
  `/me` endpoint) — everything here is reached through the existing admin
  employee/teacher profile pages.
- No attendance/leave module — `payDays`/`upl` on the payslip are simplified as
  noted above.
