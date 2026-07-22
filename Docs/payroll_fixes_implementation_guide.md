# Payroll System Fixes & Salary Calculator — UI Implementation Guide (2026-07-18, round 2 same day)

Backend response to `Docs/Issues in Payroll System.pdf` plus the same-day follow-up feedback.
This page covers what changed in the API, what the UI must change, and which reported issues are
frontend-only. Round-2 additions (employer-SSF payslip offset, calculator v2 with
bonus/basic/CIT/insurance + Assign-to-Employee, bulk salary adjustments, and the verified
explanation of the "two fiscal years, same tax" report) are marked below.

Everything here follows the standard envelope (`responseCode` / `responseMessage` / `data`) and
requires a Bearer token. New permission codes are listed per endpoint (seeded to SuperAdmin
automatically; grant to other roles via `POST /api/roles/claims`).

**No new migration is needed** — every change is code/seed-only.

---

## 1. Payroll run list showed 0 slips / 0.00 totals — FIXED (backend)

`GET /api/payrollruns` previously returned `slipCount: 0`, `totalGrossEarnings: 0`,
`totalNetPay: 0` on every row (the aggregates were computed from a child collection the list
query never loaded), while the run detail showed real numbers. Fixed server-side — no UI change
needed beyond removing any workaround.

Also changed deliberately: **cancelled slips no longer count** toward `slipCount` /
`totalGrossEarnings` / `totalNetPay` on either the list or the detail header — those amounts are
not going to be paid. Cancelled slips still appear in the detail's `slips` array with
`status: 4` (Cancelled), so show them greyed out rather than expecting them in the totals.

## 2. Refresh a Draft payroll run — NEW ENDPOINT

The run generated at `POST /api/payrollruns` is a **snapshot**. Editing a compensation plan,
fixing tax slabs, approving a loan, or adding a salary adjustment after generation changes
nothing on an existing Draft run — that is by design (transactional snapshots are immutable
history), and it is why "created new fiscal year with different salary tax slabs but the run
still shows the old numbers" happens: the slips were built before/with different data, or the
run needs rebuilding to pick the current slab set up.

```
POST /api/payrollruns/{id}/refresh          permission: PAYROLL_RUN_REFRESH
```

No request body. Response: same shape as `POST /api/payrollruns` —
`{ run: PayrollRunDto (with slips), skipped: [{ employeeId, employeeName, reason }] }`.

Semantics (Draft runs only — anything else is `409 CONFLICT`):

| Situation | What refresh does |
|---|---|
| Draft slip, employee still payable | Generated lines rebuilt from the **current** compensation plan, tax slabs, loan EMIs and Pending adjustments. Slip id and `slipNo` survive. |
| Manual lines the admin added to a Draft slip | **Preserved** — refresh replaces only generated lines (`source` 1–4), never `source: 5` (Manual). |
| Adjustments the run had consumed | Re-pended first, then re-applied — so adjustments added *after* generation are picked up too. |
| Slip individually cancelled by the admin | Stays cancelled (reported in `skipped`). |
| Employee became payroll-eligible after generation | Gets a brand-new slip in the run. |
| Employee no longer payable (status change, plan removed, no slabs) | Their Draft slip is cancelled (reported in `skipped`). |

**UI**: add a "Refresh" button on the Run Detail page (and optionally the list row action menu),
visible while `status` is Draft (1). Show the `skipped` reasons after the call, then re-render
from the returned `run`. This is the answer to "payroll run should have refresh feature to
update if anything is updated" — and the fix path for the wrong-tax-slab report: fix the fiscal
year's slabs, then hit Refresh (or cancel + regenerate; both now work).

Note the tax engine always did read the slabs of **the run's own fiscal year** at generation
time; a run for fiscal year 2084/85 uses 2084/85's slabs. What never happened until now is any
*re*-read after generation.

### Why FY-SAMPLE and 2084/85 produce the exact same TDS (verified against the dev DB, round 2)

