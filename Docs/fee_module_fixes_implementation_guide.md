# Fee Module Fixes — UI Implementation Guide (2026-07-17)

Fixes and additions from the reported fee-module issues (`Docs/Issues in fee module.pdf`).
Complements (never replaces) `Docs/setup_fee_payroll_redesign_implementation_guide.md` and the
fee sections of `UI-Implementation-Guide.md` — both updated alongside this guide.

All responses use the standard envelope `{ responseCode, responseMessage, data }`.

## What changed, at a glance

| Reported issue | Fix |
|---|---|
| "Generate for Nursery, All Sections" generated LKG invoices instead | `POST /api/feeinvoices/generate` now accepts `academicClassId` — a real "this grade, all sections" scope (§1) |
| No student search in the fee module | New `search` filter on the invoice + payment lists, and a dedicated student search endpoint (§2) |
| Statement of Account view needed, with pending amounts | New ledger-style `GET /api/feeinvoices/account-statement/{enrollmentId}` (§3) |
| "Annual installment 1/5" auto-split shouldn't be automatic | `installmentCount` is now configured per fee-structure item; default = charge in full on the first invoice (§4) |
| No print-receipt function | New `GET /api/feepayments/{id}/receipt` renders a printable HTML receipt including invoice line details (§5) |
| Onboarding checkboxes (transportation, hostel, …) to control invoice inclusion | `POST /api/enrollments` now takes `optionalFeeStructureItemIds` (§6) |

## 1. Fee generation is now class-scoped

**Root cause of the Nursery/LKG bug:** the generate API previously accepted only
`classSectionId` — there was no way to say "one class, all sections", so a UI class picker
without a section fell back to generating for the **entire academic year** (every grade). The
"skipped: class has no fee structure" rows were students of *other* grades swept into the run.

`POST /api/feeinvoices/generate` — request body:

```json
{
  "academicYearId": "GUID",
  "billingYear": 2026,
  "billingMonth": 7,
  "academicClassId": "GUID | omit for the whole year",
  "classSectionId": "GUID | omit for all sections of the class",
  "regenerateDrafts": false
}
```

- `academicClassId` only → every Enrolled enrollment of that grade, all sections.
- `academicClassId` + `classSectionId` → one section (the section must belong to that class,
  else `VALIDATION_ERROR`).
- Neither → the whole academic year (previous behavior, now the *explicit* choice).
- The class must belong to `academicYearId` (`VALIDATION_ERROR` otherwise; unknown id →
  `NOT_FOUND`).

**UI rule:** when the class dropdown has a value, always send it as `academicClassId`. "All
Sections" means *omit `classSectionId` while keeping `academicClassId`* — never omit both
unless the user explicitly picked "All Classes".

## 2. Student search

### 2.1 Search box on the invoice and payment lists

Both lists take a new optional `search` query parameter (case-insensitive substring):

- `GET /api/feeinvoices?...&search=milan` — matches student first/last name, admission no,
  email, **or invoice no**.
- `GET /api/feepayments?...&search=ADM2026` — matches student first/last name, admission no,
  email, **or receipt no**.

All existing filters still apply and combine with `search`.

### 2.2 Dedicated student lookup (Fee Generation / Statement of Account pages)

`GET /api/feeinvoices/students?search=&academicYearId=&page=1&pageSize=10`

`search` matches name / admission no / email over **active (Enrolled) enrollments**;
`academicYearId` narrows to one year (recommended — pass the year already selected in the page
header). Returns `PaginatedResponse<FeeStudentSearchResultDto>`:

```json
{
  "items": [
    {
      "enrollmentId": "GUID",
      "studentId": "GUID",
      "studentName": "Milan Sharma",
      "admissionNo": "ADM2026105",
      "email": "milan@example.com",
      "phone": "+9779800000000",
      "academicYearId": "GUID",
      "gradeCode": "NURSERY",
      "sectionCode": "A",
      "outstandingAmount": 32750.00
    }
  ],
  "page": 1, "pageSize": 10, "totalCount": 3
}
```

`outstandingAmount` is the live balance (Σ net − paid over the enrollment's non-Draft,
non-Cancelled invoices). **Typical flow:** type-ahead search → pick a student → use their
`enrollmentId` to open the Statement of Account (§3) or pre-fill the payment form
(`POST /api/feepayments`).

## 3. Statement of Account (ledger view)

