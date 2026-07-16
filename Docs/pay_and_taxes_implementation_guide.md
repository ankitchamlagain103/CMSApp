# Pay & Taxes implementation guide

Adds three pieces on top of the existing Employee/Teacher compensation plan
(`employee_management_implementation_guide.md`): a **monthly** tax breakdown, a
structured **payslip** list/detail (separate from the existing HTML preview), and a
**loans and advances** workflow. Built for the admin-facing employee/teacher profile
pages; the same endpoints work unchanged if/when employee self-service login exists.

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

## Payslip — list + detail

Separate, structured (non-HTML) path from the existing
`GET .../salaries/payslip-preview` (which stays as-is, HTML template, latest
revision only).

- `GET /api/employees/{id}/payslips?fiscalYearId=` (+ `Teachers` alias) →
  `List<PayslipSummaryDto>` — **only** fiscal months whose `periodStartDate` has
  already passed (a future month has no payslip yet): `{ monthIndex, monthLabel
  ("Bhadra/FY-SAMPLE"), periodStartDate, periodEndDate, monthDays, payDays, upl }`.
  `payDays`/`upl` are simplified (`payDays = monthDays`, `upl` always `0`) — this
  codebase has no attendance/leave module to source real figures from.
- `GET /api/employees/{id}/payslips/{fiscalYearId}/{monthIndex}` (+ `Teachers`
  alias) → `PayslipDetailDto`: `{ employeeName, employeeCode, jobPositionCode,
  payMonthLabel, monthDays, payDays, upl, incomeLines, grossIncome,
  deductionLines, totalDeduction, netPaid }`. `deductionLines` = the month's
  resolved salary deductions **plus** a `"TDS"` line (that month's flat tax share)
  **plus** any `Approved`, due-that-month `EmployeeLoan` EMI (see below). `404`
  ("No payslip is available for that month yet") if the month hasn't started.

## Loans and Advances

`Domain/Entities/EmployeeLoan.cs` (`SoftDeleteAuditableEntity`, financial-audit
record like `StudentDiscount`/`StudentScholarship`): `EmployeeId`, `LoanTypeCode`
(a Config `DeductionType` code — restricted in the service to `LOAN`/`ADVANCE` only,
via `Domain/Constants/LoanTypeCodes.cs`, not any `DeductionType` code),
`PrincipalAmount`, `EmiAmount`, `RequestedDate`, `StartDate`, `Status`
(`Domain/Enums/LoanStatus`: `PendingApproval`/`Approved`/`Rejected`/`Cancelled`/
`Closed`), `Remarks`.

**No separate repayment ledger table.** Repayment progress is computed, not stored
(`Application/Payroll/LoanCalculator.cs`): `installmentsElapsed` = whole calendar
months between `StartDate` and today, capped at `⌈Principal / EMI⌉`;
`amountRepaid = installmentsElapsed × EMI` (capped at `Principal`).
`EmployeeLoanDto` exposes `amountRepaid`/`remainingBalance`/`isFullyRepaid` as
computed fields. `GET .../loans` opportunistically persists `Status = Closed` for
any `Approved` loan it finds fully repaid — self-healing on every read, no
scheduled job needed.

**"Auto-deduction" is also computed, not a persisted `EmployeeSalaryDeduction`
row** — `GetPayslipDetailAsync` folds in any `Approved` loan's EMI for a given
month by checking `LoanCalculator.IsDueInPeriod(loan, month.periodStartDate)`.
This means a later salary revision (a raise) needs **no special handling** to
"carry forward" the deduction — the loan lives independently of any
`EmployeeSalary` row.

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

## Frontend

`sections/apps/common/CompensationPanel.jsx` was renamed to `PayAndTaxesPanel.jsx`
and extended from 2 to 6 sibling tabs (Payslip, Tax Details, Compensation Plan,
Accounts and Taxes, Investment & Tax Planning, Loans and Advances), each a
separate component file in `sections/apps/common/`. The outer profile-page tab
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

## Known gaps

- **Migration pending**: new table `dbo.employee_loans` (FK to `employees`, no
  other schema changes) — not created here, same as every other pending-migration
  entry in `CLAUDE.md`. Until it exists, every `/loans` endpoint returns `500`;
  the frontend hooks (`useGetEmployeeLoans`/`useGetTeacherLoans`) pass
  `silentError: true` so this degrades to an empty "No loans or advances recorded
  yet" table instead of a full-page error takeover.
- No employee self-service login exists yet (`Employee.UserId` unpopulated, no
  `/me` endpoint) — everything here is reached through the existing admin
  employee/teacher profile pages.
- No attendance/leave module — `payDays`/`upl` on the payslip are simplified as
  noted above.
