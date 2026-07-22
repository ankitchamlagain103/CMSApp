# Fee Advance-Billing Quote, Annual Settlement & UX Fixes — Implementation Guide (2026-07-17, round 3)

Third round of fee-module fixes, on top of `Docs/fee_module_fixes_implementation_guide.md`
(round 1) and `Docs/fee_advance_payment_and_ux_implementation_guide.md` (round 2, which added
advance payment itself). All responses use the standard envelope
`{ responseCode, responseMessage, data }`.

## What changed, at a glance

| Reported issue | Fix |
|---|---|
| Collect Payment form has an "Allow advance billing" toggle but no way to know the amount for X months | New read-only `GET /api/feepayments/advance-quote` (§1) |
| Statement of Account should be its own menu under Student Management | New sidebar entry, same endpoint (§2) |
| Fee summary's "Monthly Recurring" only showed Rs 225, ignoring the Rs 100,000 Annual Fee's installment share | `FeeStructureSummaryDto` now folds the Annual item's per-month share in (§3) |
| Bulk Adjustment form has no Fee Category field | `FeeAdjustment` gained an optional `FeeCategoryCode` (§4) |
| "Refresh" is missing after Generate Invoices | Frontend-only — this repo has no frontend code (§5) |
| Need to pay an Annual Fee (currently mid-installment-schedule) in full on one invoice, dynamically | New `POST .../settle-annual-in-full` + a redesigned, self-healing installment engine (§6) |

## 1. "How much for X months?" — the advance-billing quote

The Collect Fee Payment form's gap: the cashier can toggle "Allow advance billing," but nothing
tells them what to type into **Amount** for "3 months." This new endpoint answers exactly that,
without writing anything to the database:

`GET /api/feepayments/advance-quote?enrollmentId=&monthsToPay=3&paymentDate=&applyRuleDiscounts=true`

```json
{
  "enrollmentId": "GUID",
  "monthsRequested": 3,
  "monthsAvailable": 3,
  "grossAmount": 118500.00,
  "ruleDiscountTotal": 5000.00,
  "netAmountToCollect": 113500.00,
  "months": [
    { "billingYear": 2026, "billingMonth": 7, "netAmount": 39250.00, "isAlreadyGenerated": true },
    { "billingYear": 2026, "billingMonth": 8, "netAmount": 39250.00, "isAlreadyGenerated": false },
    { "billingYear": 2026, "billingMonth": 9, "netAmount": 40000.00, "isAlreadyGenerated": false }
  ],
  "ruleDiscounts": [
    { "ruleCode": "TEST_RULE", "ruleName": "3 Months Pay", "amount": 5000.00, "description": "3 Months Pay (3 months paid together)" }
  ]
}
```

- `monthsToPay` counts from the earliest owed month forward — currently-open (already
  generated) invoices first, then as many new months as needed, exactly what a real advance
  payment of that amount would settle.
- **Purely a preview** — no invoice is created, nothing is written. It internally builds the
  same projected months the real payment would (so the numbers match exactly), then discards
  them.
- `netAmountToCollect` already has any `AdvanceMonthsDiscount`/`EarlyPaymentDiscount` rule
  applied (pass `applyRuleDiscounts=false` to see the pre-discount total instead).
