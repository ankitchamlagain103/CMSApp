# Fee Advance Payment & UX Improvements — Implementation Guide (2026-07-17, round 2)

Second round of fixes to the fee module, on top of `Docs/fee_module_fixes_implementation_guide.md`
(round 1: class-scoped generation, student search, Statement of Account, configurable annual
installments, payment receipts, optional fees at onboarding). This round: advance payment
against the Fee Rules, bulk one-off charges, folding Fee Payments into a Fee Generation tab, a
receipt template redesign, and making optional fees editable after onboarding.

All responses use the standard envelope `{ responseCode, responseMessage, data }`.
**No new migration is required for anything in this document** — every change reuses existing
tables; only round 1's `fee_structure_items.installment_count` column (already applied) was
needed.

## What changed, at a glance

| Reported issue | Fix |
|---|---|
| Fee Rules exist ("pay 3 months together for Rs 5000 off") but there's no way to actually pay in advance | Payments can now bill ahead: `CreateFeePaymentCommand.AllowAdvanceBilling` (§2) |
| "If parents pay 3 months at once, generation shouldn't re-bill those months, but there should be a record of all 3 invoices with discount/scholarship info" | Advance payment creates **real, finalized `FeeInvoice` rows** for the extra months (full line composition — structure items, discounts, scholarships, rule discounts); regular generation already skips any month with a non-Draft invoice, so it naturally won't re-bill them (§2) |
| Need a bulk way to add one-time charges (Education Tour, Examination Fee, Training) across many students | New `POST /api/feeinvoices/adjustments/bulk` (§3) |
| Optional fees (transportation, hostel, …) should be configurable per student, not hardcoded booleans | `PUT /api/enrollments/{id}` now edits `OptionalFeeStructureItemIds` after onboarding too (§1) |
| Remove the "Fee Payments" sidebar menu; make it a tab on Fee Generation | `FEE_PAYMENT_LIST` retired as a visible menu item — same API, new navigation (§4) |
| Receipt should look like the school's own paper receipt sample | New PaymentReceipt template design + school header placeholders (§5) |
| "What is Monthly Adjustments for?" | Explained (§6) |

## 1. Optional fees are now editable from the student profile, not just at onboarding

Round 1 added `optionalFeeStructureItemIds` to `POST /api/enrollments` (the onboarding
checkboxes). That's still there, but it only covered *new* enrollments. Now
`PUT /api/enrollments/{id}` (the student-profile edit endpoint) accepts the same field with
**three-way semantics**, mirroring `UpdateUserCommand.RoleIds` / `UpdateStudentCommand.Guardians`:

```json
{
  "rollNumber": "12",
  "enrollmentDate": "2026-07-17",
  "status": 1,
  "optionalFeeStructureItemIds": ["GUID", "GUID"]
}
```

- **Omit the field (or send `null`)** — existing selections untouched.
- **`[]`** — clear every optional-fee opt-in (the student stops using the bus, drops the
  hostel, …).
- **Non-empty list** — replace-sync to exactly this set (existing selections not in the list
  are removed, new ones added), same validation as onboarding: each id must be an `isOptional`
  item on the enrollment's own class fee structure, or the whole update fails with
  `VALIDATION_ERROR` before anything saves.

**This only affects future invoice generation** — already-generated invoices are immutable
snapshots, exactly like every other "editable configuration vs. locked history" rule in this
module. Toggling a checkbox off mid-year doesn't retroactively remove a transportation line
from July's already-finalized invoice.

**UI:** the student profile's "Fees" tab should now show the same optional-item checkboxes as
onboarding, pre-checked from `GET /api/enrollments/{id}/fee-selections`, and save through this
endpoint on submit.

## 2. Advance payment (pay N months at once)

### The problem this fixes

A Fee Rule like "pay 3 months together, get Rs 5000 off" (`AdvanceMonthsDiscount`,
`MinMonthsTogether = 3`) only evaluates against **fully-settled invoices in the payment**. If
only July's invoice exists, a parent tendering 3 months' worth of money had nowhere for the
extra 2 months to go — the old code rejected any amount over the currently outstanding total as
"over-payment is not supported." The rule could never fire because there was never anything to
fire it against.

