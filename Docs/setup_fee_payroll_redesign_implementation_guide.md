# Setup Module, Fee Generation & Payroll Runs — UI Implementation Guide

Backend delivered 2026-07-16 from the blueprint in
`Docs/setup_fee_payroll_redesign_implementation_plan.md` (read that for the full architecture,
business rules F1–F11/P1–P8/S1–S4, and workflows). This page is the immediately-implementable
UI contract. Every response uses the standard envelope
(`{ responseCode, responseMessage, data }`); every endpoint below needs a JWT and its seeded
permission row unless noted.

> **Migration required first**: 10 new tables (`fee_rules`, `fee_invoices`,
> `fee_invoice_lines`, `fee_payments`, `fee_payment_allocations`, `fee_adjustments`,
> `payroll_runs`, `salary_slips`, `salary_slip_lines`, `salary_adjustments`) — user-owned, not
> created by the backend. Every endpoint below 500s until it exists. No existing table
> changes.
>
> **2026-07-18 addendum**: one more new table, `fee_generation_runs` (see §10), and two new
> columns, `fee_invoices.carried_forward_amount numeric NOT NULL DEFAULT 0` and
> `fee_invoices.carried_forward_to_invoice_id uuid NULL` — see
> `Docs/fee_generation_run_and_carry_forward_implementation_guide.md`.

---

## 1. Navigation changes (Setup module)

On the next backend boot, `MenuSeeder` re-shapes the menu tree (`GET /api/roles/user-menus`
reflects it automatically — menu ids and role grants are preserved):

| Menu | Change |
|---|---|
| **SETUP** (new main, "Setup", order 5) | now parents `YEAR_LIST`, `CLASS_LIST`, `FEE_STRUCTURE_LIST`, `FISCAL_YEAR_LIST`, and the new `FEE_RULE_LIST` (`/apps/fee-rule/list`) |
| **ACADEMIC_MANAGEMENT** | retired (soft-deleted) — both submenus moved to Setup |
| **TEACHER_MANAGEMENT** | retired — every `TEACHER_*` permission row (and `TEACHER_LIST` itself, now hidden) lives under `EMPLOYEE_LIST`; `/api/teachers/*` endpoints unchanged. Teacher UI folds into the Employee profile (teacher tab when `employeeCategoryCode == "ACADEMIC"` and a teacher profile exists; **Compensation Plan tab always renders last**) |
| **FEE_MANAGEMENT** | now transactional only: `FEE_INVOICE_LIST` ("Fee Generation", `/apps/fee-invoice/list`) + `FEE_PAYMENT_LIST` ("Fee Payments", `/apps/fee-payment/list`) |
| **PAYROLL_MANAGEMENT** | now transactional only: `PAYROLL_RUN_LIST` ("Salary Generation", `/apps/payroll-run/list`) |

Route paths of moved pages are unchanged; only grouping/breadcrumbs change.

## 2. Fee category `fee_frequency` (Config catalog 1010)

`AdditionalValue1` on a FeeCategory option is now **normative**: one of `MONTHLY`, `ANNUAL`,
`ONE_TIME`. Create/update of a 1010 option without a valid value fails with
`VALIDATION_ERROR`. Existing rows are auto-normalized on boot (legacy `Monthly`/`Annual`/
`OneTime` → the codes). The category value pre-fills a new fee-structure item; the item's own
`frequencyType` (1 Monthly / 2 Annual / 3 OneTime) is what generation executes.

New Config catalogs: **1016 SalaryAdjustmentType** (`UNPAID_LEAVE`, `LATE_FINE`, `OVERTIME`,
`BONUS`, `INCENTIVE`, `ARREAR`, `OTHER`; `AdditionalValue1` = suggested direction) and
**1017 FeeAdjustmentType** (`SPECIAL_DISCOUNT`, `ADDITIONAL_CHARGE`, `FINE`,
`CARRY_CORRECTION`, `OTHER`). New AppConfig: `FEE_DUE_DAY_OF_MONTH` (default `10`).

## 3. Fee Rules — `/api/feerules` (Setup)

