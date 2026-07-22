# Fee Payment UX, Config Labels & Fee-Rule Fix — Implementation Guide (2026-07-19)

Backend response to the 2026-07-19 feedback batch: payment preview now shows *what* is being
collected, every config-backed code now ships with its human-readable label, the fee-rule
"never applies" bug is fixed, Statement of Account moved off the sidebar into the student
profile, and the Monthly Adjustments tab (including the auto-generated Carry Correction rows)
is explained with its edit/cancel APIs. **No migration needed — code/seed-only.**

---

## 1. Payment preview now lists the fees being collected

`POST /api/feepayments/preview` — each entry in `allocations` now carries a `lines` array:
the full line breakdown of that invoice (category fees, discounts, scholarships,
adjustments), so the Collect Fee Payment modal can show *which category fees* the payment
covers before the cashier confirms.

```json
{
  "allocations": [
    {
      "feeInvoiceId": "…",
      "invoiceNo": "INV20260012",
      "billingYear": 2026,
      "billingMonth": 8,
      "amount": 25250.00,
      "settlesInvoice": true,
      "isNewlyGenerated": false,
      "lines": [
        { "source": 1, "feeCategoryCode": "TUITION",   "feeCategoryLabel": "Tuition Fee",   "description": "Tuition Fee (Monthly)",   "amount": 20000.00 },
        { "source": 1, "feeCategoryCode": "TRANSPORT", "feeCategoryLabel": "Transport Fee", "description": "Transport Fee (Monthly)", "amount": 5250.00 }
      ]
    }
  ]
}
```

- `source` is the existing `FeeLineSource` enum (1 StructureItem, 2 AnnualInstallment,
  3 OneTimeCharge, 4 Discount, 5 Scholarship, 6 MonthlyAdjustment, 7 RuleDiscount, 8 Manual).
- `lines` is filled **only on the preview response**. The confirm response
  (`POST /api/feepayments`) leaves it empty — after confirming, the printable receipt
  (`GET /api/feepayments/{id}/receipt`) already renders the full line detail.
- Advance-billed months (`isNewlyGenerated: true`) also carry `lines`, so the cashier sees
  next month's composition even though that invoice doesn't exist in the database yet.

**Typical UI**: in the Collect Fee Payment modal, after Preview, render each allocation as an
expandable row whose children are `lines` (label + amount; negative amounts are discounts).

## 2. Fee rules now actually apply (advance/early-payment discount fix)

**The bug**: a rule discount only fired when the tendered amount fully settled the qualifying
invoices *at their pre-discount balances*. Tendering the full total earned the discount but
was then rejected as an over-payment ("collect X instead"); tendering the recommended
discounted amount X no longer settled the last month pre-discount, so the rule silently
stopped matching. Net effect: **no rule could ever be applied on a confirmed payment.**

**The fix** (`FeePaymentService.BuildPlanAsync`): the discount a rule earns now counts toward
the settlement itself. The planner tries the largest FIFO prefix of open invoices first, asks
the rule engine what that set would earn, and accepts the first set where
`tendered amount + earned discounts >= the set's pre-discount balances`.

Worked example with the "3 Months Pay — Rs 5000 off when ≥ 3 months together" rule (scoped
to Nursery) and three open months of Rs 25,250 each (total 75,750):

| Cashier tenders | Before the fix | After the fix |
|---|---|---|
| 75,750 (full total) | Discount shown, but confirm rejected as over-payment | Preview: `unallocatedAmount: 5000` → "collect 70,750 instead" |
| 70,750 (discounted total) | Rule didn't match → month 3 left partially paid | Rule applies; all 3 months settle in full; Rs 5,000 `RuleDiscount` line lands on the newest settled invoice |

Notes:
- Also fixes `EarlyPaymentDiscount` when collecting the discounted amount.
- `GET /api/feepayments/advance-quote` already assumed the discount counts toward settlement;
  its `netAmountToCollect` is now actually collectible — quote → fill amount → preview →
  confirm is a consistent loop.
- **Reminder on scope**: a rule with an `academicClassId` only fires for enrollments of that
  class. The screenshot rule was scoped to *Nursery — AY2083* while the payment being tested
  was for an *LKG* student — that combination never matches by design; clear the class scope
  (or create one rule per class) for a school-wide rule.

## 3. Config labels everywhere codes were shown

Every read DTO that stores a Config catalog code now also returns the option's **Label**
(resolved from the catalog; falls back to the code itself if the option was deleted, so it is
never blank). Bind UI columns to the `*Label` fields; keep using codes for lookups/writes.

