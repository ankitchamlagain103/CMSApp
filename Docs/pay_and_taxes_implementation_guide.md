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

**2026-07-23 addendum**: the frontend's Compensation Plan and Investment & Tax
Planning tabs were redesigned, the Annual Forecast tab was replaced by a new
Tax Details tab (against the `GET .../salaries/tax-details` endpoint
documented above), and every tab's data now fetches only once it's actually
clicked instead of all at once on page load — see the updated "Frontend"
section below. None of this changes any backend endpoint or DTO documented
above; it's presentation-only.

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

## Tax Details — spreadsheet grid (2026-07-23)

`GET /api/employees/{id}/salaries/tax-details?fiscalYearId=` (+
`/api/teachers/{id}/...` alias) → `TaxDetailsGridDto` — a flat, spreadsheet-row
alternative to the `.../tax-calculation/monthly` shape above, purpose-built to
match a reference HRMS's Tax Details table 1:1 (one row per `Particulars`
line, one column per fiscal month) instead of the nested `months[]` structure.
This is a **separate, additive endpoint** — `.../tax-calculation/monthly`,
`EmployeeMonthlyTaxBreakdownDto`, and its hooks are untouched.

```jsonc
{
  "list": [
    { "rowNumber": 1, "isHeader": true, "isBold": false, "isTab": false,
      "particulars": "Incomes", "description": null, "total": 0,
      "month1Amount": 0, "month2Amount": 0, /* ... month3..month12Amount */ },
    { "rowNumber": 2, "isHeader": false, "isBold": false, "isTab": false,
      "particulars": "Basic Salary", "description": "Fixed Income",
      "total": 216000.00, "month1Amount": 18000.00, /* ... one amount per month */ },
    // ... one row per income component, then "Annual Income Forecast" (header),
    // "Retirement Fund" (header), "SSF Deduction", the a)/b)/c)/"Min of a, b or c"
    // rows, "Annual Adjusted Taxable Amount" (header), one row per tax slab that
    // has income in it ("SST 0%" / "TDS 10%" / ... -- see below), "Total SST for
    // the Year" / "Total TDS for the Year" (headers), "SST/TDS Paid in Past
    // Month", "Remaining SST/TDS in 12 month", "SST/TDS this Month"
  ],
  "name": "Ankit Chamlagain", "gender": "Male", "taxPaidAs": "Couple",
  "isHandicapped": false,
  "month1IsForecast": false, "month2IsForecast": true, /* ... month3..12IsForecast */
}
```

**Field notes:**

- `isBold`/`isTab` are always `false` from this backend today — kept on the row
  shape only so the consuming grid component's contract doesn't need a follow-up
  change if bold/indented rows are ever introduced; don't build UI logic that
  expects them to vary yet.
- `isHandicapped` is **not modeled anywhere in this codebase** (no
  disability/handicapped flag on `Employee`, no additional-exemption rule in
  `TaxCalculator`) — always `false`. Shown only because the reference UI's
  config panel displays it.
- `monthNIsForecast` follows the same rule `GetAnnualForecastAsync` already
  uses: `false` (Actual) when a real Approved/Paid `SalarySlip` exists for that
  fiscal month, `true` (Forecast) otherwise.
- **The retirement-fund a)/b)/c)/min rows, "Annual Adjusted Taxable Amount",
  the per-slab SST/TDS rows, and every "Total/Paid/Remaining/this Month" row
  are populated in exactly one month column — the "assessment month"** (the
  fiscal month containing today, Nepal time; a fiscal year that doesn't contain
  today falls back to month 1 if entirely future, month 12 if entirely past) —
  and `0` everywhere else. These are a *current status* snapshot, not something
  meaningful to project across all 12 months (same reasoning
  `GetAnnualForecastAsync` already documents for its a/b/c/min rows).
- **Slab rows are dynamic, not a fixed two-row shape**: one row per `TaxSlab`
  that actually has taxable income in it (whatever the fiscal year's real
  slab configuration is), labeled `"SST "` for the first slab (index 0, by
  construction the Social Security Tax bracket — shows `0%` when the SSF
  waiver applies even though the configured rate is nonzero) and `"TDS "` for
  every slab after it. `description` on a slab row is that slab's
  `taxableAmountInSlab`, comma-formatted.