Configurable payment-time discounts. `ruleType`: `1 AdvanceMonthsDiscount` ("pay X months
together"), `2 EarlyPaymentDiscount` ("pay N days before due date"). `valueType`:
`1 Percentage` / `2 FixedAmount`.

| Endpoint | Notes |
|---|---|
| `GET /api/feerules?page=&pageSize=&ruleType=&academicClassId=&isActive=` | paged list |
| `POST /api/feerules` | body below; `code` unique/immutable; `minMonthsTogether` (≥2) required for type 1; `daysBeforeDueDate` (≥0) required for type 2 |
| `GET /api/feerules/{id}` / `PUT /api/feerules/{id}` / `DELETE /api/feerules/{id}` | update keeps `code`/`ruleType` immutable; delete is soft |

```json
POST /api/feerules
{
  "code": "PAY_3_MONTHS_5PCT",
  "name": "Pay 3 months together",
  "ruleType": 1,
  "valueType": 1,
  "value": 5,
  "minMonthsTogether": 3,
  "academicClassId": null,
  "feeCategoryCode": null,
  "effectiveFrom": "2026-01-01",
  "effectiveTo": null,
  "priority": 1,
  "isCombinable": false,
  "isActive": true
}
```

`academicClassId`/`feeCategoryCode` null = applies to all classes / the whole recurring
subtotal. Priority orders evaluation; the first matching rule always applies, then only
`isCombinable` rules stack.

## 4. Fee Generation — `/api/feeinvoices` (Fee Management)

Statuses: `1 Draft` (editable) → `2 Generated` (issued, locked) → `3 Pending` (overdue,
stamped opportunistically) → `4 PartiallyPaid` → `5 Paid`; `6 Cancelled`. Line `source`:
`1 StructureItem, 2 AnnualInstallment, 3 OneTimeCharge, 4 Discount, 5 Scholarship,
6 RuleDiscount, 7 MonthlyAdjustment, 8 Manual`.

| Endpoint | Notes |
|---|---|
| `POST /api/feeinvoices/generate` | `{ academicYearId, billingYear, billingMonth, academicClassId?, classSectionId?, regenerateDrafts }` → `FeeGenerationResultDto` (`feeGenerationRunId`, `generatedCount`, `generatedInvoiceIds`, `skippedCount`, `skippedSummary[{reason, count}]` — 2026-07-21, replaces the old per-enrollment `skipped[{enrollmentId, studentName, reason}]`, which ballooned to hundreds of rows on a regenerate call; reasons are fixed short strings: "Invoice Already Generated", "Draft Invoice Already Exists", "No Fee Structure Configured", "Fee Structure Not Active"). Idempotent; `academicClassId` scopes to one grade/all sections (2026-07-17 — always send it when the class picker has a value); annual items charge in full on the first invoice unless the item configures `installmentCount` (see `fee_module_fixes_implementation_guide.md` §4; last installment absorbs rounding); OneTime items only on an enrollment's first invoice; discounts/scholarships/pending adjustments folded in as negative/signed lines. **2026-07-18**: also auto-carries any strictly-earlier-period outstanding balance forward as a Pending `CARRY_CORRECTION` adjustment, **voiding** every contributing older invoice (`status` → `6 Cancelled`, `carriedForwardAmount`/`carriedForwardToInvoiceId` stamped) so the balance is billed exactly once, on the new invoice (see `fee_generation_run_and_carry_forward_implementation_guide.md` §2) — and always finds-or-creates that period's `FeeGenerationRun` master row, whichever class(es) were in scope |
| `GET /api/feeinvoices?...` | paged; filters: `academicYearId, billingYear, billingMonth, academicClassId, classSectionId, enrollmentId, status, search` (student name/admission no/email/invoice no). **`status` now accepts multiple values** (2026-07-18) — repeat the query key (`status=4&status=6`) or comma-separate (`status=4,6`); a single `status=4` still works exactly as before. `balanceAmount` = net − paid; `carriedForwardAmount`/`carriedForwardToInvoiceId` (2026-07-18) are set once this invoice has been voided and its balance carried onto a later invoice (`status` will read `6 Cancelled`) |
| `GET /api/feeinvoices/students?search=&academicYearId=&page=&pageSize=` | student search for the fee module (name/admission no/email over Enrolled enrollments) → `enrollmentId` + live `outstandingAmount` per match (2026-07-17). **2026-07-18**: default page size 20; with no `search` text, zero-balance students are hidden and results sort by `outstandingAmount` descending (the "who owes money" worklist); an active search still returns matches regardless of balance, sorted alphabetically |
| `GET /api/feeinvoices/account-statement/{enrollmentId}` | ledger-style Statement of Account: invoice debits, payment credits, running balance, `closingBalance` = live outstanding (2026-07-17) |
| `GET /api/feeinvoices/{id}` | detail with `lines[]` |
| `PUT /api/feeinvoices/{id}` | Draft only: `{ dueDate, remarks }` |
| `POST /api/feeinvoices/{id}/lines` | Draft only, manual line `{ feeCategoryCode?, description, amount }` — amount signed (negative = credit) |
| `PUT/DELETE /api/feeinvoices/{id}/lines/{lineId}` | Draft only; removing a MonthlyAdjustment line re-pends its source adjustment |
| `POST /api/feeinvoices/{id}/lines/{lineId}/settle-annual-in-full` | Draft only, line must be `AnnualInstallment`-sourced (2026-07-17). Bills the item's true remaining annual balance in one shot; `409` if nothing remains. The installment engine is remaining-balance-driven (reads earlier invoices' actual line amounts, not a schedule index), so later months automatically stop billing an item once settled — no separate flag |
| `POST /api/feeinvoices/finalize` | `{ invoiceIds: [...] }` → `{ finalizedCount, skippedInvoiceIds }` (non-Drafts skipped) |
| `POST /api/feeinvoices/{id}/unfinalize` (2026-07-21) | reverses a Finalize — Generated/Pending back to Draft (`generatedTs` cleared), re-pends applied adjustments. Refused (`CONFLICT`) once payments exist (PartiallyPaid/Paid) — void those first, same guard `cancel` uses — or if already Draft/Cancelled. The point: an invoice locked before a later adjustment was created has no other way back to editable |
| `POST /api/feeinvoices/{id}/cancel` | allowed Draft/Generated/Pending; refused (`CONFLICT`) once payments exist — void those first. Re-pends applied adjustments |
| `GET /api/feeinvoices/statement/{enrollmentId}` | the dues view: all non-cancelled invoices oldest-first + live `outstandingAmount`. This is the "January + February both show" answer |

