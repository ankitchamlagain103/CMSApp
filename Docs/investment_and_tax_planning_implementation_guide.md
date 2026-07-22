# Investment & Tax Planning — implementation guide

One endpoint, one response, matches the tab exactly. This replaces whatever
combination of `GET .../salaries/tax-calculation` + `GET .../salaries` (for
the component/premium line items) + `GET /api/fiscalyears/{id}` (for the
retirement cap) the frontend was previously assembling and recomputing by
hand — that assembly is what was slow. Every number below is computed
server-side from a single `TaxCalculator.CalculateFromSalary` run, so it can
never disagree with the plain `GET .../salaries/tax-calculation` endpoint or
with itself.

## Endpoint

```
GET /api/employees/{id}/salaries/tax-planning?fiscalYearId=
GET /api/teachers/{id}/salaries/tax-planning?fiscalYearId=   (alias, same shape)
```

- `fiscalYearId` optional — omit to use the fiscal year currently marked
  `IsCurrent`.
- Uses the employee's **current** salary revision (latest `EffectiveFromDate`)
  — same "current" definition every other Pay & Taxes endpoint uses.
- Permission: `EMPLOYEE_TAX_PLANNING` / `TEACHER_TAX_PLANNING`.

## Response shape, with the exact worked example from the screenshot

```jsonc
{
  "responseCode": "Success",
  "responseMessage": null,
  "data": {
    "employeeId": "019f659b-959f-7280-989b-05c83fe3ba91",
    "salaryId": "...",
    "fiscalYearId": "019f6ff4-a19a-75ae-aed0-1c2c68334169",
    "fiscalYearCode": "2084/85",
    "assessmentType": "Individual",

    "incomeLines": [
      { "code": "DEARNESS_ALLOWANCE",     "label": "Dearness Allowance",     "valueType": 2, "annualAmount": 18000.00,  "isTaxable": true  },
      { "code": "OTHER_ALLOWANCE",        "label": "Other Allowance",        "valueType": 2, "annualAmount": 240000.00, "isTaxable": true  },
      { "code": "BASIC",                  "label": "Basic Salary",           "valueType": 2, "annualAmount": 216000.00, "isTaxable": true  },
      { "code": "COMMUNICATION_ALLOWANCE","label": "Communication Allowance","valueType": 2, "annualAmount": 6000.00,   "isTaxable": true  },
      { "code": "SSF_CONTRIBUTION",       "label": "SSF Contribution",       "valueType": 1, "annualAmount": 23760.00,  "isTaxable": false }
    ],
    "totalAnnualIncome": 480000.00,

    "retirementFund": {
      "eligibleContributionAnnual": 24000.00,
      "oneThirdOfTaxableIncome": 160000.00,
      "maximumLimit": 0.00,
      "exemptionApplied": 0.00
    },

    "insuranceLines": [
      { "insuranceTypeCode": "LIFE", "insuranceTypeLabel": "Life Insurance", "annualPremiumAmount": 40000.00 }
    ],
    "insuranceDeductionCapped": 40000.00,

    "taxCalculation": {
      "annualTaxableIncome": 440000.00,
      "annualTax": 4399.99,
      "monthlyTax": 366.67,
      "grossAnnualIncome": 480000.00,
      "retirementContributionAnnual": 24000.00,
      "retirementExemption": 0.00,
      "insuranceDeduction": 40000.00,
      "breakdown": [
        { "minAmount": 1.00, "maxAmount": 1000000.00, "taxRate": 0.01, "taxableAmountInSlab": 439999.00, "taxForSlab": 4399.99 }
      ]
    },

    "grossMonthly": 40000.00,
    "netMonthly": 39633.33
  }
}
```

## Field → screenshot mapping

| UI element | Field |
|---|---|
| "Income" table rows (Description / Type / Annual Income) | `incomeLines[].label` / `incomeLines[].valueType` / `incomeLines[].annualAmount` |
| "Total Annual Income" | `totalAnnualIncome` |
| "a. Sum of Eligible Retirement Fund and Social Security Fund" | `retirementFund.eligibleContributionAnnual` |
| "b. 1/3rd of Taxable Income" | `retirementFund.oneThirdOfTaxableIncome` |
| "c. Maximum Limit" | `retirementFund.maximumLimit` |
| "Deduct the lowest A, B and C" | `retirementFund.exemptionApplied` (== `taxCalculation.retirementExemption`) |
| Insurance table rows (Type / Annual Premium) | `insuranceLines[].insuranceTypeLabel` / `insuranceLines[].annualPremiumAmount` |
| "Deductible Amount (capped)" | `insuranceDeductionCapped` (== `taxCalculation.insuranceDeduction`) |
| "Assessment Type" | `assessmentType` |
| "Fiscal Year" | render `fiscalYearCode`, **not** `fiscalYearId` — the screenshot shows the raw GUID here, which is a pre-existing frontend bug worth fixing while wiring this endpoint up |
| "Gross Monthly" | `grossMonthly` |
| "Retirement Exemption" | `taxCalculation.retirementExemption` |
| "Insurance Deduction" | `taxCalculation.insuranceDeduction` |
| "Monthly Tax" | `taxCalculation.monthlyTax` |
| "Annual Tax" | `taxCalculation.annualTax` |
| "Net Monthly" | `netMonthly` |
| Slab table (Min / Max / Rate / Taxable in Slab / Tax for Slab) | `taxCalculation.breakdown[].minAmount/maxAmount/taxRate/taxableAmountInSlab/taxForSlab` |