| Endpoint / DTO | New field(s) |
|---|---|
| `FeeStructureDto.items[]` (`/api/feestructures*`) | `feeCategoryLabel` |
| `GET /api/enrollments/{id}/fee-structure` → `feeItems[]` | `feeCategoryLabel` |
| … → `discounts[]` / `scholarships[]` (also `/discounts`, `/scholarships` lists) | `discountTypeLabel` / `scholarshipTypeLabel` |
| `FeeInvoiceDto.lines[]` (`/api/feeinvoices/{id}`, generate/finalize/line edits) | `feeCategoryLabel` |
| `FeeAdjustmentDto` (`/api/feeinvoices/adjustments*`) | `adjustmentTypeLabel`, `feeCategoryLabel` |
| Payment preview `allocations[].lines[]` | `feeCategoryLabel` (see §1) |
| `EmployeeSalaryDto.components[]/deductions[]/insurancePremiums[]` (`/api/employees/{id}/salaries`, also the teacher aliases) | `componentLabel`, `deductionLabel`, `insuranceTypeLabel` |
| `EmployeeLoanDto` (`/loans` endpoints) | `loanTypeLabel` |
| `SalaryAdjustmentDto` (`/adjustments` endpoints, bulk included) | `adjustmentTypeLabel` |
| Tax Details / payslip lines (`MonthlyLineItemDto` in `…/tax-calculation/monthly`, `…/payslips/{fy}/{m}`) | `label` |

**Generated line descriptions are now label-based too**:

- New fee invoice lines read "Tuition Fee (Monthly)", "Discount - Sibling Discount",
  "Adjustment - Fine (Exam Fee)" instead of raw codes.
- New salary slip lines read "Other Allowance", "SSF Contribution (Employer 20%)" etc. —
  `componentCode` still carries the code.
- **Existing rows keep their old code-based descriptions** (they are immutable snapshots).
  For a Draft payroll run, `POST /api/payrollruns/{id}/refresh` rebuilds its slips and picks
  up the labels; Draft fee invoices pick them up on regenerate. Finalized/paid history stays
  as-is — use the new `*Label` DTO fields for display there.

## 4. Statement of Account: sidebar entry retired → student-profile tab

- The `STATEMENT_OF_ACCOUNT_LIST` menu row (Student Management ▸ Statement of Account) is
  **retired** by `MenuSeeder`'s retire pass on next startup (soft-deleted; existing grants
  are unaffected, the row just disappears from menu trees).
- The UI should instead add a **"Statement" tab on the student profile page** (next to Fee
  Summary), driven by the existing endpoint:
  `GET /api/feeinvoices/account-statement/{enrollmentId}` — use
  `student detail → currentEnrollment.id` for the enrollment id. The permission that gates it
  (`FEE_ACCOUNT_STATEMENT`, under `FEE_INVOICE_LIST`) is unchanged; the Fee Generation page's
  Statement of Account tab keeps working too.

## 5. Monthly Adjustments tab — what it is, and the edit/delete APIs

**What an adjustment is**: a one-month, per-student override folded into that month's invoice
when it is generated — a one-off fine, a one-off charge (Education Tour), or a one-off
discount. Standing monthly reductions belong in Discounts & Scholarships instead.

**Why "Opening Balance / Carry Correction" rows appear automatically**: when a month is
generated and the enrollment still owes money on *earlier* months, the system voids those
older invoices and rolls their unpaid balance into the new invoice via an auto-created
`CARRY_CORRECTION` adjustment ("Carried forward from INV…"). These rows are **system-owned
bookkeeping** — they exist so the new invoice self-documents where the charge came from.
Don't edit or cancel them by hand; cancelling one would silently drop money the student owes.
The UI should render them read-only (hide the edit/cancel actions when
`adjustmentTypeCode === "CARRY_CORRECTION"`).

**Edit / delete APIs** (already live):

| Action | Endpoint | Rules |
|---|---|---|
| Edit | `PUT /api/feeinvoices/adjustments/{adjustmentId}` | **Pending only** — body `{ adjustmentTypeCode, feeCategoryCode?, direction, valueType, value, remarks? }`; `409 Conflict` once Applied/Cancelled |
| Cancel | `DELETE /api/feeinvoices/adjustments/{adjustmentId}` | **Pending only** — sets status Cancelled; `409 Conflict` once Applied |

An **Applied** adjustment (like the ones in the screenshot) is already baked into a generated
invoice. To change it: regenerate that Draft invoice (which re-pends its adjustments), or
cancel the invoice — both flip the adjustment back to Pending, after which edit/cancel work.

## 6. Failure table

| Case | Code |
|---|---|
| Preview/confirm amount exceeds outstanding + 12 advance months | `VALIDATION_ERROR` |
| Confirm with `unallocatedAmount > 0` (over-payment after discounts) | `VALIDATION_ERROR` ("collect exactly X") |
| Edit/cancel a non-Pending adjustment | `CONFLICT` |
| Statement for unknown enrollment | `NOT_FOUND` |