**Fee adjustments** (pre-generation monthly overrides, statuses `1 Pending / 2 Applied /
3 Cancelled`):

- `GET /api/feeinvoices/adjustments?enrollmentId=&billingYear=&billingMonth=&status=`
- `POST /api/feeinvoices/adjustments` — `{ enrollmentId, billingYear, billingMonth,
  adjustmentTypeCode (catalog 1017), feeCategoryCode? (catalog 1010, 2026-07-17 — scopes a
  Percentage value to just that category's recurring subtotal), direction (1 Increase/charge,
  2 Decrease/credit), valueType, value, remarks }`. Refused with `CONFLICT` if that month's
  invoice is already past Draft. **`CARRY_CORRECTION` is also created automatically** by
  `generate` (2026-07-18, see §10) when an enrollment has an outstanding balance from a
  strictly earlier period — it behaves exactly like any other Pending adjustment (visible/
  editable/cancellable here) once created, and generation keeps its `value` in sync (or
  cancels it) on every regenerate if the underlying balance changes.
- `PUT /api/feeinvoices/adjustments/{id}` / `DELETE .../adjustments/{id}` — Pending only
  (DELETE = cancel, keeps the audit row).
- `POST /api/feeinvoices/adjustments/bulk` (2026-07-17) — same fields as the single-adjustment
  create (including `feeCategoryCode`), minus `enrollmentId`, plus the generation-style scope
  (`academicYearId, academicClassId?, classSectionId?`). Stamps one Pending adjustment onto
  every Enrolled enrollment in scope → `BulkFeeAdjustmentResultDto` (`createdCount`,
  `createdAdjustmentIds`, `skipped[{enrollmentId, studentName, reason}]`) — the
  Education-Tour/Examination-Fee/Training bulk-charge case.

## 5. Fee Payments — `/api/feepayments` (Fee Management)

**As of 2026-07-17 this is a tab on the Fee Generation page, not its own sidebar menu** — the
API below is unchanged, only `MenuSeeder`'s `FEE_PAYMENT_LIST` navigation entry was retired
(re-parented as a hidden permission under `FEE_INVOICE_LIST`, codes/ids/grants preserved).

