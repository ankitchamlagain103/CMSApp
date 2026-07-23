# Compensation basis (Net / Gross / CTC) on the Employee "Add Revision" flow — implementation guide

## The bug this fixes

The Employee/Teacher **Compensation Plan** tab's "Add Revision" modal is a
raw line-item editor: HR types Basic, allowances, SSF %, etc. one field at a
time with **no concept of what the target number represents**. Nothing stops
someone from entering numbers that don't reconcile with each other, which is
exactly what produced "Gross Monthly is greater than CTC" — that combination
is structurally impossible when a salary is built the way the **Salary
Calculator** (`POST /api/salarycalculator`) already builds one, because it
enforces `Ctc = GrossPayment + SsfEmployerContribution` by construction
(`Ctc` can never be smaller than `GrossPayment`). The fix is not a new
backend endpoint — it's routing the "Add Revision" flow through the
calculator that already exists, instead of a disconnected manual form.

## Why "Gross Monthly" and "CTC" looked inconsistent

Two different things in this system are both loosely called "gross," and
comparing them directly is what produced the confusing number:

| Field | Where it appears | What it actually is |
|---|---|---|
| `grossPayment` | Salary Calculator (`POST /api/salarycalculator`) | **Cash-only** gross: `basicSalary + otherAllowance`. Deliberately **excludes** the employer's SSF contribution — that money never reaches the employee's bank account. |
| `ctc` | Salary Calculator | `grossPayment + ssfEmployerContribution`. Always `>= grossPayment` by construction — this is the actual guarantee "Gross Monthly is greater than CTC" was violating. |
| `taxCalculation.grossAnnualIncome` (shown as **`grossMonthly`** on the Tax Calculation / Investment & Tax Planning tabs, `= grossAnnualIncome / 12`) | Tax Calculation, Tax Planning, Payslip | The **taxable** annual gross — sum of every `isTaxable: true` component. This **includes** the employer SSF contribution (it's assessable income under Nepal law — see the SSF-taxable fix from earlier), so numerically it sits much closer to `ctc` than to `grossPayment`. It also smears any One-Time component (a festival bonus) across all 12 months, so it isn't literally "this month's paycheck" either. |

So `grossMonthly` (Tax Planning tab) and `ctc` (Salary Calculator) are
answering two different questions that happen to look similar — one is a
tax-code concept ("annual taxable income ÷ 12"), the other is an HR/costing
concept ("payable cash + employer's SSF top-up"). They were never guaranteed
to be equal, and when a salary was hand-built (SSF percentage typo'd,
bonus omitted, etc.) they can end up in an order that looks backwards. Once a
salary is built via the calculator, the two numbers are close by
construction and the apparent contradiction goes away.

**Don't try to make `grossMonthly` and `ctc` the same field** — they answer
different questions and both are correct for what they represent. The real
fix is preventing ad-hoc, disconnected data entry in the first place.

## What already exists (no backend change needed)

Full request/response reference: `Docs/payroll_fixes_implementation_guide.md`
§4. Summary of the two calls the UI needs:

```
POST /api/salarycalculator            permission: SALARY_CALCULATOR        (preview, persists nothing)
POST /api/salarycalculator/assign     permission: SALARY_CALCULATOR_ASSIGN (persists as a real salary revision)
```

Both take the same body shape (`AssignSalaryStructureCommand` extends
`CalculateSalaryStructureCommand` with `employeeId` + `effectiveFromDate`):

```jsonc
{
  "basis": 3,                          // 1 NetPayment | 2 GrossPayment | 3 Ctc
  "amount": 110000,                    // MONTHLY figure, meaning depends on basis
  "fiscalYearId": null,                // null = the fiscal year marked current
  "assessmentType": 2,                 // 1 Individual | 2 Couple
  "basicSalaryAmount": null,           // optional: pin Basic to an exact monthly amount
  "basicPercentOfGross": 60,           // used only when basicSalaryAmount is null
  "includeSsf": true,
  "annualBonusAmount": 26821.92,       // optional: Dashain/festival bonus
  "monthlyCitAmount": null,
  "annualLifeInsurancePremium": null,
  "annualHealthInsurancePremium": null,
  // assign only:
  "employeeId": "...",
  "effectiveFromDate": "2026-07-17"
}
```

`AssignStructureAsync` re-runs the calculation server-side (never trusts a
client-echoed structure) and persists through the exact same
`IEmployeeService.AddSalaryAsync` path `POST /api/employees/{id}/salaries`
uses — same date-conflict (`409 CONFLICT` on an existing revision at that
effective date) and catalog-code validation.

## Recommended UI flow

Replace the current single-step "Add Revision" modal with a two-step flow on
the Employee/Teacher profile's Compensation Plan tab:

**Step 1 — Structure inputs**
- **Calculation Basis** selector: `Net Payment` / `Gross Payment` / `CTC`
  (radio buttons or segmented control — exactly like the standalone Salary
  Calculator page already has).
- **Amount** (monthly, meaning follows the selected basis).
- **Fiscal Year** (default: the one marked current) and **Assessment Type**
  (prefill from the employee's most recent salary revision if one exists,
  else default Individual).
- Collapsible **Advanced** section: `Basic Salary Amount` (pin exactly) OR
  `Basic % of Gross` (default 60, mutually exclusive with the pin),
  `Include SSF` toggle, `Annual Bonus`, `Monthly CIT`, `Annual Life/Health
  Insurance Premium`.

**Step 2 — Live preview, then commit**
- Debounce input changes and call `POST /api/salarycalculator` to show a
  live breakdown as HR types: Basic / Other Allowance / both SSF shares /
  Monthly Tax / **Net Payment** / **CTC** side by side, plus the tax slab
  table (same shape the Tax Planning tab already renders — reuse that
  component). This is where HR visually confirms the numbers make sense
  *before* anything is saved.
- **"Assign to Employee"** button — calls `POST /api/salarycalculator/assign`
  with the employee's id (from the route) and an **Effective From** date
  picker (default: today). On `409`, surface the conflict message and let
  HR pick a different date (same UX as the existing manual form already
  needs to handle). On success, navigate to (or refresh) the employee's Pay
  & Taxes tabs — Compensation Plan, Tax Details, and Investment & Tax
  Planning will now show mutually consistent numbers, since every figure
  traces back to the one server-side calculation.

**Keep the existing raw manual-entry form**, relabeled "Advanced / Custom
Structure" and reachable via a secondary link — it's still needed for
structures the calculator's Basic+Allowance model can't express (more than
one allowance line, a loan deduction alongside SSF, etc.), and for *editing*
an existing revision's individual lines after the fact. Make the
basis-driven flow the **default** path so a fresh compensation plan can't be
entered without an internally-consistent Gross/CTC relationship in the first
place.

## Things worth knowing before wiring this up

- `grossPayment = basicSalary + otherAllowance` always; `ctc = grossPayment +
  ssfEmployerContribution` always — if a UI ever shows `ctc < grossPayment`
  after this change, that's a real bug (open an issue), not a modeling
  ambiguity like the one this guide fixes.
- `annualBonusAmount` is taxed annually but excluded from every **monthly**
  cash figure (`grossPayment`/`netPayment`/`ctc`) — only the `annual*`
  siblings include it. Label the preview clearly (e.g. a footnote: "excludes
  the one-time bonus, see Annual figures below") so HR doesn't expect the
  bonus to shrink or inflate the monthly numbers.
- `taxCalculation.grossAnnualIncome` in the preview response will be close
  to `annualCtc` but not necessarily identical to it — see the table above.
  Don't treat a small gap there as an error.
- If `fiscalYear.retirementExemptionCapAmount` is `0` for the selected
  fiscal year, SSF/CIT contributions reduce **no** tax at all (the `min(a,
  b, c)` exemption collapses to 0) — worth a inline warning in the preview
  UI ("Retirement exemption cap is 0 for this fiscal year — contributions
  won't reduce tax until an admin sets a real cap") rather than a silent,
  confusing high-tax number.
- Failures: `400 VALIDATION_ERROR` (bad amount/percent, pinned Basic
  exceeding the computed gross, no tax slabs configured for the assessment
  type), `404 NOT_FOUND` (fiscal year id unknown, or none marked current).