### How it works now

`POST /api/feepayments` / `POST /api/feepayments/preview` — request body gains one field:

```json
{
  "enrollmentId": "GUID",
  "paymentDate": "2026-07-17",
  "amount": 118500.00,
  "paymentMode": 2,
  "applyRuleDiscounts": true,
  "allowAdvanceBilling": true
}
```

`allowAdvanceBilling` defaults to `true`. When the tendered `amount` exceeds the enrollment's
currently open (already-generated) invoice total, the service **bills ahead**: it generates and
immediately finalizes the next consecutive billing months' invoices — using the exact same line
composition as regular generation (`FeeInvoiceFactory`, shared by both paths so they can never
disagree: structure items, the student's own optional-fee selections, discounts, scholarships,
pending monthly adjustments) — until the running total covers the tendered amount, capped at
**12 months per payment**. It never re-uses or duplicates a month that already has an invoice
(Draft included), so it can't collide with a period a bulk generation run already claimed.

The fully-settled set (existing open invoices **plus** the newly billed ones) is then what the
Fee Rule engine evaluates — so an `AdvanceMonthsDiscount` rule now has real months to count and
fires correctly. Discounts, scholarships, pending adjustments, and rule discounts all attach to
these new invoices exactly like a normal month.

**Response additions** — both `FeePaymentPreviewDto` and `FeePaymentDto`:

```json
{
  "monthsBilledInAdvance": 2,
  "allocations": [
    { "feeInvoiceId": "GUID", "invoiceNo": "INV20260001", "billingYear": 2026, "billingMonth": 7,  "amount": 39250.00, "settlesInvoice": true, "isNewlyGenerated": false },
    { "feeInvoiceId": "GUID", "invoiceNo": "INV20260017", "billingYear": 2026, "billingMonth": 8,  "amount": 39250.00, "settlesInvoice": true, "isNewlyGenerated": true },
    { "feeInvoiceId": "GUID", "invoiceNo": "INV20260018", "billingYear": 2026, "billingMonth": 9,  "amount": 35000.00, "settlesInvoice": true, "isNewlyGenerated": true }
  ],
  "ruleDiscounts": [
    { "ruleCode": "TEST_RULE", "ruleName": "3 Months Pay", "amount": 5000.00, "description": "3 Months Pay (3 months paid together)" }
  ]
}
```

**Always preview first** (unchanged advice from round 1) — the preview shows exactly which
months will be billed, `monthsBilledInAdvance`, and any earned rule discount, before the cashier
confirms. `isNewlyGenerated: true` is the UI's cue to badge those rows "advance-billed" on the
confirmation screen and receipt.

**"There should have a record in the database of the 3 invoices with discount/scholarship
information"** — that's exactly what this does: each advance-billed month is a real,
independently viewable `FeeInvoice` (status `Generated`, immediately `PartiallyPaid`/`Paid` once
allocated) with its own `FeeInvoiceLine` rows, visible in the ordinary Fee Generation → Invoices
tab and in the Statement of Account, not a lump-sum credit note.

**"Generation shouldn't re-bill those months"** — already true by construction: the bulk
`POST /api/feeinvoices/generate` skips any enrollment/month pair that already has a non-Draft
invoice (`"An invoice for this month already exists with status '...'."`), and advance-billed
invoices are created as `Generated`, not `Draft`. No special-casing was needed.

**Failure cases:**

| Code | When |
|---|---|
| `VALIDATION_ERROR` | tendered amount still exceeds outstanding even after billing up to 12 months ahead (message states the extended outstanding total); `allowAdvanceBilling: false` and amount exceeds current outstanding (old strict behavior); the class has no active fee structure to bill against |

Set `allowAdvanceBilling: false` to keep the old strict "amount cannot exceed what's already
generated" behavior for a cashier workflow that never wants to pre-bill.