`GET /api/feeinvoices/account-statement/{enrollmentId}`

The accounting-style statement from the mockup: opening balance, chronological entries with
Credit/Debit columns and a running balance, closing balance. Invoices post **debits** (the
invoice's full pre-discount `grossAmount`, not `netAmount`), payments post **credits** (what was
received). Draft/Cancelled invoices and Voided payments never appear.

**2026-07-20 fix**: the statement previously posted only `netAmount` as the invoice's debit,
which silently netted any discount/scholarship into the invoice line with no visible record that
a reduction had been applied. Every negative line on the invoice (`Discount`, `Scholarship`,
`RuleDiscount` payment-time-rule discount, or a negative `MonthlyAdjustment`) now posts its own
credit entry alongside the invoice, using that line's own description (e.g. `"Discount - Sibling
Discount"`) — so `grossAmount` (debit) minus those reduction credits nets to the same
`netAmount` as before, but the reduction is now a visible ledger line instead of a silent netting.
`entryType` for these rows is one of `"Discount"` / `"Scholarship"` / `"Rule Discount"` /
`"Adjustment"` (any `entryType` other than `"Invoice"`/`"Payment"` should render as a generic
credit row — don't hardcode a closed set of badge colors against just those two).

```json
{
  "enrollmentId": "GUID",
  "studentId": "GUID",
  "studentName": "Levi Wall",
  "admissionNo": "ADM2026101",
  "email": "levi@example.com",
  "gradeCode": "LKG",
  "sectionCode": "A",
  "openingBalance": 0.00,
  "totalDebit": 40250.00,
  "totalCredit": 11000.00,
  "closingBalance": 29250.00,
  "entries": [
    {
      "date": "2026-07-16T00:00:00Z",
      "entryType": "Invoice",
      "reference": "INV20260001",
      "description": "Fee invoice for July 2026",
      "debit": 40250.00, "credit": 0.00, "balance": 40250.00
    },
    {
      "date": "2026-07-16T00:00:00Z",
      "entryType": "Discount",
      "reference": "INV20260001",
      "description": "Discount - Sibling Discount",
      "debit": 0.00, "credit": 1000.00, "balance": 39250.00
    },
    {
      "date": "2026-07-17T00:00:00Z",
      "entryType": "Payment",
      "reference": "RCP20260001",
      "description": "Payment received (Cash)",
      "debit": 0.00, "credit": 10000.00, "balance": 29250.00
    }
  ]
}
```

- `closingBalance` **is** the pending amount — when it's `> 0`, show an "Add Payment" /
  "Settle" button that jumps to fee-payment entry with this `enrollmentId`.
- Invoice entries are dated by their finalization time (`generatedTs`), falling back to the
  due date; same-day entries list invoices before payments so the running balance never dips
  spuriously.
- The older `GET /api/feeinvoices/statement/{enrollmentId}` (invoice-per-row dues view) is
  unchanged — use whichever fits the screen; both agree on the outstanding total.
- PDF/Excel export is client-side (same convention as payslips: `@react-pdf/renderer` / CSV).

## 4. Annual installments are admin-configured, not automatic

Previously an Annual fee item was force-split into equal installments over the months from the
enrollment's first invoice to the year end — producing the surprise "ANNUAL (Annual,
installment 1/5)" lines. Now:

- `FeeStructureItem` has a new nullable `installmentCount` (1–12, **Annual items only** —
  sending it for Monthly/OneTime is a `VALIDATION_ERROR`).
- **Omitted / null / 1** → the full annual amount is charged once, on the enrollment's first
  invoice (line description `"ANNUAL (Annual)"`).
- **N ≥ 2** → N equal monthly installments starting from the enrollment's first invoice
  (`"ANNUAL (Annual, installment i/N)"`; the last installment absorbs rounding).

The field rides the existing shapes everywhere items appear:

- `POST /api/feestructures` — each entry of `items[]` may carry `"installmentCount": 5`.
- `POST /api/feestructures/{id}/items` — same field on the single-item body.
- `PUT /api/feestructures/{id}/items/{itemId}` — editable in place (affects **future
  generation only**; already-generated invoices are immutable snapshots).
- `FeeStructureItemDto` now returns `installmentCount` on every fee-structure response.

**UI:** on the fee-structure item form, show an "Installments" number input (1–12) only while
Frequency = Annual; default it to 1 ("charge in full on first invoice").