The identical `366.67`/month is **arithmetically correct**, not a slab-selection bug. Verified
directly against the database:

- FY-SAMPLE Individual slabs: 0–500,000 @ **1%**, then 10/20/30/36%.
- 2084/85 Individual slabs: 1–1,000,000 @ **1%**, then 10/20/27/29%.
- The employee's (EMP2026006) taxable income: taxable components 40,000/month (Basic 18,000 +
  allowances 22,000; the 1,980 SSF_CONTRIBUTION is flagged **not taxable** in the data) =
  480,000/year, minus the capped LIFE insurance premium 40,000 = **440,000**, minus a
  retirement exemption of **0** (see below) = 440,000.
- 440,000 falls entirely inside the first bracket of **both** years, and both first brackets are
  **1%** → 4,400/year → 366.67/month either way. The two slab sets only diverge above
  Rs. 500,000 of taxable income — raise Basic to ~55,000+ and the runs will differ.

Two data problems found while verifying, both worth fixing from the admin UI:

1. **`retirementExemptionCapAmount` is `0.00` on BOTH fiscal years** — the "least of three"
   exemption (actual contributions vs ⅓ of income vs the cap) is therefore always 0, so SSF/CIT
   contributions currently reduce nothing. Set a real cap (Rs. 500,000 per current law) via
   `PUT /api/fiscalyears/{id}`.
2. **The employee's SSF lines are mis-entered**: `SSF_CONTRIBUTION` is 11% (should be the
   employer's 20%), not taxable, not retirement-flagged; `SSF_DEDUCTION` is a **fixed Rs. 20**
   (should be 11% of Basic), retirement-flagged. The correct shape is in §3 below — or just use
   the calculator's **Assign to Employee** (§4) to write a clean revision.

## 3. SSF rates are now configuration — NEW CONFIG CATALOG 1018

Nepal SSF rule, as modeled: **31% of Basic Salary** total —

- **Employee share (11%)**: deducted from the employee's pay (`SSF_DEDUCTION` deduction line).
- **Employer share (20%)**: paid by the employer **on top of** salary (`SSF_CONTRIBUTION`
  component line). It is a CTC cost and a taxable benefit — it must **never** also be deducted
  from pay. The "SSF contributed amount is also deducted" data seen in the issue report is a
  mis-entered plan: a 21%-of-basic `SSF_DEDUCTION` plus an 11% `SSF_CONTRIBUTION` component has
  the two shares swapped *and* double-counts. Correct plan shape: contribution 20% (component,
  taxable, retirement), deduction 11% (deduction, retirement).

New Config catalog **1018 (SsfRate)**, seeded:

| Code | Label | additionalValue1 |
|---|---|---|
| `EMPLOYEE_SHARE` | SSF Employee Share (% of Basic) | `11` |
| `EMPLOYER_SHARE` | SSF Employer Share (% of Basic) | `20` |

`GET /api/configs/dropdown/1018` (no permission grant needed) returns both rows; the percentage
is in `additionalValue1`. Use it to **prefill** the SSF component/deduction values in the Add
Salary Revision form instead of hardcoding 11/20, and edit the rates via the normal Configs
admin CRUD when the law changes. The salary calculator below reads this catalog server-side.

### Payslip handling of the employer share (round 2 — "SSF_CONTRIBUTION is also a deduction")

A component flagged `isRetirementContribution: true` (the employer SSF/EPF share) is money
**deposited with the fund, not cash paid out**. Payslips, the monthly tax breakdown, and payroll
run slips now reflect that automatically: the component still appears as an earning (it is
income, and taxable when flagged so), and an **equal deduction line with the same code** is
emitted alongside it — the remittance to the fund — so gross shows the full package while net
pay no longer includes the employer share as take-home cash. Render the pair as
"SSF_CONTRIBUTION" under earnings and under deductions; no UI math needed. (This only triggers
on retirement-flagged **components** — the employee's own `SSF_DEDUCTION`/`CIT_DEDUCTION`
deduction rows are unaffected.) Existing Draft runs pick this up via **Refresh** (§2); note it
has no effect on a plan whose SSF_CONTRIBUTION isn't retirement-flagged yet — fix the plan
first (see the data problems above).