- **"Paid in Past Month" is TDS only, sourced from real disbursed history**:
  for every actual month strictly before the assessment month, its own
  snapshotted salary revision is re-run through `TaxCalculator` (same
  technique `GetAnnualForecastAsync` already uses for its a/b/c/min rows) to
  get that month's own SST/TDS split, then summed. **`SST Paid in Past Month`
  is always `0`** — a documented simplification: this codebase's persisted
  `SalarySlip` only stores one combined `TDS` tax line, not a separate SST
  line, so there's nothing to sum historically; in practice this is rarely
  wrong, since the SST bracket is usually waived to `0` for an SSF-contributing
  employee anyway.
- **`SST/TDS this Month` amortizes the remaining liability over the rest of
  the year** (`(Total for Year − Paid in Past Month) / (12 − assessmentMonth + 1)`),
  not the flat `annualTax / 12` used everywhere else in this codebase
  (payslips, payroll-run slips, monthly-breakdown projections). This is a
  more accurate "how much should be withheld this month, given what's already
  been withheld" figure, deliberately scoped to just this one grid — the flat
  `MonthlyTax`/`monthTax` used elsewhere is unchanged.
- New permissions: `EMPLOYEE_TAX_DETAILS_GRID`, `TEACHER_TAX_DETAILS_GRID`.

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
each sibling tab its own component file in `sections/apps/common/`. The
original nested-`months[]` Tax Details tab (`.../tax-calculation/monthly`) was
removed from the UI once, then **reintroduced (2026-07-23) against the flat
spreadsheet-grid endpoint** (`GET .../salaries/tax-details`, `TaxDetailsGridDto`
— see its own section above) as `TaxDetailsTab.jsx`, replacing the tab that had
briefly taken its slot, Annual Forecast (`AnnualForecastTab.jsx` — deleted; its
backend, `GET .../salaries/annual-forecast`, is untouched and still documented
in its own dedicated guide, just no longer surfaced by any tab here). **Current
tabs, in order: Payslip, Tax Details, Compensation Plan, Salary Adjustments,
Accounts and Taxes, Investment & Tax Planning, Loans and Advances.** The outer
profile-page tab label changed from "Compensation & Tax" (employee) / "Salary &
Tax" (teacher) to "Pay & Taxes" in
`employee-profile.jsx`/`teacher-profile.jsx`. "Accounts and Taxes" is a
read-only display of `Employee.bankName`/`bankAccountNumber`/`paymentMode` —
those fields already existed on `EmployeeDto`/`TeacherDto`, so this tab needed
zero backend change.

**Tabs fetch on click, not all at once (2026-07-23).** `activeTab` used to be
`PayAndTaxesPanel.jsx`'s own internal `useState` — meaning every sibling tab's
SWR hook fired the moment the profile page mounted, regardless of which tab was
actually showing. `activeTab`/`onActiveTabChange` are now controlled props
instead, owned by `EmployeeCompensation.jsx`/`TeacherSalary.jsx` (the two
wrappers that actually call the data-fetching hooks), and every hook there is
gated on `activeTab === '<tab>'` — `useGetEmployeeSalaries`/`useGetEmployeeLoans`
(no `enabled` option) by passing `null` as the id argument when their tab isn't
active, `useGetEmployeePayslips`/`useGetEmployeeTaxPlanning`/
`useGetEmployeeTaxDetails` (which do take `{ enabled }`) by adding the same
`activeTab === '<tab>'` check alongside their existing fiscal-year-ready check.
Salary Adjustments and Accounts and Taxes needed no such change — their hooks
already live inside `SalaryAdjustmentsTab.jsx`/`AccountsTaxesTab.jsx`
themselves, so React never invokes them until those components actually mount.

**Payslip export**: PDF uses `@react-pdf/renderer` (already a project dependency —
see `sections/apps/invoice/export-pdf/` for the precedent), a real downloaded
file. "Excel" export is a client-side generated **CSV** (no `xlsx`/`exceljs`
dependency exists in this project) — Excel opens a `.csv` directly, so this covers
the common case; ask for a real `.xlsx` via the `xlsx` package if that distinction
matters.