> Existing rows after migration: `installment_count` is `NULL` everywhere, i.e. every Annual
> item switches to charge-in-full — re-enter the split explicitly where installments are
> actually wanted.

## 5. Print receipt — `GET /api/feepayments/{id}/receipt`

Renders the new admin-configurable **PaymentReceipt** document template (type `5`) into final
HTML — same contract as the other document previews (`{ templateType, html }`); the frontend
opens it in a print dialog / new tab exactly like payslip previews.

- `404 NOT_FOUND` — unknown payment id, or no PaymentReceipt template configured yet (the
  seeder ships a default, so this only happens if the row was deleted).
- `409 CONFLICT` — the payment is voided.

Template management rides the existing document-template endpoints
(`GET/PUT /api/documenttemplates/...`, placeholders at
`GET /api/documenttemplates/placeholders/5`). Placeholders:

| Token | Meaning |
|---|---|
| `{{ReceiptNo}}`, `{{PaymentDate}}` | receipt header |
| `{{StudentName}}`, `{{AdmissionNo}}`, `{{GradeCode}}`, `{{SectionCode}}` | student block |
| `{{PaymentMode}}`, `{{ReferenceNo}}`, `{{Remarks}}` | payment details (`ReferenceNo` renders as `(Ref …)` or empty) |
| `{{AmountPaid}}` | total received on this receipt |
| `{{AllocationsRows}}` | `<tr>` rows: invoice no, billing month, allocated amount |
| `{{InvoiceLinesRows}}` | `<tr>` rows: **every line of every allocated invoice** (invoice no, description, amount) — the "receipt includes invoice line details" requirement |
| `{{OutstandingAmount}}` | the enrollment's live remaining balance after this payment |

## 6. Optional fees at student onboarding (transportation, hostel, …)

The invoice-inclusion switch was already data-driven — an `isOptional` fee-structure item is
billed **only** for enrollments that opted in (`EnrollmentFeeSelection`). What was missing was
a way to opt in during onboarding. Now:

`POST /api/enrollments` accepts:

```json
{
  "studentId": "GUID",
  "classSectionId": "GUID",
  "rollNumber": "12",
  "enrollmentDate": "2026-07-17",
  "optionalFeeStructureItemIds": ["GUID", "GUID"]
}
```

- Each id must be an `isOptional = true` item of the **section's class** fee structure —
  unknown/mandatory items fail with `VALIDATION_ERROR` and *nothing* is saved (all-or-nothing).
- Selections remain editable later via the existing
  `POST/DELETE/GET /api/enrollments/{id}/fee-selections...` endpoints.

**UI (onboarding form):** after the class/section picker resolves, fetch the class's fee
structure (`GET /api/feestructures?academicClassId={classId}`) and render one checkbox per
`isOptional` item, labeled from its `feeCategoryCode` + amount — e.g. `☐ Transportation Fee
(Rs 250/month)`, `☐ Hostel Fee (Rs 4,000/month)`. Checked items go into
`optionalFeeStructureItemIds`. This is deliberately **not** a pair of hardcoded
`isTransportationUsed`/`isHostelEnrolled` booleans — any optional category the school adds to
its fee structure (van route, day-boarding, …) gets its checkbox with zero backend changes.

## 7. Failure codes

| Code | When |
|---|---|
| `VALIDATION_ERROR` | class not in the year / section not in the class; `installmentCount` outside 1–12 or on a non-Annual item; optional-fee id unknown or mandatory |
| `NOT_FOUND` | unknown year/class/section/enrollment/payment id; PaymentReceipt template missing |
| `CONFLICT` | receipt requested for a voided payment (plus all pre-existing fee-module conflicts) |

## 8. Permissions & migration notes

New `MenuSeeder` permission rows (granted to SuperAdmin automatically at startup; grant to
other roles via `POST /api/roles/claims`):

| Code | Endpoint |
|---|---|
| `FEE_ACCOUNT_STATEMENT` | `GET /api/feeinvoices/account-statement/{enrollmentId}` |
| `FEE_STUDENT_SEARCH` | `GET /api/feeinvoices/students` |
| `FEE_PAYMENT_RECEIPT` | `GET /api/feepayments/{id}/receipt` |

**Migration (user-owned, as always):** one new nullable column —
`dbo.fee_structure_items.installment_count integer NULL`. No other schema changes; the
PaymentReceipt template row is seeded automatically on next startup.