- **UI flow**: cashier types/picks "Months to Pay: 3" → call this endpoint → auto-fill the
  Amount field with `netAmountToCollect` and show the `months[]` breakdown ("Jul (existing),
  Aug, Sep — Rs 5,000 discount applied") → cashier proceeds to the existing
  `POST /api/feepayments/preview` → `POST /api/feepayments` flow as normal, which independently
  re-validates and re-computes everything (this quote is a convenience, not the source of
  truth — if a rule changes between quote and confirm, confirm wins).
- `monthsAvailable` can come back less than `monthsRequested` only if the class has no active
  fee structure to project new months against (existing open months are still quoted; the
  `NOT_FOUND`/`VALIDATION_ERROR` cases mirror the real payment's).

## 2. Statement of Account under Student Management

A new sidebar entry, `Statement of Account`, appears under **Student Management** alongside
Students/Guardians/Enrollments. It points at the exact same
`GET /api/feeinvoices/account-statement/{enrollmentId}` endpoint the Fee Generation page's tab
already uses (round 2) — no new backend logic, just a second way to reach it for staff who think
of it as a student record rather than a fee-management task. `MenuSeeder` code:
`STATEMENT_OF_ACCOUNT_LIST` (`Controller: FeeInvoices`, `Action: GetAccountStatement`). Grant it
independently of the Fee Management copy if a role should see one but not the other.

## 3. Fee summary now reflects the Annual Fee's actual installment configuration

**Root cause of "Monthly Recurring only shows Rs 225":** `GET /api/enrollments/{id}/fee-structure`
built its summary by bucketing items strictly by `FrequencyType` — an `Annual` item's full
amount went into `annualTotal` only, even when the item's `installmentCount` means it's
genuinely billed **every month** as an `AnnualInstallment` line (round 1). A student with a
Rs 250/month Transportation Fee and a Rs 100,000/year Annual Fee split over 12 installments
was actually paying ~Rs 8,583/month, but the summary said Rs 225.

**Fixed**: `FeeStructureSummaryDto` gained `annualInstallmentMonthlyShare` — the per-month share
of every Annual item with `installmentCount >= 2` — and `monthlyRecurringTotal` now **includes**
it:

```json
{
  "monthlyRecurringTotal": 8583.33,
  "annualInstallmentMonthlyShare": 8333.33,
  "annualTotal": 100000.00,
  "oneTimeTotal": 10000.00,
  "refundableDepositTotal": 2500.00,
  "totalDiscountReduction": 858.33,
  "totalScholarshipReduction": 0.00,
  "netMonthlyRecurring": 7725.00
}
```

- `annualTotal` still shows the full yearly amount (installment-split or not) — that number
  didn't change.
- Discounts/scholarships now reduce against the **combined** monthly total
  (`monthlyRecurringTotal`, structure-Monthly + the Annual installment share), matching what a
  real generated invoice's recurring subtotal actually discounts against — so the 10% Sibling
  Discount in the example above is now Rs 858.33, not Rs 25.
- `FeeLineItemDto` (the `feeItems[]` rows) gained `installmentCount` so the UI can show a flag
  like "12 Installments" on the Annual Fee row, matching the "Flags" column already used for
  "Refundable."
- The `FeeReceipt` document template gained the matching `{{AnnualInstallmentMonthlyShare}}`
  placeholder.

**This was purely a read-model fix** — actual invoice generation already billed the Annual
installment correctly every month; only the profile-page *summary* was under-reporting it.

## 4. Fee category on adjustments

`FeeAdjustment` (both the single-student and bulk endpoints from round 2) gained an optional
`feeCategoryCode`:

```json
{
  "academicYearId": "GUID",
  "academicClassId": "GUID",
  "billingYear": 2026,
  "billingMonth": 9,
  "adjustmentTypeCode": "EDUCATION_TOUR",
  "feeCategoryCode": "EXAM_FEE",
  "direction": 1,
  "valueType": 1,
  "value": 10,
  "remarks": "10% exam-fee surcharge for late registration"
}
```

- Must be a `FeeCategory` (catalog 1010) option when present, same validation as
  `FeeStructureItem.FeeCategoryCode`/`FeeRule.FeeCategoryCode`.
- **A `Percentage` adjustment with a category set resolves against just that category's
  recurring subtotal** on the invoice, not the whole invoice's — e.g. a 10% adjustment scoped to
  `EXAM_FEE` only discounts/surcharges the Exam Fee line, leaving Transportation/Annual Fee
  untouched. Omit it (as before) to resolve against the whole recurring subtotal.
- A `FixedAmount` adjustment's category is informational/categorization only — the flat amount
  is unaffected, but the generated line's `feeCategoryCode` is stamped and the description
  reads `"Adjustment - EDUCATION_TOUR (EXAM_FEE)"` so reports/receipts can group by category.
- Applies to `POST /api/feeinvoices/adjustments`, `POST /api/feeinvoices/adjustments/bulk`, and
  `PUT /api/feeinvoices/adjustments/{id}` alike; `FeeAdjustmentDto` returns it on every read.

## 5. "Refresh" after Generate Invoices

This is a **frontend concern this repository can't fix** — there is no frontend/UI project in
this codebase (`CMSApp.slnx` is Domain/Application/Infrastructure/WebApi only; the "Admin
Portal" shown in the screenshots runs from a separate repository on `localhost:3000`). The
backend already supports the fix cleanly: `POST /api/feeinvoices/generate`'s response
(`FeeGenerationResultDto`) returns `generatedInvoiceIds` and the full `skipped[]` list the
instant generation finishes — the frontend's Generate button handler should invalidate/refetch
the `GET /api/feeinvoices` query (or simply call it again) right after a successful generate
response, the same way it presumably already does after `POST /api/feepayments`. No API change
needed here; flag this to whoever owns the frontend repository.

## 6. Pay an Annual Fee in full, on any Draft invoice, dynamically

### The redesign that made this possible

Previously, an Annual item's per-invoice installment amount was computed purely from **where in
the schedule this invoice falls** (`installmentIndex / installmentCount`, counted from an
"anchor" month). That meant there was no safe way to charge more than the scheduled share on
one invoice — the *next* invoice would still compute its own share independently and the family
would be double-billed.

**`FeeInvoiceFactory`'s Annual branch is now remaining-balance-driven instead of
schedule-position-driven**: for every invoice it generates, it sums what's *actually already
been billed* for that item across the enrollment's other invoices (reading real
`AnnualInstallment` line amounts, not a counter) and bills the **remainder** — spread over
whatever installments are still left, with the final installment absorbing rounding exactly as
before. When nothing unusual happens this produces byte-identical numbers to the old schedule
(verified: an even share, resummed, telescopes to the same even share). The payoff:

- **Pay in full early** → future invoices see the remaining balance is 0 and stop billing that
  item entirely, automatically. No flag, no new table, no schema change.
- **Pay a partial acceleration** (more than the scheduled share, less than full) → future
  invoices automatically re-spread the smaller remainder over the remaining installments —
  "X no of months" support falls out of the design for free, without a dedicated "how many
  months" input: whatever you charge on one invoice is honored, and everything after adapts.
- **Delete or edit a Draft's line before finalizing** → already worked, now also correctly
  un-books that amount for the remaining-balance calculation of later months, since the
  calculation always reads current, real, persisted line data.

### The discoverable action

`POST /api/feeinvoices/{id}/lines/{lineId}/settle-annual-in-full` — Draft invoices only,
`lineId` must be an `AnnualInstallment`-sourced line. Recomputes the item's true remaining
balance (full annual amount minus what's billed on the enrollment's *other* invoices — this
line's own current amount is excluded from that sum since it's about to be replaced) and sets
this line's `amount` to that full remainder, relabeling it `"<Category> (Annual, paid in
full)"`.

```json
// before
{ "category": "Annual Fee", "description": "ANNUAL (Annual, installment 1/12)", "amount": 8333.33 }

// after POST .../settle-annual-in-full
{ "category": "Annual Fee", "description": "Annual Fee (Annual, paid in full)", "amount": 100000.00 }
```

- `409 CONFLICT` if the item is already fully billed elsewhere (nothing left to settle here —
  e.g. calling it twice, or on a class where the Annual item isn't installment-split at all).
- `404`/`400` for an unknown line or a line that isn't an Annual installment.
- Invoice totals recompute immediately (`RecomputeTotals`), same as any other line edit.
- **No admin UI needs a separate "how many months" input** for the common case — this one
  button covers "pay the whole year now." For a partial acceleration (pay 6 of 12 months'
  worth at once), the existing "editable Draft line" capability already covers it: just change
  the line's `amount` via `PUT .../lines/{lineId}` to whatever's being paid; the engine
  re-derives everything after from there.

## 7. Permissions & migration notes

New `MenuSeeder` permission rows (granted to SuperAdmin automatically; grant to other roles via
`POST /api/roles/claims`):

| Code | Endpoint |
|---|---|
| `FEE_PAYMENT_ADVANCE_QUOTE` | `GET /api/feepayments/advance-quote` |
| `STATEMENT_OF_ACCOUNT_LIST` | `GET /api/feeinvoices/account-statement/{enrollmentId}` (second entry, Student Management) |
| `FEE_INVOICE_SETTLE_ANNUAL` | `POST /api/feeinvoices/{id}/lines/{lineId}/settle-annual-in-full` |

**Migration needed (user-owned, as always)**: one new nullable column —
`dbo.fee_adjustments.fee_category_code varchar(100) NULL`. Nothing else changed schema-wise
this round — the annual-settlement redesign reuses existing columns entirely (it just reads
`FeeInvoiceLine.Amount`/`FeeStructureItemId` from history instead of doing index arithmetic).