Append-only money records; FIFO allocation across open invoices (oldest month first).
Statuses: `1 Confirmed`, `2 Voided`.

| Endpoint | Notes |
|---|---|
| `GET /api/feepayments/advance-quote?enrollmentId=&monthsToPay=&paymentDate=&applyRuleDiscounts=` | read-only, never writes (2026-07-17) — "how much for X months?" `FeeAdvanceQuoteDto` (`monthsRequested, monthsAvailable, grossAmount, ruleDiscountTotal, netAmountToCollect, months[{billingYear, billingMonth, netAmount, isAlreadyGenerated}], ruleDiscounts[]`). Answers the Collect Payment form's "how do I know the amount" gap — auto-fill Amount from `netAmountToCollect`, then proceed to preview/confirm as normal |
| `POST /api/feepayments/preview` | same body as create, **no writes**. Returns the allocation plan + earned rule discounts + `unallocatedAmount`. **UI flow: always preview first** — if `unallocatedAmount > 0`, tell the cashier to collect `amount - unallocatedAmount` (rule discounts reduced the total) |
| `POST /api/feepayments` | `{ enrollmentId, paymentDate, amount, paymentMode (1 BankDeposit/2 Cash/3 Cheque), referenceNo?, remarks?, applyRuleDiscounts (default true), allowAdvanceBilling (default true, 2026-07-17) }` → `FeePaymentDto` with `receiptNo` (`RCP{year}{seq}`), `allocations[]` (each with `isNewlyGenerated`), and `monthsBilledInAdvance`. **Advance payment (2026-07-17)**: if `amount` exceeds the currently-open total, the service bills the next consecutive months ahead (same line composition as generation, up to 12 months) so a `AdvanceMonthsDiscount` Fee Rule has real fully-settled invoices to fire against — set `allowAdvanceBilling: false` for the old strict behavior. Over-payment beyond what advance billing can cover (or with it disabled) is rejected |
| `GET /api/feepayments?...` | paged; filters: `enrollmentId, fromDate, toDate, paymentMode, status, search` (student name/admission no/email/receipt no) |
| `GET /api/feepayments/{id}` | detail with allocations |
| `GET /api/feepayments/{id}/receipt` | printable HTML receipt from the PaymentReceipt document template (type 5), redesigned 2026-07-17 around the school's own paper-receipt layout (school-header band from `AppConfig` `APP_NAME`/`SCHOOL_ADDRESS`/`SCHOOL_PHONE`, Sr.No-led particulars table), including every allocated invoice's line details. `409` for voided payments |
| `POST /api/feepayments/{id}/void` | reverses allocations, re-derives invoice statuses. Rule-discount lines earned at confirm are deliberately not reverted |

## 6. Salary Adjustments — `/api/employees/{id}/adjustments`