**A brand-new enrollment with zero invoices ever generated is payable too** — advance billing
starts from the current billing month (the payment date's month) when there's no existing
invoice to anchor to, so a parent can enroll and immediately pre-pay several months in one go.

## 3. Bulk fee adjustments (Education Tour, Examination Fee, Training, …)

For one-off charges that hit many students at once for a single billing month — the exact case
named in the report — instead of calling `POST /api/feeinvoices/adjustments` once per student:

`POST /api/feeinvoices/adjustments/bulk`

```json
{
  "academicYearId": "GUID",
  "academicClassId": "GUID | omit for the whole year",
  "classSectionId": "GUID | omit for all sections of the class",
  "billingYear": 2026,
  "billingMonth": 9,
  "adjustmentTypeCode": "EDUCATION_TOUR",
  "direction": 1,
  "valueType": 2,
  "value": 1500,
  "remarks": "Grade 9 educational tour, September 2026"
}
```

Same scope shape as `POST /api/feeinvoices/generate` (`academicClassId` = one grade/all
sections, `classSectionId` narrows further, both omitted = the whole academic year).
`direction: 1 Increase` (charge) / `2 Decrease` (credit); `valueType: 1 Percentage / 2 FixedAmount`.
`adjustmentTypeCode` must be a `FeeAdjustmentType` (catalog 1017) option — add a new one via
`POST /api/configs` if "Education Tour"/"Examination Fee"/"Training" don't already exist there.

Creates one **Pending** `FeeAdjustment` per Enrolled enrollment in scope — same idempotent,
report-don't-fail shape as generation:

```json
{
  "billingYear": 2026, "billingMonth": 9,
  "createdCount": 47,
  "skippedCount": 3,
  "createdAdjustmentIds": ["GUID", "..."],
  "skipped": [
    { "enrollmentId": "GUID", "studentName": "Milan Sharma", "reason": "This month's invoice is already generated for this student -- enter the adjustment for a later month or cancel that invoice first." }
  ]
}
```

Each created adjustment applies automatically the next time that student's September invoice is
generated (or immediately if billed in advance per §2) — exactly like a single adjustment,
just created 47 times in one call instead of 47 API round-trips. Skipped students (whose
September invoice is already past Draft) show up with a reason instead of failing the batch;
re-run for just that student via the single-adjustment endpoint once their invoice is cancelled
or regenerated.

## 4. Fee Payments folded into a Fee Generation tab

The **API is unchanged** — `/api/feepayments/*` still exists exactly as before (preview, create,
list, detail, void, receipt). What changed is navigation: `FEE_PAYMENT_LIST` is no longer a
visible sidebar sub-menu under Fee Management. The Fee Generation page should now render a
fourth tab alongside Invoices / Monthly Adjustments / Statement of Account:

```
Fee Generation
├── Invoices              (existing)
├── Monthly Adjustments   (existing)
├── Statement of Account  (round 1)
└── Fee Payments          (new tab -- was its own page)
```

The Fee Payments tab is the same list/collect-payment UI that used to live at
`/apps/fee-payment/list` — just embedded as a tab instead of a separate route. All its
permission codes (`FEE_PAYMENT_LIST`, `FEE_PAYMENT_PREVIEW`, `FEE_PAYMENT_CREATE`,
`FEE_PAYMENT_DETAIL`, `FEE_PAYMENT_VOID`, `FEE_PAYMENT_RECEIPT`) kept their exact codes and ids
in `MenuSeeder` — only re-parented from the retired `FEE_PAYMENT_LIST` sub-menu onto
`FEE_INVOICE_LIST` as hidden permission rows, same pattern as the 2026-07-16
`ACADEMIC_MANAGEMENT`/`TEACHER_MANAGEMENT` retirement. **Existing role grants on these
permissions survive untouched** — nobody needs their roles re-granted.

The new bulk-adjustment permission (`FEE_ADJUSTMENT_BULK_CREATE`, §3) is seeded under the same
`FEE_INVOICE_LIST` parent.

## 5. Payment receipt: new template design + school header

The seeded default `PaymentReceipt` template (`GET /api/feepayments/{id}/receipt`) was redrawn
to match the school's own paper receipt format: a bordered card, school-name header with
address/phone, `Receipt No.`, student/grade line, a Sr.No-led Particulars table (fed by every
allocated invoice's own lines), a Total row, Paid-By/Balance line, and signature blocks.

**Three new placeholders**, sourced from `AppConfig`:

| Token | Source |
|---|---|
| `{{SchoolName}}` | `APP_NAME` (already exists — the app-wide branding name doubles as the school name on documents) |
| `{{SchoolAddress}}` | new `AppConfig` param `SCHOOL_ADDRESS` (seeded blank — set via `PUT /api/appconfigs/{id}`) |
| `{{SchoolPhone}}` | new `AppConfig` param `SCHOOL_PHONE` (seeded blank, same) |

`{{InvoiceLinesRows}}` now renders 4 columns instead of 3: **Sr.No, Invoice No, Particulars,
Amount** — the Sr.No column matches the sample's layout; Invoice No stays because (unlike the
paper sample) one CMSApp receipt can legitimately span several months' invoices, especially
after an advance payment (§2).

**Deliberately not copied from the sample:** the "All above mentioned Amount once paid are non
refundable in any case whatsoever" disclaimer. This system explicitly tracks genuinely
refundable items (`FeeStructureItem.IsRefundable` — e.g. security deposits), so that blanket
claim would be factually wrong here. The footer instead reads "This is a computer-generated
receipt." Adjust the template's wording via `PUT /api/documenttemplates/{id}` if your school's
policy differs per fee category.

**Heads up if you already have a `PaymentReceipt` template row from round 1's seeding**:
`DocumentTemplateSeeder` is strictly create-if-missing (an admin's hand-edited template must
survive restarts) — it will **not** overwrite an existing row with this new design. Delete the
existing row (there's no delete endpoint for templates yet — do it once via SQL, or simply
`PUT /api/documenttemplates/{id}` with the new HTML shown in `DocumentTemplateSeeder.
BuildPaymentReceiptHtml()`) to pick up the redesign; a genuinely fresh database gets it
automatically.

## 6. What "Monthly Adjustments" is for

The "New Monthly Adjustment" screen (`POST /api/feeinvoices/adjustments`, `FeeAdjustment`
entity) is the **per-student, one-month override** tool — the single-student sibling of the
bulk endpoint in §3. Typical uses:

- **A one-off discount or waiver for one student in one month** — "Special Discount 50% for
  Nabin Adhikari, July 2026" (the modal in the screenshot): a scholarship board approves a
  one-time 50% break for a specific family's hardship, not a standing `StudentDiscount`.
- **A one-off surcharge** — a late-registration fine, a broken-equipment charge, applied to one
  student for one month via `direction: 1 Increase`.
- **Correcting a single student's upcoming bill** without touching the fee structure (which
  would affect the whole class) or a standing discount/scholarship (which would apply every
  month going forward).

It differs from **`StudentDiscount`/`StudentScholarship`** (standing, apply every month until
removed) and from **editing a Draft invoice's lines directly** (only possible after generation,
and only for that one already-created invoice) by being a **pre-generation, one-month-only**
instruction: create it any time before that month's invoice is generated (or before a Draft is
finalized — regenerate the Draft to pick it up), and it folds into that one invoice as a line,
then reverts to reusable `Pending` if the invoice is later cancelled or regenerated. Once
`Applied`, it's locked to that invoice; `Pending` adjustments can still be edited or cancelled.
§3's bulk endpoint is the same mechanism, just stamped onto every enrollment in a scope in one
call instead of one at a time.

## 7. Permissions

New `MenuSeeder` permission row (granted to SuperAdmin automatically; grant to other roles via
`POST /api/roles/claims`):

| Code | Endpoint |
|---|---|
| `FEE_ADJUSTMENT_BULK_CREATE` | `POST /api/feeinvoices/adjustments/bulk` |

No other new permissions this round — the Fee Payments permissions (§4) kept their existing
codes/ids, so existing grants apply unchanged.