**Salary Forecast — backend-only today.** The `GET .../salary-forecast`
endpoint and `SalaryForecastDto` described above exist and work, but no
`SalaryForecastTab.jsx` / `useGetEmployeeSalaryForecast` /
`useGetTeacherSalaryForecast` was ever built into `PayAndTaxesPanel.jsx` — an
earlier draft of this guide described that plan before it was implemented, and
it never landed. If a standalone Salary Forecast tab is wanted later, the
backend is already there — just add the two hooks and a tab, same
`silentError: true` SWR pattern every other tab here uses.

**Tax Details tab (2026-07-23).** `useGetEmployeeTaxDetails`/
`useGetTeacherTaxDetails` (`api/employee-service.js`/`api/teacher-service.js`,
same `silentError: true` SWR pattern as every other tab, `GET
.../salaries/tax-details?fiscalYearId=`) feed `TaxDetailsTab.jsx`: a small
Name/Gender/Tax paid as/Handicapped info table, then the flat spreadsheet grid
itself — one column per `FISCAL_MONTHS` entry (`api/student-catalogs.js`,
already the shared Shrawan..Ashad list every other fiscal-month picker in this
app uses), horizontally scrollable. Per row, `row.isHeader` drives bold text
plus a tinted row background — this is a real, working signal (unlike
`isBold`/`isTab`, which the backend always sends `false`) and is `true` for
both pure section titles ("Incomes", "Retirement Fund") and real summary/total
rows ("Annual Income Forecast", "Annual Adjusted Taxable Amount", "Total
SST/TDS for the Year"), not just section titles — don't assume `isHeader`
means "no amount," most `isHeader` rows carry a real `total`. **Row 1
("Incomes") is the one exception worth knowing**: `BuildGridHeaderRow` gives it
`Total = 0` and every `monthNAmount = 0` (there's nothing to sum into a bare
section title), so rendering it as ordinary zero amounts would just be visual
noise — `TaxDetailsTab.jsx` instead uses that row's month cells to show each
column's Actual/Forecast status (`taxDetails.month{N}IsForecast`, a DTO-root
flag with no row of its own otherwise). Every other row's zero-valued month
cells (e.g. "SST Paid in Past Month" outside the one assessment month it's
computed for) render as a blank cell rather than "0.00" — a plain
`amount ? currencyFormat(amount) : ''` check, not a special case. Every month
column whose `monthNIsForecast` is `true` renders in a lighter `text.disabled`
color across **every** row, not just row 1's Actual/Forecast label itself — a
month that hasn't actually arrived yet reads as visually lighter/less certain
than a settled Actual month, matching the reference HRMS. Both tables (the
Name/Gender/Tax paid as/Handicapped info box and the spreadsheet grid) draw a
visible `1px solid` border on every cell (`& .MuiTableCell-root` in the
`Table`'s own `sx`) instead of MUI's default borderless-except-underline
look — deliberately spreadsheet-like, matching the reference. Tab order:
Tax Details sits right after Payslip (before Compensation Plan), not at the
end — both `<Tab>` and its matching `{activeTab === 'tax-details' && (...)}`
block in `PayAndTaxesPanel.jsx` were moved together so the JSX order still
matches the visible tab order.

**Investment & Tax Planning** (`investment_and_tax_planning_implementation_guide.md`):
`useGetEmployeeTaxPlanning`/`useGetTeacherTaxPlanning` (same
`silentError: true` SWR pattern, `GET .../salaries/tax-planning`) feed
`InvestmentTaxPlanningTab.jsx`, which replaces the tab's old hand-assembled
version (three separate hooks — `useGetEmployeeTaxCalculation` + the raw
`salaries` list + the fiscal year's `retirementExemptionCapAmount` — stitched
together inside `PayAndTaxesPanel.jsx` itself). Renders `taxPlanning.
fiscalYearCode`, never `fiscalYearId` (the raw-GUID display the dedicated guide
flags as a pre-existing bug), and binds the Income table's "Type" column to
`incomeLines[].valueType` via `SALARY_VALUE_TYPE_OPTIONS` instead of a
hardcoded "Fixed Income" string. **2026-07-23:** the tab's own "Tax Calculation
on Annual Taxable Income" summary row (six tiles: Gross Monthly, Retirement
Exemption, Insurance Deduction, Monthly Tax, Annual Tax, Net Monthly) was
removed — the tax-slab breakdown table now sits directly under the section
heading. The `useGetEmployeeTaxCalculation`/`useGetTeacherTaxCalculation` hooks
(previously kept alive only to feed a matching four-tile Gross/Net Monthly
summary on the Compensation Plan tab) were removed from
`EmployeeCompensation.jsx`/`TeacherSalary.jsx` too, once that Compensation Plan
summary row was also removed — nothing in `PayAndTaxesPanel.jsx` reads their
loading/error state anymore. The endpoint and hooks themselves are untouched
and still work if a future screen wants them.