Pre-run monthly payroll overrides (catalog 1016). `direction`: `1 Increase` (earning) /
`2 Decrease` (deduction). For `UNPAID_LEAVE`: send `quantity` = day count, `direction: 2`;
`value` is ignored (the deduction is computed from that month's earnings at generation).
For everything else `value` is the amount, or a percentage of BASIC when
`valueType: 1`; `quantity` is an optional multiplier (e.g. late-arrival count).

- `GET /api/employees/{id}/adjustments?fiscalYearId=&monthIndex=&status=`
- `POST /api/employees/{id}/adjustments` — `{ fiscalYearId, monthIndex (1-12,
  Shrawan..Ashad), adjustmentTypeCode, direction, valueType, value, quantity?, remarks }`.
  Refused with `CONFLICT` if that month's run is already Approved/Paid.
- `PUT` / `DELETE /api/employees/{id}/adjustments/{adjustmentId}` — Pending only.

## 7. Payroll Runs — `/api/payrollruns` (Payroll Management)

Run statuses: `1 Draft` → `2 Approved` → `3 Paid`; `4 Cancelled`. Slip statuses mirror the
run and are individually cancellable while unpaid. Slip line `lineType`: `1 Earning,
2 Deduction, 3 Tax, 4 LoanEmi`; `source`: `1 SalaryStructure, 2 TaxCalculator,
3 LoanSchedule, 4 MonthlyAdjustment, 5 Manual`.

| Endpoint | Notes |
|---|---|
| `POST /api/payrollruns` | `{ fiscalYearId, monthIndex, remarks? }` → `PayrollGenerationResultDto` (`run` with slip summaries + `skipped[{employeeId, employeeName, reason}]`). One live run per fiscal month (`CONFLICT` otherwise). Snapshots: effective salary revision + monthly breakdown + TDS + due loan EMIs + Pending adjustments |
| `GET /api/payrollruns?page=&pageSize=&fiscalYearId=&status=` | paged; run rows carry `slipCount`, `totalGrossEarnings`, `totalNetPay` |
| `GET /api/payrollruns/{id}` | run + slip summaries |
| `POST /api/payrollruns/{id}/approve` | Draft only; locks everything, stamps `approvedBy` |
| `POST /api/payrollruns/{id}/mark-paid` | Approved only |
| `POST /api/payrollruns/{id}/cancel` | Draft or Approved-unpaid; re-pends consumed adjustments |
| `GET /api/payrollruns/{id}/slips/{slipId}` | slip detail with `lines[]`, `payDays`, `unpaidLeaveDays`, totals |
| `POST /api/payrollruns/{id}/slips/{slipId}/cancel` | one employee's slip (leaver) while run unpaid |
| `POST/PUT/DELETE .../slips/{slipId}/lines[/{lineId}]` | Draft slips only; manual lines are Earning/Deduction with `amount > 0` |

## 8. Payslip endpoints now prefer persisted slips

`GET /api/employees/{id}/payslips` (+ teacher aliases) and
`GET .../payslips/{fiscalYearId}/{monthIndex}` gained **`isProjection`**: `false` when the
month has a persisted salary slip from a run (real `payDays`/`upl`, adjustments and manual
edits included), `true` for the old read-time projection (unchanged behavior). Badge
projections accordingly ("estimated"). Existing fields are unchanged — this is additive.

## 10. Fee Generation Runs (master table) — `/api/feegenerationruns`

**2026-07-18.** The fee-side counterpart of Payroll Runs (§7) — a period-keyed master record
grouping a billing month's invoices by class → student, so an admin can see "everything that
happened for July 2026" in one place instead of only the flat invoice list. Full detail in
`Docs/fee_generation_run_and_carry_forward_implementation_guide.md`.

Unlike a `PayrollRun`, this is **not** a create-once workflow header — one live row per
`(academicYearId, billingYear, billingMonth)` is found-or-created automatically by
`POST /api/feeinvoices/generate` (§4), so it always reflects every scoped generate call for
that period (one class at a time, or "all classes" when no class is picked — see Image #2's
"No class selected — this run covers EVERY class" banner) plus any invoice created by advance
payment (§5) that happened to land in the same period.

| Endpoint | Notes |
|---|---|
| `GET /api/feegenerationruns?page=&pageSize=&academicYearId=&billingYear=&billingMonth=` | paged list, newest period first. Each row: `generatedTs`, `lastRegeneratedTs`, `invoiceCount`, `classCount`, `studentCount`, `totalNetAmount`, `totalPaidAmount`, `totalOutstandingAmount` (live, excludes carried-forward invoices) |
| `GET /api/feegenerationruns/{id}` | detail: the same header fields plus `classes[{ academicClassId, gradeCode, invoiceCount, studentCount, totalNetAmount, totalPaidAmount, totalOutstandingAmount, students[{ enrollmentId, studentId, studentName, admissionNo, sectionCode, totalNetAmount, totalPaidAmount, totalOutstandingAmount, invoices[FeeInvoiceDto] }] }]`. `gradeCode` is the raw Config code (e.g. `NURSERY`, `LKG`) — resolve the display label from the Grade dropdown, same convention as everywhere else `gradeCode` appears |

No create/cancel endpoint here — the row's lifecycle is entirely driven by `generate`.

## 11. Failure codes (all endpoints)

| Code | Typical causes here |
|---|---|
| `VALIDATION_ERROR` | bad enum/month/amount; unknown catalog code; over-payment; fee_frequency missing on a 1010 option; editing with wrong per-type rule params |
| `NOT_FOUND` | unknown year/section/enrollment/employee/invoice/run/slip/line/adjustment |
| `CONFLICT` | duplicate rule code; month already generated/run exists; editing non-Draft documents; cancelling paid documents; adjustment for a locked month; voiding a voided payment |
