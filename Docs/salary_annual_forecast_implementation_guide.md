# Salary annual forecast ("salary receipt history") — implementation guide

The "which months have real, disbursed payroll history vs. which are still just a projection"
grid, matching the reference HRMS's forecast table (Particulars / Description / Annual Amount /
one column per fiscal month).

## Endpoint

```
GET /api/employees/{id}/salaries/annual-forecast?fiscalYearId=
GET /api/teachers/{id}/salaries/annual-forecast?fiscalYearId=   (alias, same shape)
```

- `fiscalYearId` optional — omit to use the fiscal year currently marked `IsCurrent`.
- Permission: `EMPLOYEE_SALARY_ANNUAL_FORECAST` / `TEACHER_SALARY_ANNUAL_FORECAST`.

## Response shape

```jsonc
{
  "responseCode": "Success",
  "data": {
    "employeeId": "...",
    "fiscalYearId": "...",
    "fiscalYearCode": "2084/85",
    "monthNames": ["Shrawan", "Bhadra", "Ashwin", "Kartik", "Mangsir", "Poush", "Magh", "Falgun", "Chaitra", "Baishakh", "Jestha", "Ashad"],
    "isActualByMonth": [true, false, false, false, false, false, false, false, false, false, false, false],

    "incomeLines": [
      { "code": "BASIC", "label": "Basic Salary", "description": "Actual", "annualAmount": 216000.00, "monthlyAmounts": [18000.00, 18000.00, 18000.00, "...9 more"] },
      { "code": "SSF_CONTRIBUTION", "label": "SSF Contribution", "description": "Actual", "annualAmount": 43200.00, "monthlyAmounts": [3600.00, 3600.00, "..."] },
      { "code": "FESTIVAL_BONUS", "label": "Festival Dashain Bonus", "description": "Actual", "annualAmount": 26821.92, "monthlyAmounts": [26821.92, null, null, "...9 more nulls"] }
    ],

    "annualIncomeForecastLine": { "code": "ANNUAL_INCOME_FORECAST", "label": "Annual Income Forecast", "annualAmount": 1346821.92, "monthlyAmounts": [136821.92, 110000.00, 110000.00, "..."] },

    "retirementFundLines": [
      { "code": "SSF_DEDUCTION_TOTAL", "label": "SSF Deduction", "annualAmount": 1980.00, "monthlyAmounts": [1980.00, 1980.00, "..."] },
      { "code": "RETIREMENT_A", "label": "a) Sum of Eligible Retirement Fund and Social Security Fund", "annualAmount": 66960.00, "monthlyAmounts": [66960.00, null, null, "..."] },
      { "code": "RETIREMENT_B", "label": "b) Allowable Limit", "annualAmount": 500000.00, "monthlyAmounts": [500000.00, null, null, "..."] },
      { "code": "RETIREMENT_C", "label": "c) 1/3rd of Taxable Income", "annualAmount": 448940.64, "monthlyAmounts": [448940.64, null, null, "..."] },
      { "code": "RETIREMENT_MIN", "label": "Min of a, b or c", "annualAmount": 66960.00, "monthlyAmounts": [66960.00, null, null, "..."] }
    ]
  }
}
```

## What "Actual" vs "Forecast" means, per month

`isActualByMonth[i]` is `true` when a real, **Approved or Paid** `SalarySlip` already exists for
that fiscal month (via `IPayrollRunRepository.GetSlipsForYearAsync` — a Draft or Cancelled run
doesn't count). For an Actual month:

- Income line amounts come from that month's real `SalarySlipLine` (`Earning`-type) rows —
  exactly what was disbursed, not a projection.
- The retirement-fund `a`/`b`/`c`/`min` breakdown is **re-computed from that month's own
  snapshotted salary revision** (`SalarySlip.EmployeeSalaryId`), not the employee's current plan
  — so it reflects what was actually true when that month was paid, even if the compensation
  plan has since changed.

For a **Forecast** month (no Actual slip yet):

- Every figure is projected from the employee's **current** compensation plan, via the same
  `MonthlyBreakdownCalculator`/`TaxCalculator` pipeline the Tax Details tab already uses — so a
  Forecast month here can never disagree with `GET .../salaries/tax-calculation/monthly`.
- **The retirement-fund `a`/`b`/`c`/`min` rows are deliberately `null` for every Forecast
  month** (matches the reference screenshot, where those cells are blank except under the one
  Actual column). Projecting "eligible retirement contribution to date" forward doesn't mean
  anything meaningful before the month is actually disbursed — it isn't a monthly-recurring
  figure, it's a whole-year assessment concept — so rather than show a misleading forecasted
  number, the UI should render these cells blank for Forecast months, same as the reference.

## Row set (`incomeLines`) is the union across the whole year, not just this month's plan

A code shows up as its own row if it appeared in **any** month — an Actual month's real slip
lines, or the Forecast baseline's projected components. This means a one-time bonus that was
actually paid in an earlier month (Actual) still gets its own row even though the *current*
compensation plan wouldn't project it going forward — `monthlyAmounts` is `null` for every month
it didn't apply to (pad to exactly 12 entries; don't assume a short array means "rest are zero").

`annualAmount` on every row (including `annualIncomeForecastLine`) is simply the sum of that
row's 12 `monthlyAmounts` (treating `null` as 0) — always internally consistent, never a
separately-computed figure.

## Things worth knowing

- **This is a read-only, computed-on-the-fly endpoint** — nothing is stored; it recomputes from
  the real Payroll Run slips (for Actual months) and the current compensation plan (for Forecast
  months) on every call.
- **Changing a Draft slip doesn't affect this view** — only `Approved`/`Paid` slips count as
  Actual, matching the same "Payslip source-of-truth" rule the plain Payslip endpoints already
  use (see `CLAUDE.md`'s 2026-07-21 note). A Draft run's numbers are still just a projection here.
- Failure conditions mirror the other salary-tax endpoints: `404 NotFound` (employee/fiscal year
  not found, or no salary on record), no hard failure for missing tax slabs — a Forecast month
  with no configured tax slabs just contributes `0` gross/SSF-deduction rather than failing the
  whole request, since the Actual months (if any) are still valid and worth returning.

## Where this lives in the codebase

- `Application/Employees/Dtos/SalaryAnnualForecastDto.cs` + sibling `SalaryForecastLineDto.cs`.
- `EmployeeService.GetAnnualForecastAsync` (`Application/Employees/EmployeeService.cs`) — the
  private `AddOrUpdateMonthlyLine` helper merges an Actual or Forecast month's lines into the
  running per-code row map, keeping every row's `MonthlyAmounts` aligned to the same 12 columns
  regardless of which months it actually appeared in.
- `TeacherService.GetAnnualForecastAsync` forwards to the same `IEmployeeService` call via the
  shared-employee-id trick, same as every other Teacher-aliased Pay & Taxes endpoint.
- Controllers: `EmployeesController.GetAnnualForecast` / `TeachersController.GetAnnualForecast`.