**Compensation Plan tab redesign (2026-07-23).** `PayAndTaxesPanel.jsx`'s
`ComponentsSubPanel`/`DeductionsSubPanel`/`InsuranceSubPanel`:

- The inline "add a row" form is now a visually distinct dashed-border card
  (`Add Component`/`Add Deduction`/`Add Premium` overline label) instead of a
  bare `Grid` blending straight into the table, and the add action is a full
  `Add` button (with a loading spinner while the request is in flight) instead
  of a bare icon.
- Each existing row gets an **Edit** action on its Frequency value, alongside
  Remove. There is no `PUT` endpoint for a single
  `EmployeeSalaryComponent`/`EmployeeSalaryDeduction` row (only `POST`/`DELETE`
  — see `employee_management_implementation_guide.md`), so this "edit" is
  composed client-side as delete-then-re-add with every other field (code,
  value, valueType, taxable, retirement) carried over unchanged, only
  `frequencyType` replaced. Disabled, with an explanatory tooltip, on any code
  whose catalog option locks its frequency (see below) — a locked frequency is
  never editable, same as it's never freely enterable on add.
- The Frequency picker (add-row and the inline edit above) is restricted to
  **Monthly / One Time only** (`SALARY_LINE_FREQUENCY_OPTIONS` in
  `api/student-catalogs.js`, excluding `Annual`) — a salary component/deduction
  line is never meaningfully Annual in practice, even though the backend's
  `PayFrequencyType` enum and validation still technically allow it (Fee
  Structure rows elsewhere in the app still use the full
  `FREQUENCY_TYPE_OPTIONS` including Annual — this restriction is scoped to
  salary lines only).
- The percentage lock (force `valueType`/`value` for a catalog-locked code,
  e.g. `SSF_CONTRIBUTION`) and the new frequency lock both now parse the
  composite `additionalValue1` catalog format `CALCULATE_TYPE|TYPE|FREQUENCY`
  via `parseSalaryLineRule` in `api/student-catalogs.js` — full spec in
  `salary_structure_and_slip_adjustments_implementation_guide.md`.
- Component/deduction/insurance codes (`BASIC`, `SSF_CONTRIBUTION`, ...) render
  as a small monospace "code badge" (muted text, tinted background, rounded)
  instead of plain caption text, and every currency amount renders through a
  shared `Money` component that bolds just the currency code (`NPR`) and keeps
  the number at regular weight. An earlier pass of this redesign also added a
  green/red/blue color dot per section and per row to distinguish
  Earning/Deduction/Insurance lines — this was tried and then **removed**
  (2026-07-23) in favor of the code badge alone; don't reintroduce the dots.
- The tab's own four-tile summary row (Gross Monthly, Net Monthly, Effective
  From, Assessed As) that used to sit above "Salary Revisions" was removed
  entirely (2026-07-23) — see the Investment & Tax Planning note above for the
  matching hook cleanup this triggered.

## Known gaps

- No employee self-service login exists yet (`Employee.UserId` unpopulated, no
  `/me` endpoint) — everything here is reached through the existing admin
  employee/teacher profile pages.
- No attendance/leave module — `payDays`/`upl` on the payslip are simplified as
  noted above.
