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

## Response shape, with a worked example verified byte-for-byte against a real HRMS payslip (2026-07-22)

**`SSF_CONTRIBUTION` must be `isTaxable: true`.** An earlier version of this
doc (and the dev data it was pulled from) modeled the employer's SSF
contribution as `isTaxable: false` — **that shape is wrong** and was the root
cause of a real discrepancy against a live HRMS system. Under Nepal's Income
Tax Act, the employer's contribution to an approved retirement fund (SSF/EPF)
*is* part of assessable income — the tax relief for it comes entirely through
the retirement-fund exemption (`retirementFund.exemptionApplied`, the
`min(a, b, c)` rule below), not through excluding it from gross via a
separate "non-taxable" flag. Marking it non-taxable double-relieves it (once
by excluding it from gross, again via the exemption) and silently understates
tax. `SalaryCalculatorService`'s suggested-structure output already gets this
right (`IsTaxable = true` for the SSF contribution component); it's only a
mistake if entered by hand on a salary revision. `payroll_fixes_implementation_guide.md`
flagged this exact mistake on a specific dev-DB test employee back on
2026-07-18 — if your numbers look low, check that employee's SSF Contribution
component is still marked non-taxable and fix it there, not here.

The worked example below reconciles exactly (to the rupee) against a real
HRMS payslip for the same inputs — Basic 216,000/yr, SSF Contribution
(component, 20% of Basic, **taxable**, retirement-flagged) 43,200/yr, Other
Allowance 971,820/yr, Dearness Allowance 67,380/yr, Communication Allowance
3,600/yr, Travel Allowance 18,000/yr, Festival Dashain Bonus (One Time)
26,821.92/yr, SSF Deduction (deduction, 11% of Basic, retirement-flagged)
23,760/yr, fiscal year 2084/85, Couple assessment, no insurance premiums:

```jsonc
{
  "responseCode": "Success",
  "responseMessage": null,
  "data": {
    "employeeId": "...",
    "salaryId": "...",
    "fiscalYearId": "...",
    "fiscalYearCode": "2084/85",
    "assessmentType": "Couple",

    "incomeLines": [
      { "code": "SSF_CONTRIBUTION",        "label": "SSF Contribution",        "valueType": 1, "annualAmount": 43200.00,  "isTaxable": true },
      { "code": "BASIC",                   "label": "Basic Salary",            "valueType": 2, "annualAmount": 216000.00, "isTaxable": true },
      { "code": "DEARNESS_ALLOWANCE",      "label": "Dearness Allowance",      "valueType": 2, "annualAmount": 67380.00,  "isTaxable": true },
      { "code": "OTHER_ALLOWANCE",         "label": "Other Allowance",         "valueType": 2, "annualAmount": 971820.00, "isTaxable": true },
      { "code": "COMMUNICATION_ALLOWANCE", "label": "Communication Allowance", "valueType": 2, "annualAmount": 3600.00,   "isTaxable": true },
      { "code": "TRAVEL_ALLOWANCE",        "label": "Travel Allowance",        "valueType": 2, "annualAmount": 18000.00,  "isTaxable": true },
      { "code": "FESTIVAL_BONUS",          "label": "Festival Dashain Bonus",  "valueType": 2, "annualAmount": 26821.92,  "isTaxable": true }
    ],
    "totalAnnualIncome": 1346821.92,

    "retirementFund": {
      "eligibleContributionAnnual": 66960.00,
      "oneThirdOfTaxableIncome": 448940.64,
      "maximumLimit": 500000.00,
      "exemptionApplied": 66960.00,
      "additionalContributionAvailable": 381980.64
    },

    "insuranceLines": [],
    "insuranceDeductionCapped": 0.00,

    "taxCalculation": {
      "annualTaxableIncome": 1279861.92,
      "annualTax": 27986.19,
      "monthlyTax": 2332.18,
      "grossAnnualIncome": 1346821.92,
      "retirementContributionAnnual": 66960.00,
      "retirementExemption": 66960.00,
      "insuranceDeduction": 0.00,
      "insuranceDeductionLines": [],
      "cashDeductionsAnnual": 66960.00,
      "netAnnualIncome": 1251875.73,
      "isSsfExemptionApplied": true,
      "breakdown": [
        { "minAmount": 0.00, "maxAmount": 1000000.00, "taxRate": 0.01, "taxableAmountInSlab": 1000000.00, "taxForSlab": 0.00, "isSsfExempted": true },
        { "minAmount": 1000000.00, "maxAmount": 1500000.00, "taxRate": 0.10, "taxableAmountInSlab": 279861.92, "taxForSlab": 27986.19, "isSsfExempted": false }
      ]
    },

    "grossMonthly": 112235.16,
    "netMonthly": 104322.98
  }
}
```