## Things worth knowing before wiring this up

- **`incomeLines` lists every earning component on the salary, taxable or
  not** — `isTaxable: false` rows (like `SSF_CONTRIBUTION` above) are still
  shown in the Income table but excluded from `totalAnnualIncome`. Don't
  filter the array before rendering; render `isTaxable` as a badge/column
  instead if you want to call it out.
- **`valueType` is the component's real type**, the raw numeric
  `Domain/Enums/AwardValueType` value — **`1` = `Percentage`, `2` =
  `FixedAmount`** (no `JsonStringEnumConverter` is registered anywhere in
  this API, so every enum serializes as its underlying int, not a string;
  don't assume otherwise). Already resolved into a period/annual amount
  either way — `valueType` is informational, `annualAmount` is always the
  real currency figure. The screenshot shows every row labeled "Fixed
  Income" including the Percentage-valued SSF Contribution row — that looks
  like a hardcoded frontend string rather than something reflecting the real
  type; bind the "Type" column to `valueType` now that it's available
  instead.
- **The retirement fund's "b" input is `totalAnnualIncome` (the sum of
  taxable components), not gross-including-non-taxable** — same
  `grossAnnualIncome` `TaxCalculator` already uses internally. In the example
  above `480,000 / 3 = 160,000`.
- **"c. Maximum Limit" comes from the fiscal year's `RetirementExemptionCapAmount`.**
  A value of `0.00` is a real, valid configuration — it means the fiscal
  year's retirement exemption is fully disabled (this is the current state of
  both seeded fiscal years in dev; see `CLAUDE.md`'s payroll-fixes section),
  not a bug in this endpoint. `min(a, b, c)` with `c = 0` is always `0`, which
  is exactly what the example shows.
- **Deductions never appear in `incomeLines`**, but retirement-flagged ones
  still feed `retirementFund.eligibleContributionAnnual` — in the example,
  `SSF_CONTRIBUTION` (component, 23,760) + `SSF_DEDUCTION` (deduction, 240) =
  `24,000`. If the "a" figure looks wrong, check both the components *and*
  deductions tabs of the Compensation Plan, not just Income.
- **Insurance lines are the raw premiums; only the total is capped.** There's
  no per-line capped amount in this response — Nepal's insurance-deduction
  cap is a combined figure per `TaxCalculator.CalculateFromSalary`, not
  per-policy, so a per-line "capped" column isn't meaningful.
- **All amounts are already rounded to 2 decimal places server-side** — the
  same fix that resolved the `39633.334166666666666666666667`-style values
  reported earlier now applies here too (`TaxCalculator`/
  `MonthlyBreakdownCalculator` round at every leaf computation). Don't
  re-round or `toFixed()` defensively; the numbers are exact as returned.
- **Fiscal-year-not-found / no-salary-yet / no-tax-slabs-configured** all
  return the same failure shapes `GET .../salaries/tax-calculation` already
  returns (this endpoint composes on top of it) — see the failure table
  below.

## Failure table

| Condition | `responseCode` | HTTP |
|---|---|---|
| Employee/teacher id doesn't exist | `NotFound` | 404 |
| Employee has no salary on record yet | `NotFound` | 404 |
| `fiscalYearId` given but doesn't exist | `NotFound` | 404 |
| No `fiscalYearId` given and no fiscal year is marked `IsCurrent` | `NotFound` | 404 |
| Fiscal year has no tax slabs configured for the salary's assessment type | `ValidationError` | 400 |

## Where this lives in the codebase

- `Application/Employees/Dtos/TaxPlanningDto.cs` (+ sibling
  `TaxPlanningIncomeLineDto.cs`, `TaxPlanningInsuranceLineDto.cs`,
  `RetirementFundBreakdownDto.cs`).
- `EmployeeService.GetTaxPlanningAsync` (`Application/Employees/EmployeeService.cs`)
  — composes on `GetCurrentSalaryTaxCalculationAsync`, then rebuilds the
  per-component/per-premium lines using the same `TaxCalculator.ResolveAmount`/
  `TaxCalculator.Annualize`/`TaxCalculator.FindBasicPeriodAmount` helpers
  `MonthlyBreakdownCalculator` already shares, so a component's resolved
  amount can never disagree between the two views.
- `TeacherService.GetTaxPlanningAsync` forwards to the same
  `IEmployeeService` call via the shared-employee-id trick, same as every
  other Teacher-aliased Pay & Taxes endpoint.
- Controllers: `EmployeesController.GetTaxPlanning` / `TeachersController.GetTaxPlanning`.