## 4. Salary calculator for HR — NEW MODULE

"Structure a salary from a target figure": HR picks whether the fixed number is the take-home
(**NetPayment**), the payable gross (**GrossPayment**), or the employer's total cost (**CTC**),
and the backend solves the full structure — Basic/allowance split, both SSF shares, optional
festival bonus / CIT savings / capped insurance premiums, and TDS via the fiscal year's real
tax slabs (same `TaxCalculator` the payroll run uses, so the resulting plan reproduces these
exact numbers at run time). This follows the standard Nepal salary sequence: gross annual
income (salary + allowances + bonus + taxable benefits) − retirement contributions
(least-of-three: actual SSF/CIT vs ⅓ of income vs the fiscal year's cap) − capped insurance
premiums → progressive slabs.

New sidebar entry: **Payroll Management → Salary Calculator** (`SALARY_CALCULATOR`, url
`/apps/payroll/salary-calculator`).

```
POST /api/salarycalculator                  permission: SALARY_CALCULATOR
```

Request (round 2 — bonus/basic/CIT/insurance knobs added):

```json
{
  "basis": 2,                          // 1 NetPayment | 2 GrossPayment | 3 Ctc
  "amount": 40000,                     // MONTHLY figure for the chosen basis
  "fiscalYearId": null,                // optional; null = the fiscal year marked current
  "assessmentType": 1,                 // 1 Individual (default) | 2 Couple
  "basicSalaryAmount": null,           // optional; PIN Basic to this exact monthly amount
  "basicPercentOfGross": 60,           // used only when basicSalaryAmount is null; default 60
  "includeSsf": true,                  // false = no SSF lines at all
  "annualBonusAmount": null,           // optional; Dashain/festival bonus, once a year, taxable
  "monthlyCitAmount": null,            // optional; CIT savings deduction, retirement-flagged
  "annualLifeInsurancePremium": null,  // optional; deductible up to the LIFE cap (40,000 seeded)
  "annualHealthInsurancePremium": null // optional; deductible up to the HEALTH cap (20,000 seeded)
}
```

Semantics of the new knobs:

- `basicSalaryAmount` beats `basicPercentOfGross` — use it when the school fixes Basic (e.g.
  government scale) and flexes only the allowance. `400 VALIDATION_ERROR` if the pinned Basic
  exceeds the computed gross.
- `annualBonusAmount` (suggested line: `FESTIVAL_BONUS`, OneTime frequency) is taxed inside the
  annual calculation but **excluded from the monthly cash figures** — monthly `netPayment` is
  the regular month; `annualGrossPayment`/`annualNetPayment`/`annualCtc` include the bonus.
- `monthlyCitAmount` (suggested line: `CIT_DEDUCTION`, new option in Deduction Type catalog
  1014) reduces take-home and feeds the retirement exemption alongside SSF.
- The insurance premiums don't appear as pay lines — they reduce taxable income up to each
  type's cap from Config catalog 1015 and come back as `suggestedInsurancePremiums` for the
  revision form.
- Reminder: the retirement exemption is `min(actual contributions, grossAnnual/3,`
  `fiscalYear.retirementExemptionCapAmount)` — while the fiscal year's cap is 0 (both years in
  the dev DB today), SSF/CIT reduce **nothing**; set the cap on the fiscal year first.

Response `data` (all money figures **monthly** unless prefixed `annual`; the numbers below
assume a fiscal year whose first Individual slab is 1% — your configured slabs decide the tax):