**`netMonthly` is `NOT` `grossMonthly - monthlyTax`** (fixed 2026-07-22) — it's
`taxCalculation.netAnnualIncome / 12`, where `netAnnualIncome =
grossAnnualIncome - cashDeductionsAnnual - annualTax`. `cashDeductionsAnnual`
sums every real salary *deduction* (`SSF_DEDUCTION` here, 23,760/yr) **plus**
every `IsRetirementContribution` *component* (`SSF_CONTRIBUTION` here,
43,200/yr — it's taxable income but the employer's share never reaches the
employee's bank account, same offsetting rule
`MonthlyBreakdownCalculator` already applies per month). The earlier version
of this doc showed `netMonthly: 109,902.98` (`grossMonthly - monthlyTax` only)
— that number silently ignored the 66,960/yr of real deductions and
overstated take-home pay by exactly `66,960 / 12 = 5,580`/month.

**Two prerequisites for the slab boundaries above to be exactly contiguous
(no 1-rupee gap between brackets)**: the fiscal year's `TaxSlab` rows must
use `MinAmount` == the previous slab's `MaxAmount` (first slab `MinAmount =
0`), not a `+1` offset — `PayrollSeeder`'s `2084/85` slabs were fixed to this
convention on 2026-07-22 (see `CLAUDE.md`), but this only affects freshly
seeded databases; an already-seeded fiscal year keeps its old `1 /
1,000,001 / 1,500,001 / ...` boundaries until corrected via `PUT
/api/fiscalyears/{id}/taxslabs` (each slab's `MinAmount` should equal the
previous slab's `MaxAmount`). The old `+1` convention silently drops exactly
1 rupee of taxable income at every bracket transition — immaterial for most
incomes, but it's what stood between an otherwise-exact match and a real
HRMS payslip in the example that prompted this fix (a 10-paisa difference:
27,986.09 vs the correct 27,986.19).

## Field → screenshot mapping

| UI element | Field |
|---|---|
| "Income" table rows (Description / Type / Annual Income) | `incomeLines[].label` / `incomeLines[].valueType` / `incomeLines[].annualAmount` |
| "Total Annual Income" | `totalAnnualIncome` |
| "a. Sum of Eligible Retirement Fund and Social Security Fund" | `retirementFund.eligibleContributionAnnual` |
| "b. 1/3rd of Taxable Income" | `retirementFund.oneThirdOfTaxableIncome` |
| "c. Maximum Limit" | `retirementFund.maximumLimit` |
| "Deduct the lowest A, B and C" | `retirementFund.exemptionApplied` (== `taxCalculation.retirementExemption`) |
| "If you want, you can contribute an additional NPR X to a retirement fund to save more tax" | `retirementFund.additionalContributionAvailable` — **bind this, don't recompute it** (see note below) |
| Insurance/other-deduction table rows (Type / Annual Premium / Eligible % / Cap / Deducted / Contribute-more-to-save-tax) | `insuranceLines[].insuranceTypeLabel` / `.annualPremiumAmount` / `.eligiblePercentage` / `.capAmount` / `.deductedAmount` / `.additionalAmountAvailable` |
| "Deductible Amount (capped)" | `insuranceDeductionCapped` (== `taxCalculation.insuranceDeduction`, == sum of `insuranceLines[].deductedAmount`) |
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

- **Nepal's Social Security Tax (SSF) waiver (2026-07-22)**: when the salary has an active SSF
  contribution — a `SSF_CONTRIBUTION` component or `SSF_DEDUCTION` deduction with a nonzero
  resolved amount — the first (lowest) tax slab's rate is **not charged at all**, not just
  reduced. This is separate from the retirement-fund "least of three" exemption above (that
  shrinks the taxable base; this zeroes the tax on the first bracket specifically, since an SSF
  member is already funding the government's own social-security scheme). Reflected as
  `taxCalculation.isSsfExemptionApplied` (top-level flag) and
  `taxCalculation.breakdown[].isSsfExempted` (per-row — true only on the exempted row; its
  `taxRate` still shows the configured rate for transparency, but `taxForSlab` is `0`). **This was
  previously missing** — every SSF-contributing employee's tax was overstated by the first
  bracket's full amount (e.g. an employee whose taxable income sat entirely in the first 1%
  bracket owed `0`, not `3,730.39`, once SSF is contributed). If an employee doesn't contribute to
  SSF, behavior is unchanged.
- **`retirementFund.additionalContributionAvailable` is the correct "contribute X more to save
  more tax" figure — bind it instead of computing `maximumLimit - eligibleContributionAnnual`
  yourself.** That naive subtraction (what an earlier frontend build did) ignores the `b` cap
  (1/3 of taxable income): if `b` is smaller than `c`, contributing beyond `b` buys zero
  additional exemption even though `c` isn't reached yet. The correct figure is
  `max(0, min(b, c) - a)`, computed server-side. Example from a prior report: `a` = 66,960,
  `b` (1/3) = 160,000, `c` (cap) = 500,000 — the old frontend math showed "contribute up to
  433,040 more" (`c - a`), but only 93,040 of that (`min(b, c) - a`) actually reduces tax; beyond
  that, `b` binds regardless of `c`.
- **`incomeLines` lists every earning component on the salary, taxable or
  not** — an `isTaxable: false` row would still be shown in the Income table
  but excluded from `totalAnnualIncome`. In practice this should be rare:
  `SSF_CONTRIBUTION` (see the warning above) and every other real earning
  component are `isTaxable: true`. Don't filter the array before rendering;
  render `isTaxable` as a badge/column instead if you want to call it out.
- **`valueType` is the component's real type**, the raw numeric
  `Domain/Enums/AwardValueType` value — **`1` = `Percentage`, `2` =
  `FixedAmount`** (no `JsonStringEnumConverter` is registered anywhere in
  this API, so every enum serializes as its underlying int, not a string;
  don't assume otherwise). Already resolved into a period/annual amount
  either way — `valueType` is informational, `annualAmount` is always the
  real currency figure.
- **The retirement fund's "b" input is `totalAnnualIncome` (the sum of
  taxable components)** — same `grossAnnualIncome` `TaxCalculator` already
  uses internally. In the example above `1,346,821.92 / 3 = 448,940.64`.
  Since `SSF_CONTRIBUTION` correctly counts toward `totalAnnualIncome` (see
  the warning above), it also correctly inflates "b" — this is expected, not
  a bug: HRMS systems compute "b" the same way.
- **"c. Maximum Limit" comes from the fiscal year's `RetirementExemptionCapAmount`.**
  A value of `0.00` is a real, valid configuration — it means the fiscal
  year's retirement exemption is fully disabled. Both seeded fiscal years
  default this to `500,000` (see `CLAUDE.md`'s payroll-fixes section), but a
  hand-edited or manually-created fiscal year row can still have it at `0` —
  `min(a, b, c)` with `c = 0` is always `0`, silently killing the entire
  retirement exemption. If a payslip's tax looks too high, check this first.
- **Deductions never appear in `incomeLines`**, but retirement-flagged ones
  still feed `retirementFund.eligibleContributionAnnual` — in the example,
  `SSF_CONTRIBUTION` (component, 43,200) + `SSF_DEDUCTION` (deduction,
  23,760) = `66,960`. If the "a" figure looks wrong, check both the
  components *and* deductions tabs of the Compensation Plan, not just Income.
- **Insurance/other-deduction lines now carry a full per-line breakdown (2026-07-22)**, matching
  Nepal's FY 2083/84 formula: `deductedAmount = min(actualAmount * eligiblePercentage / 100, capAmount)`.
  For a straight insurance premium (Life/Health/Housing), `eligiblePercentage` is `100` and
  the formula reduces to the old `min(actual, cap)`. **`Children's Education` is the one type
  where `eligiblePercentage` is `25`** — only a quarter of the declared annual education expense
  counts before the NPR 25,000 cap applies, per the "25% of annual education expenses, capped at
  NPR 25,000" rule; add an `EDUCATION`-type row via `POST .../insurance-premiums` with
  `annualPremiumAmount` = the actual annual expense (not the deduction amount) to use it.
  **`additionalAmountAvailable`** is the "you can spend NPR X more on Y to save more tax" figure
  per type — bind it instead of computing `capAmount - actualAmount` client-side, which is wrong
  for `Children's Education` (ignores the 25% factor: reaching NPR 1 more of deduction there
  needs NPR 4 more of actual expense). Insurance-type caps as of this fix: Life 40,000 / Health
  20,000 / Housing 10,000 (**changed from 25,000** — a prior guess before the official FY 2083/84
  formula doc was available; an already-seeded dev database keeps the old value until a manual
  `PUT /api/configs/{id}`) / Education 25,000 (at 25% eligibility).
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
- `Application/Payroll/Dtos/TaxDeductionLineDto.cs` (the generic per-line shape
  `TaxCalculationResultDto.InsuranceDeductionLines` uses — `TaxPlanningInsuranceLineDto` mirrors
  it with tab-facing field names) and `Application/Payroll/Dtos/InsuranceCapConfig.cs` (Cap +
  EligiblePercentage, replacing the old plain-decimal cap dictionary).
  `Application/Common/Helpers/InsuranceCapHelper.cs` builds the `Dictionary<string,
  InsuranceCapConfig>` from Config catalog 1015 — shared by `EmployeeService`,
  `PayrollRunService`, and `SalaryCalculatorService`, which previously each hand-rolled the same
  parse independently.
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