```json
{
  "basis": 2,
  "fiscalYearId": "…", "fiscalYearCode": "2084/85", "assessmentType": 1,
  "basicPercentOfGross": 60, "ssfEmployeeRatePercent": 11, "ssfEmployerRatePercent": 20,
  "basicSalary": 24000.00,
  "otherAllowance": 16000.00,
  "grossPayment": 40000.00,
  "ssfEmployeeDeduction": 2640.00,
  "ssfEmployerContribution": 4800.00,
  "monthlyCitDeduction": 0.00,
  "monthlyTax": 373.60,
  "netPayment": 36986.40,
  "ctc": 44800.00,
  "annualBonusAmount": 0.00,
  "annualGrossPayment": 480000.00, "annualNetPayment": 443836.80, "annualCtc": 537600.00,
  "taxCalculation": { "grossAnnualIncome": 537600.00, "retirementContributionAnnual": 89280.00, "retirementExemption": 89280.00, "insuranceDeduction": 0.00, "annualTaxableIncome": 448320.00, "annualTax": 4483.20, "breakdown": [ … ] },
  "suggestedComponents": [
    { "code": "BASIC",            "valueType": 2, "value": 24000.00, "frequencyType": 1, "isTaxable": true,  "isRetirementContribution": false },
    { "code": "OTHER_ALLOWANCE",  "valueType": 2, "value": 16000.00, "frequencyType": 1, "isTaxable": true,  "isRetirementContribution": false },
    { "code": "SSF_CONTRIBUTION", "valueType": 1, "value": 20,       "frequencyType": 1, "isTaxable": true,  "isRetirementContribution": true }
  ],
  "suggestedDeductions": [
    { "code": "SSF_DEDUCTION",    "valueType": 1, "value": 11,       "frequencyType": 1, "isTaxable": false, "isRetirementContribution": true }
  ],
  "suggestedInsurancePremiums": []
}
```

(The example assumes a fiscal year whose `retirementExemptionCapAmount` is ≥ 89,280 — with the
cap still 0 the exemption is 0 and the tax rises accordingly.) `suggestedComponents`/
`suggestedDeductions`/`suggestedInsurancePremiums` are shaped exactly like the line inputs of
`POST /api/employees/{id}/salaries` — a "Use this structure" button can copy them straight into
the Add Salary Revision form, or skip the form entirely with Assign below. Identities that
always hold: `grossPayment = basicSalary + otherAllowance`; `netPayment = grossPayment −
ssfEmployeeDeduction − monthlyCitDeduction − monthlyTax`; `ctc = grossPayment +
ssfEmployerContribution`; each annual figure = `12 × monthly + annualBonusAmount`.
`taxCalculation.grossAnnualIncome` is the *taxable* annual gross (it includes the employer SSF
contribution as a taxable benefit and the bonus), so it is deliberately **not**
`12 × grossPayment`.

Failures: `400 VALIDATION_ERROR` (bad amount/percent, pinned Basic exceeding gross, no slabs
for the assessment type), `404 NOT_FOUND` (fiscal year id unknown, or nothing marked current
when `fiscalYearId` omitted).

### Assign to an employee (round 2)

```
POST /api/salarycalculator/assign           permission: SALARY_CALCULATOR_ASSIGN
```

Body = the **same fields as the calculate call** plus `employeeId` and `effectiveFromDate`.
The backend re-runs the calculation server-side (never trust a client-echoed structure) and
persists the suggested lines as a real salary revision through the exact same path as
`POST /api/employees/{id}/salaries` — so the date-conflict check (`409 CONFLICT` if a revision
already exists effective that date), catalog-code validation, and audit stamping all apply.
Response `data`: `{ calculation: <same shape as above>, salary: <EmployeeSalaryDto — the
created revision> }`. UI flow: calculate → show the breakdown → "Assign to Employee" button →
employee picker + effective-from date → POST assign → navigate to the employee's Pay & Taxes
tab. A raise later = another assign with a newer `effectiveFromDate` (revisions are append-only
history).

## 4½. Bulk salary adjustments (round 2 — NEW ENDPOINT)

The "give everyone a Dashain allowance / festival bonus / leave encashment this month" case —
one call instead of one `POST /api/employees/{id}/adjustments` per employee (mirrors the fee
side's `POST /api/feeinvoices/adjustments/bulk`).

```
POST /api/employees/adjustments/bulk        permission: EMPLOYEE_SALARY_ADJUSTMENT_BULK
```

```json
{
  "fiscalYearId": "…",
  "monthIndex": 3,                    // 1 Shrawan … 12 Ashad
  "adjustmentTypeCode": "BONUS",      // catalog 1016: BONUS, INCENTIVE, OVERTIME, ARREAR, LATE_FINE, UNPAID_LEAVE, OTHER
  "direction": 1,                     // 1 Increase (earning) | 2 Decrease (deduction)
  "valueType": 2,                     // 1 Percentage of BASIC | 2 FixedAmount
  "value": 10000,
  "quantity": null,                   // day count for UNPAID_LEAVE, optional multiplier otherwise
  "remarks": "Dashain allowance 2082",
  "employeeIds": [],                  // non-empty = exactly these employees
  "employeeCategoryCode": null        // used only when employeeIds is empty: narrow "all payroll-eligible" to one category (e.g. "ACADEMIC")
}
```

Scope resolution: a non-empty `employeeIds` wins (unknown ids are reported in `skipped`, the
rest proceed); otherwise **every payroll-eligible employee** (Active/OnLeave with a
compensation plan), optionally narrowed by `employeeCategoryCode`. All rows are created
Pending in one transaction. Response `data`: `{ fiscalYearId, monthIndex, adjustmentTypeCode,
createdCount, adjustments: [SalaryAdjustmentDto…], skipped: [{ employeeId, employeeName,
reason }] }`. Same guards as the single endpoint: `409 CONFLICT` if that month's run is already
past Draft; if a Draft run exists, the adjustments show up after **Refresh** (§2). UI: a "Bulk
Adjustment" button on the Salary Generation page (or Employee list) with a type/amount form and
an "All employees / category / pick employees" scope selector.

## 5. Frontend-only items from the issue report (no API change)

The backend repo has no frontend project, so these are UI guidance only:

- **Add Salary Revision modal**: make the body scrollable (`max-height` + `overflow-y: auto` on
  the form region, header/footer fixed) — with 6+ component rows the Add button currently walks
  off-screen. Show a **live computed Gross** in the modal header: sum every Monthly component's
  amount, resolving `valueType: 1` (Percentage) rows against the BASIC row's value — same rule
  the backend uses. The salary calculator response can drive this display directly.
- **Salary Slip line-entry row** (Run Detail → slip modal): replace the free-text Description
  entry with the same autocomplete/combobox pattern as the revision modal — Type (Earning/
  Deduction) + a code dropdown fed by `GET /api/configs/dropdown/1013` (components) or `/1014`
  (deductions), description auto-filled from the option label, amount field. The API
  (`POST /api/payrollruns/{id}/slips/{slipId}/lines`) already accepts `componentCode` alongside
  `description`/`amount` — send the picked code instead of leaving it null.
- **Run Detail / list refresh button**: wire to the new endpoint from §2.

## Permission rows added (auto-granted to SuperAdmin)

| Code | Endpoint |
|---|---|
| `PAYROLL_RUN_REFRESH` | `POST /api/payrollruns/{id}/refresh` |
| `SALARY_CALCULATOR` | `POST /api/salarycalculator` (visible sub-menu under Payroll Management) |
| `SALARY_CALCULATOR_ASSIGN` | `POST /api/salarycalculator/assign` |
| `EMPLOYEE_SALARY_ADJUSTMENT_BULK` | `POST /api/employees/adjustments/bulk` |

## Config catalog changes (seeded on next startup)

- **1018 SsfRate** — `EMPLOYEE_SHARE` (11) / `EMPLOYER_SHARE` (20), rate in `additionalValue1`.
- **1014 DeductionType** gained `CIT_DEDUCTION` ("Citizen Investment Trust (CIT)") — the
  optional, employee-chosen savings deduction (no fixed percentage; a chosen amount or a
  percentage of Basic), retirement-flagged so it counts toward the exemption.
