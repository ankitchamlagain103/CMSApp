# Setup Module, Fee Management & Payroll Redesign — Implementation Blueprint

> **Status: PLANNING DOCUMENT — no code, UI, or migrations have been produced from this yet.**
> This is the complete implementation blueprint for the development team. At implementation
> time, each delivered feature additionally gets its own short
> `Docs/<feature>_implementation_guide.md` for the UI team, per repo convention, and the
> corresponding `UI-Implementation-Guide.md` sections must be updated.

Date: 2026-07-16
Scope: four workstreams — (1) a new **Setup** navigation module, (2) a **fee management
redesign** (frequency-driven monthly generation, fee rules, receipts with statuses, dues
carry-forward), (3) a **payroll redesign** (Teacher module merged into Employee, monthly salary
generation with dynamic adjustments), and (4) an **editable pre-generation review workflow**
shared by both fee and salary generation.

---

## Table of contents

1. [Current-state summary](#1-current-state-summary)
2. [Setup module](#2-setup-module)
3. [Fee management redesign](#3-fee-management-redesign)
4. [Payroll management redesign](#4-payroll-management-redesign)
5. [Editable monthly adjustments](#5-editable-monthly-adjustments)
6. [System design](#6-system-design)
7. [Database design](#7-database-design)
8. [Business rules](#8-business-rules)
9. [Workflows](#9-workflows)
10. [API surface](#10-api-surface)
11. [Migration strategy](#11-migration-strategy)
12. [Implementation phases](#12-implementation-phases)
13. [Open questions / decisions needed](#13-open-questions--decisions-needed)

---

## 1. Current-state summary

What already exists and is reused (nothing here is rebuilt from scratch):

| Area | Existing pieces | Kept / changed |
|---|---|---|
| Menu/permission catalog | `MenuSeeder.BuildMenuCatalog()` — data-driven, **self-healing sync** (pass 1 inserts missing codes, pass 2 rewrites every catalog row including `ParentId`) | Kept. The Setup module is almost entirely a catalog edit — menu `Id`s are stable, so **existing role-claim grants survive untouched** |
| Fee configuration | `FeeStructure` (header, one per `AcademicClass`) + `FeeStructureItem` (`FeeCategoryCode`, `Amount`, `FrequencyType` Monthly/Annual/OneTime, `IsOptional`, `IsRefundable`); `FeeCategory` Config catalog (1010) with suggested frequency in `AdditionalValue1` | Kept as the **configuration** layer. New: generation/receipt/payment layer on top |
| Discounts/scholarships | `StudentDiscount` / `StudentScholarship` per `Enrollment`, `AwardValueType` Percentage/FixedAmount, two-tier default from Config (1008/1009) | Kept — become recurring inputs to monthly fee generation |
| Fee computation | `GET /api/enrollments/{id}/fee-structure` computes an on-the-fly summary; **nothing is persisted, no statuses, no payments** | Kept as the "what the class charges" view; generation persists real invoices |
| Payroll structure | `EmployeeSalary` revision + `EmployeeSalaryComponent`/`Deduction`/`InsurancePremium` line items (Config catalogs 1013/1014/1015), `TaxCalculator`, `MonthlyBreakdownCalculator`, `EmployeeLoan` + `LoanCalculator`, `FiscalYear`/`TaxSlab` | Kept as the **compensation plan** (configuration). New: persisted `PayrollRun`/`SalarySlip` (transactional) |
| Payslips | `GET .../payslips` computes payslips **at read time** — flat `annualTax / 12`, `payDays = monthDays`, `upl = 0`, nothing persisted, nothing editable | Replaced by persisted, reviewable, adjustable salary generation (the read-time endpoints remain for preview) |
| Teacher vs Employee | Backend split already done (2026-07-15): `Teacher` is a thin profile sharing its PK with `Employee`; `/api/teachers/*` are thin aliases into `IEmployeeService` | Backend keeps the shared-PK model. The **standalone Teacher navigation module is retired**; teacher profile becomes a tab inside Employee |

Key architectural gap the redesign fills: **everything financial today is computed at read
time and never persisted**. A school needs an auditable record — "the January invoice said
Rs 4,500 and was paid on Feb 3" must stay true even after the fee structure is edited. The
redesign introduces a clean split between **configuration data** (structures, categories,
rules, compensation plans — editable, forward-looking) and **transactional data** (invoices,
receipts, payments, salary slips — generated snapshots, immutable once finalized).

---

## 2. Setup module

### 2.1 Goal

Keep the main navigation lightweight: day-to-day operational modules (students, enrollments,
employees, fee collection, payroll runs) stay top-level; everything that is *configured once
and consumed by generation logic* moves under a single **Setup** main menu.

### 2.2 Menu catalog changes (`MenuSeeder.BuildMenuCatalog()`)

New main menu:

```
MainMenu("SETUP", "Setup", "icons.ControlOutlined", 5, null)
```

Submenus **moved into `SETUP`** (existing codes — only their parent/order change, so `Id`s,
permission children, and every existing role-claim grant are preserved by the seeder's sync
pass):

| Code | Display | Moved from | Notes |
|---|---|---|---|
| `YEAR_LIST` | Academic Years | `ACADEMIC_MANAGEMENT` | incl. `YEAR_CLONE_STRUCTURE` |
| `CLASS_LIST` | Classes, Sections & Subjects | `ACADEMIC_MANAGEMENT` | class/section/subject structure is master data; enrollments (transactional) already live under Student Management |
| `FEE_STRUCTURE_LIST` | Fee Structures | `FEE_MANAGEMENT` | per-class fee configuration |
| `FISCAL_YEAR_LIST` | Fiscal Years & Tax Slabs | `PAYROLL_MANAGEMENT` | incl. all `TAX_SLAB_*` permissions |

Submenus **newly created under `SETUP`**:

| Code | Display | Purpose |
|---|---|---|
| `FEE_RULE_LIST` | Fee Rules | the new configurable fee-rule system (§3.3) |

Consequences for the source main menus:

- **`ACADEMIC_MANAGEMENT` is retired** — both of its submenus move to Setup, leaving it
  empty. (Alternative considered: keep it holding only `CLASS_LIST`; rejected — a main menu
  with one submenu defeats the "lightweight nav" goal, and class structure is definitionally
  setup data.)
- **`FEE_MANAGEMENT` stays**, now holding only transactional submenus: the new
  `FEE_INVOICE_LIST` (fee generation & receipts) and `FEE_PAYMENT_LIST` (payment collection)
  — see §3.
- **`PAYROLL_MANAGEMENT` stays**, now holding only transactional submenus: the new
  `PAYROLL_RUN_LIST` (salary generation) — see §4.
- **`TEACHER_MANAGEMENT` is retired** as part of the Teacher→Employee merge (§4.1).
- `CONFIG_MANAGEMENT` ("Master Settings") is **not** touched — it remains the home of the
  generic Config/ConfigType/AppConfig/Menu/DocumentTemplate administration. Setup is
  domain-specific master data; Master Settings is system-plumbing configuration. (The Fee
  Category, Salary Component, Deduction Type, etc. *catalogs* are still edited through
  Config Types under Master Settings — Setup screens should deep-link there where relevant.)

### 2.3 Seeder change required: a retire pass

`MenuSeeder` today only **inserts and updates** — a code removed from `BuildMenuCatalog()`
silently stays in the database, so retiring `ACADEMIC_MANAGEMENT` / `TEACHER_MANAGEMENT`
needs a third pass:

1. Add a `BuildRetiredMenuCodes()` list (`ACADEMIC_MANAGEMENT`, `TEACHER_MANAGEMENT` —
   `TEACHER_LIST` is *not* retired, it is redefined as a hidden permission under
   `EMPLOYEE_LIST`; see §4.1).
2. Pass 3 runs **after** the sync pass (so children have already been re-parented away) and
   soft-deletes any row whose code is in the retire list, honoring the existing
   "no delete while children exist" invariant — if a retired row still has non-deleted
   children, log a warning and skip (self-heals on the next boot once children move).
3. Soft-deleted menu codes stay reserved by the unique index (existing corollary) — that is
   fine; these codes are never reused.

### 2.4 Frontend impact (for the UI team — not implemented now)

- Nav tree comes from `GET /api/roles/user-menus`, which is already driven entirely by the
  menu rows — **the frontend nav re-shapes itself automatically** once the seeder runs.
- Route paths (`/apps/academic-year/list` etc.) are unchanged — only the grouping moves.
  Breadcrumbs that hardcode "Academic Management" need updating.
- New Setup landing page (optional): a card grid linking to the Setup submenus.

---

## 3. Fee management redesign

### 3.1 Concept model

Three layers, strictly separated:

```
CONFIGURATION (Setup)                 TRANSACTIONAL (Fee Management)
─────────────────────                 ──────────────────────────────
FeeCategory catalog (1010)            FeeInvoice        (one per enrollment per billing month)
  └ fee_frequency field                 └ FeeInvoiceLine (charges, installments, discounts,
FeeStructure / FeeStructureItem                            rule discounts, adjustments)
  (per class, per category)           FeePayment        (money actually received)
FeeRule (new)                           └ FeePaymentAllocation (payment → invoice, FIFO)
StudentDiscount / StudentScholarship  FeeAdjustment     (pre-generation monthly override, §5)
  (per enrollment, recurring)
```

Configuration is editable at any time and affects **future** generation only. Transactional
rows are snapshots: once an invoice leaves `Draft`, its lines are immutable (only status and
paid amounts move).

### 3.2 Fee frequency on the Fee Category configuration

Requirement: drive generation from a `fee_frequency` field on the Fee Category configuration
instead of a separate mechanism.

Implementation — formalize what is already half-there:

- `Config` rows of type **1010 (FeeCategory)** already carry a *suggested* frequency in
  `AdditionalValue1`. This becomes **normative**: `AdditionalValue1` MUST hold one of
  `MONTHLY` / `ANNUAL` / `ONE_TIME` for every FeeCategory option. `ConfigCatalogSeeder`
  is updated so all 11 seeded categories carry a valid value, and `ConfigService` gains a
  validation hook: creating/updating a Config under TypeCode 1010 rejects a missing/invalid
  `AdditionalValue1` (`ValidationError`). No schema change — this is the documented pattern
  of reusing Config's free-form slots (same as Discount/ScholarshipType default rates).
- `FeeStructureItem.FrequencyType` (the per-class effective value) is **kept** — the category
  supplies the default that pre-fills a new item, but the item's stored value is what
  generation reads. Rationale (already documented on the enum): schools differ on whether
  e.g. Computer Fee is monthly or annual. The category-level `fee_frequency` is the
  system-wide default and the value used when a rule or report needs a category-level answer.
- New constants: `Domain/Constants/FeeFrequencyCodes.cs` (`Monthly = "MONTHLY"`,
  `Annual = "ANNUAL"`, `OneTime = "ONE_TIME"`, `All`) mapping 1:1 onto the existing
  `FeeFrequencyType` enum, used wherever the string form lives in Config.

### 3.3 Fee rules (new, configurable, extensible)

New entity **`FeeRule`** (`Domain/Entities/FeeRule.cs`, `SoftDeleteAuditableEntity` —
financial-audit configuration) + `Application/FeeRules/` feature (IUnitOfWork-based, service
in Application, own `IFeeRuleRepository`).

| Field | Type | Meaning |
|---|---|---|
| `Id` | Guid | PK |
| `Code` | string, unique | e.g. `PAY_3_MONTHS_5PCT` |
| `Name` | string | display name |
| `RuleType` | enum `FeeRuleType` | `AdvanceMonthsDiscount = 1`, `EarlyPaymentDiscount = 2` (extension slots: `LateFee`, `SiblingAutoDiscount`, …) |
| `TriggerStage` | enum `FeeRuleTrigger` | `OnPayment = 1`, `OnGeneration = 2` — both shipped rule types are `OnPayment`; the field exists so future rules (late fee at generation) slot in |
| `ValueType` | `AwardValueType` (reused) | `Percentage` / `FixedAmount` |
| `Value` | decimal | the discount rate/amount |
| `MinMonthsTogether` | int? | `AdvanceMonthsDiscount`: minimum invoice-months settled in one payment (the "X") |
| `DaysBeforeDueDate` | int? | `EarlyPaymentDiscount`: pay ≥ N days before the invoice due date (0 = any time before due date) |
| `AcademicClassId` | Guid? | null = applies to all classes |
| `FeeCategoryCode` | string? | null = applies to the whole invoice recurring subtotal; set = only that category's lines |
| `EffectiveFrom` / `EffectiveTo` | DateTime / DateTime? | validity window (evaluated against payment date) |
| `Priority` | int | evaluation order when several rules match |
| `IsCombinable` | bool | false = first matching rule (by priority) wins; true = stacks with other combinable rules |
| `IsActive` | bool | quick on/off without deleting |

**Extensibility pattern** — one evaluator per rule type behind a common interface, selected
by a plain `switch` in the service (no reflection/plugin magic, consistent with repo style):

```
Application/FeeRules/Evaluation/
├── IFeeRuleEvaluator.cs        // Evaluate(FeeRuleContext) -> FeeRuleResult (discount lines)
├── AdvanceMonthsDiscountEvaluator.cs
├── EarlyPaymentDiscountEvaluator.cs
└── FeeRuleEngine.cs            // loads active matching rules, orders by Priority,
                                // applies combinability, returns discount lines
```

`FeeRuleContext` carries: the enrollment, the set of invoices being settled, the payment
date/amount, and each invoice's due date + recurring subtotal. `FeeRuleResult` is a list of
proposed negative `FeeInvoiceLine`s (source `RuleDiscount`, with `FeeRuleId` stamped) that
the payment flow presents for confirmation before applying (§9.3).

Adding a future rule type = new enum member + new evaluator + one `switch` arm + seeded
permission if it needs new endpoints. No schema change (the nullable parameter columns cover
the shipped shapes; a genuinely new parameter gets a new nullable column — acceptable, this
is configuration, not a hot table).

### 3.4 Fee generation & invoices

New entities **`FeeInvoice`** + **`FeeInvoiceLine`** and feature `Application/FeeInvoices/`
(own `IFeeInvoiceRepository`; the repository also owns `FeePayment`/`FeePaymentAllocation` —
aggregate-owns-children convention).

**`FeeInvoice`** (`SoftDeleteAuditableEntity` — financial record):

| Field | Type | Meaning |
|---|---|---|
| `Id` | Guid | PK |
| `InvoiceNo` | string, unique | auto `INV{year}{seq}` via the existing `NumberSequenceHelper` pattern |
| `EnrollmentId` | Guid FK | the student-in-a-section this bills |
| `AcademicYearId` | Guid FK | denormalized from the enrollment chain for cheap year filtering |
| `BillingYear` / `BillingMonth` | int / int | calendar year + month (1–12) of the billing period |
| `Status` | enum `FeeInvoiceStatus` | see below |
| `GrossAmount` | decimal | sum of positive lines |
| `DiscountAmount` | decimal | sum of negative lines (absolute) |
| `NetAmount` | decimal | gross − discounts ± adjustments |
| `PaidAmount` | decimal | allocated payments so far |
| `PreviousDueAmount` | decimal | **informational snapshot** at generation time of the enrollment's outstanding balance across earlier invoices (for the printed receipt's "previous dues" row) — the authoritative outstanding is always computed live (§8.6) |
| `DueDate` | DateTime | from configuration (§3.6) |
| `GeneratedTs` | DateTime? | when it left Draft |
| `Remarks` | string | |

Unique index: `(enrollment_id, billing_year, billing_month)` **partial, excluding
`Cancelled`** — one live invoice per enrollment per month; cancelling allows a regeneration.

**`FeeInvoiceStatus`** (`Domain/Enums/`):

| Value | Meaning | Editable? |
|---|---|---|
| `Draft = 1` | generated by the run for admin review; lines fully editable | yes |
| `Generated = 2` | finalized/issued; awaiting payment, not yet due | no (status/PaidAmount only) |
| `Pending = 3` | issued and past `DueDate` with balance remaining (set by the status-refresh rule, §8.5) | no |
| `PartiallyPaid = 4` | some but not all of `NetAmount` allocated | no |
| `Paid = 5` | fully settled | no |
| `Cancelled = 6` | voided (wrong generation, student left); excluded from dues | terminal |

**`FeeInvoiceLine`** (`AuditableEntity`, hard-deleted — pure line item, only mutable while
the invoice is `Draft`):

| Field | Type | Meaning |
|---|---|---|
| `Id` | Guid | PK |
| `FeeInvoiceId` | Guid FK | cascade delete with invoice |
| `Source` | enum `FeeLineSource` | `StructureItem = 1`, `AnnualInstallment = 2`, `OneTimeCharge = 3`, `Discount = 4`, `Scholarship = 5`, `RuleDiscount = 6`, `MonthlyAdjustment = 7`, `Manual = 8` |
| `FeeStructureItemId` | Guid? FK | set for sources 1–3 (snapshot lineage) |
| `StudentDiscountId` / `StudentScholarshipId` / `FeeRuleId` / `FeeAdjustmentId` | Guid? | lineage for sources 4–7 (one of them, matching `Source`) |
| `FeeCategoryCode` | string? | copied from the structure item (null for manual lines) |
| `Description` | string | printed line text, e.g. "Annual Fee — installment 3/12" |
| `Amount` | decimal | **signed**: positive = charge, negative = discount/adjustment-down |

Totals (`GrossAmount`/`DiscountAmount`/`NetAmount`) are recomputed and stored on every
Draft edit and once more at finalization — stored, not computed on read, because they are
part of the immutable snapshot.

### 3.5 How each frequency generates

For a monthly run over billing month *M* of academic year *Y*, per enrollment (status
`Enrolled`, section resolved → class → fee structure):

| Item frequency | Behavior |
|---|---|
| `Monthly` | one `StructureItem` line at the item's `Amount`, every month |
| `Annual` | the amount is the **total for one academic year**, payable in installments: one `AnnualInstallment` line per month = `Amount / N`, where **N = the number of billing months from the enrollment's first invoice month to the end of the academic year** (12 for a full-year enrollment; fewer for mid-year admission — the full annual fee is still recovered). Rounding: round each installment to 2dp, **last installment absorbs the remainder** so the sum is exactly `Amount` |
| `OneTime` | one `OneTimeCharge` line on the enrollment's **first** invoice only (admission fee, deposit). Detected by "no earlier non-cancelled invoice exists for this enrollment" |

`IsOptional` items apply only if an `EnrollmentFeeSelection` row exists (existing mechanism,
unchanged). `IsRefundable` items are charged but flagged on the printed receipt.

Discounts/scholarships (`StudentDiscount`/`StudentScholarship`): one negative line each per
month, computed against the **recurring subtotal** (Monthly lines + AnnualInstallment lines;
OneTime charges are never discounted — consistent with the existing documented
simplification, now made precise). `Percentage` = rate × recurring subtotal;
`FixedAmount` = the amount (treated as per-month). Floor: an invoice's `NetAmount` never
goes below 0 (excess discount is not carried as credit in v1 — see §13).

### 3.6 Generation-run mechanics

Generation is an explicit admin action, **not** a background scheduler (no job
infrastructure exists in this codebase; an idempotent "generate month M" button run late is
identical to a scheduler running on time — revisit if automation is later wanted):

- `POST /api/feeinvoices/generate` body `{ academicYearId, billingYear, billingMonth,
  classSectionId? (null = all sections), regenerateDrafts: bool }`.
- Idempotent: enrollments that already have a non-cancelled invoice for that month are
  skipped and reported (`SkippedEnrollmentIds` + reason), same shape as
  `CloneStructureResultDto`. `regenerateDrafts = true` deletes-and-recreates invoices still
  in `Draft` (picking up config changes); anything past Draft is never touched.
- All invoices of one run are created in **one `SaveChangesAsync`** (all-or-nothing, repo
  convention).
- Produces `Draft` invoices → admin review (§5) → `POST /api/feeinvoices/finalize` (bulk by
  filter or by ids) flips Draft → `Generated`, stamps `GeneratedTs`, locks lines.
- `DueDate` default: a new AppConfig `FEE_DUE_DAY_OF_MONTH` (GENERAL group, seeded default
  `10`) → due date = day 10 of the billing month (clamped to month length); editable per
  Draft invoice before finalization.

### 3.7 Payments, receipts & carry-forward

New entities **`FeePayment`** + **`FeePaymentAllocation`** (both owned by
`IFeeInvoiceRepository`).

**`FeePayment`** (`SoftDeleteAuditableEntity`): `Id`, `ReceiptNo` (unique, auto
`RCP{year}{seq}`), `EnrollmentId`, `PaymentDate`, `Amount`, `PaymentMode` (existing enum),
`ReferenceNo` (bank/wallet ref), `Remarks`, `Status` (enum `FeePaymentStatus`: `Confirmed = 1`,
`Voided = 2` — voiding reverses its allocations and re-derives affected invoice statuses;
no edit-in-place of money rows, ever).

**`FeePaymentAllocation`** (`AuditableEntity`, hard): `Id`, `FeePaymentId`, `FeeInvoiceId`,
`Amount`. A payment may settle many invoices; an invoice may be settled by many payments.

**Allocation policy — oldest first (FIFO), automatic:** a payment for enrollment E is
allocated across E's open invoices (`Generated`/`Pending`/`PartiallyPaid`, ordered by
`BillingYear, BillingMonth`) until exhausted. The January-then-February scenario falls out
naturally: both invoices are open, the parent sees both listed with a combined outstanding,
and any payment clears January before touching February. Over-payment (amount exceeding
total outstanding) is **rejected** in v1 (`ValidationError`) — no credit ledger yet (§13).

**Rule application at payment time:** before confirming, the payment endpoint runs
`FeeRuleEngine` (§3.3) against the invoices the tendered amount would settle; matching
rules produce proposed `RuleDiscount` lines. Because finalized invoices are immutable, rule
discounts are written as negative lines **appended with a dedicated source** and the
invoice's `DiscountAmount`/`NetAmount` are re-derived — this is the *single* sanctioned
post-finalization mutation, always machine-generated, never manual (manual corrections go
through Cancel + regenerate, or a `MonthlyAdjustment` on the *next* month). A preview
endpoint (`POST /api/feepayments/preview`) returns the allocation plan + rule discounts so
the UI can show "paying 3 months together saves Rs X" before the cashier confirms.

**Statement / dues view:** `GET /api/feeinvoices/statement/{enrollmentId}` — every
non-cancelled invoice with status, per-invoice balance, running total, and the live
`OutstandingAmount` (§8.6). This is the parent-facing "what do I owe" answer and the fee
receipt's data source (the existing `FeeReceipt` document template gains tokens for dues
rows — template catalog extension, not a new mechanism).

---

## 4. Payroll management redesign

### 4.1 Teacher module merged into Employee Management

Backend state: already 90% done (2026-07-15 split — shared PK, `TeacherService` forwards to
`IEmployeeService`). Remaining work is navigation, API posture, and UI:

1. **Menus**: retire the `TEACHER_MANAGEMENT` main menu (§2.3). Re-parent every
   `TEACHER_*` permission row under `EMPLOYEE_LIST` (catalog edit; the sync pass moves them
   in place, so `Id`s and existing grants survive). `TEACHER_LIST` itself doubles as the
   `Teachers/GetTeachers` permission row, so it is **not deleted** — since pass 2 rewrites
   `MenuType`/`IsHidden`/`ParentId` in place, keep the code and redefine it in the catalog
   as a hidden permission:
   `Permission("EMPLOYEE_LIST", "TEACHER_LIST", "View Teachers (legacy list API)", "Teachers", "GetTeachers", …)`.
   Existing grants keep working; the row simply stops rendering as nav.
2. **API**: `/api/teachers/*` alias endpoints are **kept** (documented as legacy-compatible;
   the UI migrates to `/api/employees/*` + teacher-profile sub-resources over time). No
   breaking removal in this phase.
3. **UI**: Employee list gains an `employeeCategoryCode` filter (backend filter already
   exists via `GetEmployeesQuery`). Employee profile page: when the employee's category is
   `ACADEMIC` **and a Teacher profile row exists**, render the Teacher Profile tab
   (license/experience/specialization, qualifications, class-subject assignments, teacher
   documents) inside the employee profile. When category is `ACADEMIC` but no profile exists
   yet, show the "Add Teacher Profile" action (`POST /api/employees/{id}/teacher-profile`,
   permission `EMPLOYEE_TEACHER_PROMOTE` — already seeded).
4. **Tab order** (requirement): profile tabs render as
   `Overview → [Teacher Profile] → Documents → … → Pay & Taxes → Compensation Plan`, with
   **Compensation Plan always last**. This is a frontend ordering constant; note it in the
   per-feature guide so future tabs are inserted *before* it.

### 4.2 Compensation plan (configuration — unchanged)

`EmployeeSalary` revisions + component/deduction/insurance line items stay exactly as built.
They are the **global recurring configuration** that salary generation snapshots from. The
Compensation Plan tab remains the editor for them.

### 4.3 Salary generation (new transactional layer)

Today's payslip endpoints compute everything at read time — nothing is persisted, nothing is
editable, `upl` is hardcoded 0. The redesign makes payroll a monthly **run** with review and
approval, mirroring the fee side:

```
CONFIGURATION                          TRANSACTIONAL
─────────────                          ─────────────
EmployeeSalary (+ line items)          PayrollRun         (one per fiscal month)
FiscalYear / TaxSlab                     └ SalarySlip     (one per employee per run)
EmployeeLoan (EMI schedule)                  └ SalarySlipLine
SalaryAdjustment (pre-run              (SalaryAdjustment rows get consumed/stamped
  monthly overrides, §5)                 by the run they applied to)
```

**`PayrollRun`** (`SoftDeleteAuditableEntity`): `Id`, `FiscalYearId`, `MonthIndex` (1–12,
Shrawan..Ashad — same fiscal-month convention as `MonthlyBreakdownCalculator`), `Status`
(enum `PayrollRunStatus`: `Draft = 1`, `Approved = 2`, `Paid = 3`, `Cancelled = 4`),
`GeneratedTs`, `ApprovedTs`, `ApprovedBy`, `PaidTs`, `Remarks`. Unique
`(fiscal_year_id, month_index)` partial excluding `Cancelled`.

**`SalarySlip`** (`SoftDeleteAuditableEntity`): `Id`, `SlipNo` (unique, auto
`SAL{fyCode}{seq}`), `PayrollRunId`, `EmployeeId`, `EmployeeSalaryId` (which revision was
snapshotted), `Status` (enum `SalarySlipStatus`: `Draft = 1`, `Approved = 2`, `Paid = 3`,
`Cancelled = 4` — normally follows the run; individually cancellable, e.g. one resigned
employee inside an otherwise-approved run), `MonthDays`, `PayDays`, `UnpaidLeaveDays`,
`GrossEarnings`, `TotalDeductions`, `TaxAmount`, `NetPay`, `Remarks`. Unique
`(payroll_run_id, employee_id)`.

**`SalarySlipLine`** (`AuditableEntity`, hard, mutable only while slip is `Draft`): `Id`,
`SalarySlipId`, `LineType` (enum `SalaryLineType`: `Earning = 1`, `Deduction = 2`, `Tax = 3`,
`LoanEmi = 4`, `Adjustment = 5`), `Source` (enum `SalaryLineSource`: `SalaryStructure = 1`,
`TaxCalculator = 2`, `LoanSchedule = 3`, `MonthlyAdjustment = 4`, `Manual = 5`),
`ComponentCode` (string?, Config code where applicable), `SalaryAdjustmentId` /
`EmployeeLoanId` (Guid?, lineage), `Description`, `Amount` (always positive; `LineType`
determines direction).

**Generation logic** (`POST /api/payrollruns` body `{ fiscalYearId, monthIndex }`): for
every `Active`/`OnLeave` employee with a compensation plan effective for that month:

1. Resolve the salary revision effective for the month (latest `EffectiveFromDate` ≤ month
   end — a refinement over today's "always latest").
2. Reuse **`MonthlyBreakdownCalculator`** for the base earnings/deductions/tax of that month
   (percentage components resolved against BASIC, OneTime components landing in their
   `EffectiveFromDate` month, flat `annualTax / 12` — same documented approximations).
3. Fold in due **`EmployeeLoan`** EMIs (existing `LoanCalculator` logic) as `LoanEmi` lines.
4. Fold in pending **`SalaryAdjustment`** rows for (employee, fiscal year, month) (§5) as
   `Adjustment` lines — unpaid-leave deduction, late fine, bonus, incentive, etc.
   Unpaid-leave adjustments entered as days are converted:
   `perDay = monthly recurring earnings / MonthDays`; the line stores the computed amount
   and the day count goes into `UnpaidLeaveDays` / reduces `PayDays`.
5. Compute totals, write everything as one `Draft` run in one `SaveChangesAsync`.

**Review → Approve → Paid**: admin edits Draft slip lines (add/remove/edit `Manual` and
`Adjustment` lines; structural lines editable too — it's a draft), totals re-derive on every
edit. `POST /api/payrollruns/{id}/approve` locks the run (slips → `Approved`, consumed
`SalaryAdjustment` rows stamped `Applied` with the run id). `POST /api/payrollruns/{id}/mark-paid`
records disbursement (v1: one timestamp for the run; per-slip bank-file export is out of
scope). Cancel is allowed from `Draft` (deletes nothing — run + slips go `Cancelled`) and,
with a SuperAdmin-level permission, from `Approved` *before* Paid.

**Relationship to existing payslip endpoints**: `GET .../payslips*` (read-time computed)
remain as the **projection/preview** for months with no persisted run; once a `SalarySlip`
exists for (employee, fy, month), those endpoints serve the persisted slip instead (single
source of truth once generated). The payslip document preview (`{{...}}` template) reads the
persisted slip's lines for generated months.

---

## 5. Editable monthly adjustments

The shared pre-generation review requirement, made precise. Three distinct kinds of value,
for both domains:

| Kind | Fee side | Payroll side | Lifetime |
|---|---|---|---|
| **Global recurring configuration** | `FeeStructureItem`, `StudentDiscount`, `StudentScholarship`, FeeCategory `fee_frequency`, `FeeRule` | `EmployeeSalary` components/deductions/insurance, `TaxSlab`, `EmployeeLoan` EMI | applies every month until changed; edited in Setup / profile tabs |
| **Monthly override (pre-generation)** | **`FeeAdjustment`** (new) | **`SalaryAdjustment`** (new) | entered for a specific (subject, year, month) *before* the run; consumed by generation; reusable pattern for recurring-but-variable items (late fines, incentives) |
| **One-time manual adjustment (post-generation, pre-finalization)** | edit `Draft` `FeeInvoiceLine`s (`Source = Manual`) | edit `Draft` `SalarySlipLine`s (`Source = Manual`) | lives only on that document; the catch-all for anything not worth configuring |

**`FeeAdjustment`** (`SoftDeleteAuditableEntity`, owned by `IFeeInvoiceRepository`): `Id`,
`EnrollmentId`, `BillingYear`, `BillingMonth`, `AdjustmentTypeCode` (new Config catalog
**1017 FeeAdjustmentType**, seeded: `SPECIAL_DISCOUNT`, `ADDITIONAL_CHARGE`, `FINE`,
`CARRY_CORRECTION`, `OTHER`; each option's `AdditionalValue1` = default direction
`CHARGE`/`CREDIT`), `Direction` (enum: `Charge = 1`, `Credit = 2`), `ValueType`
(`AwardValueType`), `Value`, `Remarks`, `Status` (enum `AdjustmentStatus`: `Pending = 1`,
`Applied = 2`, `Cancelled = 3`), `AppliedFeeInvoiceId` (Guid?).

**`SalaryAdjustment`** (`SoftDeleteAuditableEntity`, owned by `IEmployeeRepository`): `Id`,
`EmployeeId`, `FiscalYearId`, `MonthIndex`, `AdjustmentTypeCode` (new Config catalog
**1016 SalaryAdjustmentType**, seeded: `UNPAID_LEAVE`, `LATE_FINE`, `OVERTIME`, `BONUS`,
`INCENTIVE`, `ARREAR`, `OTHER`; `AdditionalValue1` = default direction
`EARNING`/`DEDUCTION`), `Direction` (enum: `Earning = 1`, `Deduction = 2`), `ValueType`,
`Value`, `Quantity` (decimal? — days for `UNPAID_LEAVE`, occurrences for `LATE_FINE`;
null for flat amounts), `Remarks`, `Status` (`AdjustmentStatus`), `AppliedSalarySlipId`
(Guid?).

Rules common to both:

- `Pending` adjustments are picked up by the next generation of their month and stamped
  `Applied` + linked to the produced document, all in the run's single `SaveChangesAsync`.
- An `Applied` adjustment is immutable (its effect is snapshotted in a document); correcting
  it means cancelling the Draft document (or issuing a compensating adjustment next month).
- Regenerating a Draft (fee `regenerateDrafts`, payroll cancel + re-create run) reverts its
  consumed adjustments to `Pending` first, so they re-apply — no lost or double-applied
  adjustments.
- Adjustments for a month whose document is already past Draft are rejected at entry with a
  clear message ("January payroll is already approved — enter this for February or cancel
  the run").

---

## 6. System design

### 6.1 Module responsibilities

| Module (nav) | Backend features | Responsibility |
|---|---|---|
| **Setup** | `AcademicYears`, `AcademicClasses`, `Fees` (structures), `FeeRules` (new), `Payroll/FiscalYears` | master data + generation-driving configuration; no money movement |
| **Fee Management** | `FeeInvoices` (new: generation, review, finalize, statement), `FeePayments` (new: collect, preview, void, receipts) | transactional student-money records |
| **Payroll Management** | `PayrollRuns` (new: generate, review, approve, mark-paid) | transactional employee-money records |
| **Employee Management** | `Employees` (absorbs Teacher UI; compensation plan, loans, adjustments entry) | people + their recurring compensation configuration |
| **Student Management** | unchanged (`Students`, `Guardians`, `Enrollments` — discounts/scholarships/fee selections/adjustments entry stay on the enrollment) | people + their recurring fee-relevant configuration |
| **Master Settings** | unchanged (`Configs`, `AppConfigs`, `Menus`, `DocumentTemplates`) | system plumbing; hosts catalogs 1016/1017 and FeeCategory `fee_frequency` |

All new features follow the standard shape: `I<Feature>Service`/`<Feature>Service` in
`Application/<Feature>/` (all are `IUnitOfWork`-based — nothing here touches Identity
managers), Commands/Queries/Validators/Dtos folders, one controller per feature, envelope
responses, named locals, `CancellationToken` throughout, new repositories added as
`IUnitOfWork` properties. Financial writes that must not be skippable after an irreversible
step (allocation reversal on void, adjustment re-pending on regenerate) use
`CancellationToken.None`, same rule as refresh-token revocation.

### 6.2 Data flow

```
                      SETUP (config)                         TRANSACTIONS
                      ─────────────                          ────────────
FeeCategory(1010, fee_frequency) ─┐
FeeStructure + Items ─────────────┤
StudentDiscount/Scholarship ──────┼──► FEE GENERATION ──► FeeInvoice(Draft)
EnrollmentFeeSelection ───────────┤         (run)              │ admin edits lines
FeeAdjustment(Pending) ───────────┘                            ▼
                                                          FeeInvoice(Generated)
FeeRule ────────────► PAYMENT (rule engine) ◄────────────      │ due date passes → Pending
                            │                                  ▼
                       FeePayment + Allocations ──► PartiallyPaid / Paid


EmployeeSalary + line items ──┐
FiscalYear/TaxSlab ───────────┼──► SALARY GENERATION ──► PayrollRun(Draft) + SalarySlips
EmployeeLoan (EMI) ───────────┤         (run)                  │ admin edits lines
SalaryAdjustment(Pending) ────┘                                ▼
                                                          Approved ──► Paid
```

The one deliberate crossing of the config/transaction boundary: `OnPayment` fee-rule
discounts append machine-generated lines to already-finalized invoices (§3.7). Everything
else mutates only Drafts.

---

## 7. Database design

All new tables: `dbo` schema, snake_case, explicit `HasColumnName`, configurations beside
the entities, indexes named `ix_<table>_<column>`. Enums stored as `int`. **Migrations are
user-owned — none are created as part of this work**; the full DDL inventory is listed for
the migration author.

### 7.1 New tables

| Table | Key columns (beyond audit/soft-delete) | Indexes/constraints |
|---|---|---|
| `dbo.fee_rules` | code, name, rule_type, trigger_stage, value_type, value, min_months_together?, days_before_due_date?, academic_class_id? FK, fee_category_code?, effective_from, effective_to?, priority, is_combinable, is_active | unique `ix_fee_rules_code`; FK restrict to academic_classes |
| `dbo.fee_invoices` | invoice_no, enrollment_id FK, academic_year_id FK, billing_year, billing_month, status, gross_amount, discount_amount, net_amount, paid_amount, previous_due_amount, due_date, generated_ts?, remarks | unique `ix_fee_invoices_invoice_no`; **partial unique** `ix_fee_invoices_enrollment_period` on (enrollment_id, billing_year, billing_month) `WHERE status <> 6 AND is_deleted = false`; index on (academic_year_id, billing_year, billing_month), (enrollment_id, status) |
| `dbo.fee_invoice_lines` | fee_invoice_id FK **cascade**, source, fee_structure_item_id? FK restrict, student_discount_id?, student_scholarship_id?, fee_rule_id?, fee_adjustment_id?, fee_category_code?, description, amount (signed) | index on fee_invoice_id |
| `dbo.fee_payments` | receipt_no, enrollment_id FK, payment_date, amount, payment_mode, reference_no?, status, remarks | unique `ix_fee_payments_receipt_no`; index (enrollment_id, payment_date) |
| `dbo.fee_payment_allocations` | fee_payment_id FK cascade, fee_invoice_id FK restrict, amount | unique (fee_payment_id, fee_invoice_id); index on fee_invoice_id |
| `dbo.fee_adjustments` | enrollment_id FK, billing_year, billing_month, adjustment_type_code, direction, value_type, value, remarks, status, applied_fee_invoice_id? | index (enrollment_id, billing_year, billing_month, status) |
| `dbo.payroll_runs` | fiscal_year_id FK, month_index, status, generated_ts?, approved_ts?, approved_by?, paid_ts?, remarks | **partial unique** (fiscal_year_id, month_index) `WHERE status <> 4 AND is_deleted = false` |
| `dbo.salary_slips` | slip_no, payroll_run_id FK, employee_id FK, employee_salary_id FK, status, month_days, pay_days, unpaid_leave_days, gross_earnings, total_deductions, tax_amount, net_pay, remarks | unique `ix_salary_slips_slip_no`; unique (payroll_run_id, employee_id); index (employee_id) |
| `dbo.salary_slip_lines` | salary_slip_id FK cascade, line_type, source, component_code?, salary_adjustment_id?, employee_loan_id?, description, amount | index on salary_slip_id |
| `dbo.salary_adjustments` | employee_id FK, fiscal_year_id FK, month_index, adjustment_type_code, direction, value_type, value, quantity?, remarks, status, applied_salary_slip_id? | index (employee_id, fiscal_year_id, month_index, status) |

FK note (repo convention): every FK whose property name doesn't literally match
`<Nav>Id` gets an explicit `HasOne(...).WithMany(...).HasForeignKey(...)` to avoid shadow
columns; self-evident here but restated because every one of these tables has multiple FKs.

### 7.2 Changed/confirmed existing schema

- **No column changes** to `fee_structures`, `fee_structure_items`, `student_discounts`,
  `student_scholarships`, `employee_salaries` + children, `employee_loans`, `fiscal_years`,
  `tax_slabs`, `teachers`, `employees`. The redesign is additive on the schema side.
- `Config` (1010 FeeCategory): `AdditionalValue1` becomes normative `fee_frequency` —
  data-fix step in the migration/seeder ensures every existing 1010 row has a valid value
  (default `MONTHLY` when blank).
- New Config **types** 1016 (SalaryAdjustmentType) and 1017 (FeeAdjustmentType) + their
  options — seeded by `ConfigCatalogSeeder` (data, not schema).
- New AppConfig row `FEE_DUE_DAY_OF_MONTH` — seeded by `AppConfigSeeder` (data).
- Menu moves/retires — handled entirely by `MenuSeeder` (data).
- Reminder: the large **pre-existing pending migration backlog** in CLAUDE.md (“Known
  gaps”) must land before or together with these — the new tables FK into
  `employees`/`fiscal_years`/`enrollments`, which themselves have pending schema work.

### 7.3 New enums (`Domain/Enums/`)

`FeeRuleType`, `FeeRuleTrigger`, `FeeInvoiceStatus`, `FeeLineSource`, `FeePaymentStatus`,
`PayrollRunStatus`, `SalarySlipStatus`, `SalaryLineType`, `SalaryLineSource`,
`AdjustmentStatus`, `AdjustmentDirection` (shared by both adjustment tables:
`Increase = 1`/`Decrease = 2` — "charge/earning" vs "credit/deduction" naming resolved per
side in the DTO layer). New constants: `FeeFrequencyCodes`, plus additions to
`ConfigTypeCodes` (1016, 1017).

---

## 8. Business rules

Numbered for traceability from code reviews and tests.

### Fee side

- **F1 — Monthly generation basis.** Fees are generated per enrollment per calendar billing
  month, one live invoice max per (enrollment, year, month).
- **F2 — Frequency semantics.** `Monthly` = full amount every month. `Annual` = total for
  one academic year, auto-split into equal monthly installments over the enrollment's
  remaining billing months (last installment absorbs rounding). `OneTime` = first invoice
  only.
- **F3 — Category frequency default.** FeeCategory (1010) `AdditionalValue1` must be a valid
  `fee_frequency`; it pre-fills new structure items; the item's own `FrequencyType` is what
  generation executes.
- **F4 — Discount scope.** Recurring discounts/scholarships and percentage fee-rules apply
  to the recurring subtotal (Monthly + AnnualInstallment lines) only; OneTime charges are
  never discounted. `NetAmount` floors at 0.
- **F5 — Status lifecycle.** `Draft → Generated → (Pending | PartiallyPaid) → Paid`;
  `Cancelled` reachable from Draft/Generated/Pending (not from PartiallyPaid/Paid — void the
  payments first). Lines are mutable in Draft only; the sole post-finalization line append
  is a machine-generated `RuleDiscount` at payment time.
- **F6 — Status refresh.** `Pending` = `Generated` with `DueDate < today` and balance > 0.
  Derived, stamped opportunistically (on read of statement/list and during payment) — same
  self-healing pattern as `EmployeeLoan` auto-close; no scheduler.
- **F7 — Carry-forward.** Outstanding(enrollment) = Σ(`NetAmount` − `PaidAmount`) over all
  non-cancelled, non-draft invoices. Unpaid months accumulate indefinitely and are always
  visible on the statement; `PreviousDueAmount` on each invoice is a print-time snapshot,
  never used for arithmetic.
- **F8 — Payment allocation.** FIFO by billing period across open invoices; no partial
  invoice skipping; over-payment rejected (v1, no credit ledger).
- **F9 — Fee rules.** Evaluated at their `TriggerStage`; filtered by active window, class
  and category scope; ordered by `Priority`; non-combinable rule short-circuits.
  `AdvanceMonthsDiscount` requires the single payment to fully settle ≥ `MinMonthsTogether`
  open invoices; `EarlyPaymentDiscount` requires payment date ≤ `DueDate −
  DaysBeforeDueDate` for the invoice it discounts.
- **F10 — Payments are append-only.** Corrections = `Voided` payment (reverses allocations,
  re-derives statuses) + new payment. Receipt numbers are never reused.
- **F11 — Regeneration.** Only Draft invoices can be regenerated; doing so re-pends their
  consumed `FeeAdjustment` rows.

### Payroll side

- **P1 — Monthly run basis.** One live run per (fiscal year, month index); one slip per
  employee per run.
- **P2 — Snapshot semantics.** Generation resolves the salary revision effective for the
  month and copies computed lines into the slip; later config edits never change an existing
  slip.
- **P3 — Calculation composition.** Base = `MonthlyBreakdownCalculator` (percentage
  components against BASIC, OneTime in its effective month, flat annualTax/12) + loan EMIs
  due that month + applied `SalaryAdjustment`s + manual draft edits.
- **P4 — Unpaid leave.** Entered as days (quantity) on an `UNPAID_LEAVE` adjustment;
  deduction = days × (monthly recurring earnings / month days); reflected in
  `PayDays`/`UnpaidLeaveDays` on the slip.
- **P5 — Lifecycle.** Run: `Draft → Approved → Paid`; `Cancelled` from Draft (freely) or
  Approved (elevated permission, only while unpaid). Slip inherits the run but can be
  individually cancelled while the run is Draft/Approved-unpaid.
- **P6 — Approval locks.** Approve stamps `ApprovedBy`/`ApprovedTs`, locks all slip lines,
  stamps consumed adjustments `Applied`.
- **P7 — Loans.** EMI lines are read-time-derived at generation from the loan schedule
  (existing `LoanCalculator`); once snapshotted into an approved slip they are the durable
  record for that month. Loan auto-close keeps working off the schedule, unchanged.
- **P8 — Read-time endpoints defer to persisted slips.** For any (employee, fy, month) with
  a persisted slip, payslip list/detail/preview serve the slip; otherwise they serve the
  existing computed projection, clearly flagged `isProjection: true` in the DTO.

### Shared

- **S1 — Adjustment lifecycle.** `Pending → Applied` (by a run, linked to the document) or
  `→ Cancelled` (manual, while Pending). Applied adjustments are immutable; regeneration
  re-pends them atomically.
- **S2 — Config vs transaction.** Configuration changes never retroactively alter finalized
  documents; corrections flow through cancel/void/compensating-adjustment paths only.
- **S3 — Validation order.** Standard service order everywhere: FluentValidation → existence
  checks (`NotFound`) → invariant checks (`Conflict`/`ValidationError`) → mutate via
  `IUnitOfWork` → single `SaveChangesAsync` → map → envelope.
- **S4 — Code validation.** Every `*TypeCode`/`*CategoryCode` field validated against its
  Config catalog via `Configs.CodeExistsAsync(typeCode, code)`, never a DB FK (existing
  convention and its documented orphaned-code caveat).

---

## 9. Workflows

### 9.1 Fee configuration (Setup)

```
Admin → Setup → Fee Structures
  1. Ensure FeeCategory options exist (Master Settings → Configs, type 1010)
     — each option carries fee_frequency (MONTHLY/ANNUAL/ONE_TIME)
  2. Create/adjust the class's FeeStructure + items (frequency pre-filled from category)
  3. (Optional) Setup → Fee Rules: define advance-months / early-payment discounts
  4. (Per student) Enrollment page: discounts, scholarships, optional-fee selections
Config complete — nothing billed yet.
```

### 9.2 Fee generation & review

```
Admin → Fee Management → Fee Generation
  1. POST generate {year, month, scope}          → Draft invoices + skip report
  2. Review list (filter: class/section/status)  → open Draft
  3. Edit lines (add Manual, tweak, remove) — totals re-derive; pending FeeAdjustments
     are already folded in as MonthlyAdjustment lines
  4. Finalize (bulk or per-invoice)              → Generated, lines locked, DueDate set
  5. Due date passes unpaid                      → status shows Pending (self-healing)
Regeneration of Drafts allowed; Generated+ never regenerated.
```

### 9.3 Fee payment

```
Cashier → Fee Management → Payments → select enrollment
  1. Statement shows all open invoices (Jan + Feb + …) with live outstanding
  2. Enter tendered amount → POST preview
       → allocation plan (FIFO) + fee-rule evaluation
       → "Pays Jan+Feb+Mar in full → AdvanceMonthsDiscount 5% (−Rs X)" preview
  3. Confirm → FeePayment + allocations + RuleDiscount lines written atomically
  4. Invoice statuses re-derived (PartiallyPaid/Paid); receipt (ReceiptNo) printable
     via the FeeReceipt document template (dues + payment tokens added)
Mistake? → Void payment (reverses allocations, restores statuses) → re-enter.
```

### 9.4 Salary generation & approval

```
Admin (pre-run, any time in the month) → Employee profile → Adjustments
  0. Enter SalaryAdjustments: UNPAID_LEAVE 2 days, LATE_FINE, BONUS, … (Pending)

Admin → Payroll Management → Payroll Runs
  1. POST run {fiscalYear, monthIndex}   → Draft run + Draft slips
       (structure + tax + loan EMIs + pending adjustments, all snapshotted)
  2. Review slips; edit lines; per-slip cancel for leavers
  3. Approve run → locked, adjustments stamped Applied, ApprovedBy recorded
  4. Mark Paid after disbursement
Payslip endpoints/templates now serve the persisted slip for this month.
```

### 9.5 Monthly adjustment process (shared)

```
Value known before the run?    → enter a FeeAdjustment / SalaryAdjustment (Pending)
Noticed while reviewing Draft? → edit the Draft document's lines (Manual)
Noticed after finalize/approve?→ fee: cancel invoice (if unpaid) & regenerate,
                                  or compensating adjustment next month
                                 payroll: cancel run (if unpaid, elevated) & regenerate,
                                  or ARREAR/deduction adjustment next month
```

---

## 10. API surface

All endpoints follow existing conventions (envelope, `ActionResult<CommonResponse<T>>`,
validators, permission rows in `MenuSeeder`). New permission codes in parentheses.

### FeeRulesController (`/api/feerules`) — Setup

| Endpoint | Purpose | Permission |
|---|---|---|
| `GET /` (paged, filters: ruleType, isActive) | list | `FEE_RULE_LIST` (submenu row) |
| `POST /` | create | `FEE_RULE_CREATE` |
| `GET /{id}` | detail | `FEE_RULE_DETAIL` |
| `PUT /{id}` | update | `FEE_RULE_UPDATE` |
| `DELETE /{id}` | soft delete | `FEE_RULE_DELETE` |

### FeeInvoicesController (`/api/feeinvoices`) — Fee Management

| Endpoint | Purpose | Permission |
|---|---|---|
| `GET /` (paged; filters: year, month, class, section, status, enrollment) | list | `FEE_INVOICE_LIST` (submenu) |
| `POST /generate` | run generation → Draft + skip report | `FEE_INVOICE_GENERATE` |
| `GET /{id}` | detail with lines | `FEE_INVOICE_DETAIL` |
| `PUT /{id}` (Draft only: dueDate, remarks) | edit header | `FEE_INVOICE_UPDATE` |
| `POST /{id}/lines` / `PUT /{id}/lines/{lineId}` / `DELETE /{id}/lines/{lineId}` (Draft only) | edit lines | `FEE_INVOICE_LINE_EDIT` |
| `POST /finalize` (ids or filter) | Draft → Generated | `FEE_INVOICE_FINALIZE` |
| `POST /{id}/cancel` | cancel | `FEE_INVOICE_CANCEL` |
| `GET /statement/{enrollmentId}` | dues statement | `FEE_INVOICE_STATEMENT` |
| `GET /adjustments?enrollmentId=&year=&month=` + `POST/PUT/DELETE /adjustments…` | FeeAdjustment CRUD | `FEE_ADJUSTMENT_MANAGE` |

### FeePaymentsController (`/api/feepayments`) — Fee Management

| Endpoint | Purpose | Permission |
|---|---|---|
| `GET /` (paged; filters: enrollment, dates, mode, status) | list | `FEE_PAYMENT_LIST` (submenu) |
| `POST /preview` | allocation + rule preview (no writes) | `FEE_PAYMENT_PREVIEW` |
| `POST /` | confirm payment | `FEE_PAYMENT_CREATE` |
| `GET /{id}` | detail with allocations | `FEE_PAYMENT_DETAIL` |
| `POST /{id}/void` | void + reverse | `FEE_PAYMENT_VOID` |
| `GET /{id}/receipt-preview` | rendered receipt HTML (document template) | `FEE_PAYMENT_RECEIPT_PREVIEW` |

### PayrollRunsController (`/api/payrollruns`) — Payroll Management

| Endpoint | Purpose | Permission |
|---|---|---|
| `GET /` (paged; filters: fiscalYear, status) | list runs | `PAYROLL_RUN_LIST` (submenu) |
| `POST /` | generate run | `PAYROLL_RUN_CREATE` |
| `GET /{id}` | run detail + slip summaries | `PAYROLL_RUN_DETAIL` |
| `POST /{id}/approve` | approve | `PAYROLL_RUN_APPROVE` |
| `POST /{id}/mark-paid` | record disbursement | `PAYROLL_RUN_MARK_PAID` |
| `POST /{id}/cancel` | cancel (Draft; Approved-unpaid needs `PAYROLL_RUN_CANCEL_APPROVED`) | `PAYROLL_RUN_CANCEL` |
| `GET /{id}/slips/{slipId}` | slip detail with lines | `SALARY_SLIP_DETAIL` |
| slip line edit endpoints (Draft only), slip cancel | review edits | `SALARY_SLIP_EDIT` / `SALARY_SLIP_CANCEL` |

### EmployeesController additions

| Endpoint | Purpose | Permission |
|---|---|---|
| `GET/POST/PUT/DELETE /api/employees/{id}/adjustments…` (filters: fy, month, status) | SalaryAdjustment CRUD | `EMPLOYEE_SALARY_ADJUSTMENT_MANAGE` |

(Teacher alias equivalents are **not** added for the new endpoints — new UI work goes
straight to the employee routes; existing teacher aliases remain frozen at their current
surface.)

---

## 11. Migration strategy

### 11.1 Sequencing principle

Every step is additive-first and reversible until the final "flip": new tables and features
land dormant (nothing generates until an admin runs a generation), menus move in one seeder
release, and no existing endpoint changes shape. Existing data (`FeeStructure`s, discounts,
scholarships, compensation plans, loans, fiscal years) is consumed as-is — **no data
transformation of existing rows is required anywhere in this plan.**

### 11.2 Step-by-step

1. **Schema** (user-owned migration, after the already-pending backlog): the 10 new tables
   (§7.1). Purely additive — zero risk to existing data.
2. **Seeder release** (one deploy):
   - `MenuSeeder`: add `SETUP` main menu + `FEE_RULE_LIST`/`FEE_INVOICE_LIST`/
     `FEE_PAYMENT_LIST`/`PAYROLL_RUN_LIST` submenus + all new permission rows; re-parent
     `YEAR_LIST`, `CLASS_LIST`, `FEE_STRUCTURE_LIST`, `FISCAL_YEAR_LIST` under `SETUP`;
     re-parent all `TEACHER_*` permissions under `EMPLOYEE_LIST` and redefine `TEACHER_LIST`
     as a hidden permission; add the new **retire pass** (§2.3) soft-deleting
     `ACADEMIC_MANAGEMENT` and `TEACHER_MANAGEMENT`.
   - `ConfigCatalogSeeder`: types 1016/1017 + options; backfill/validate `fee_frequency`
     on every 1010 option (create-if-missing stays the rule for options; the 1010
     `AdditionalValue1` backfill is a one-time targeted fix for blank values only —
     never overwriting an admin-set value).
   - `AppConfigSeeder`: `FEE_DUE_DAY_OF_MONTH`.
   - **Grant preservation check**: because moves are updates in place, every existing
     `ApplicationRoleClaim` keeps pointing at the same menu `Id` — verify post-deploy that
     non-SuperAdmin roles still see their moved menus (they will, under the new parent).
3. **Backend features** in dependency order: FeeRules → FeeInvoices (+adjustments) →
   FeePayments → SalaryAdjustments → PayrollRuns. Each with its per-feature guide +
   `UI-Implementation-Guide.md` section.
4. **Payslip source-of-truth switch**: `GetPayslips`/`GetPayslipDetail`/payslip preview gain
   the persisted-slip-first lookup (P8). Backward compatible — months without runs behave
   exactly as today.
5. **Frontend**: nav re-shapes automatically from the menu tree; Teacher module screens
   fold into Employee profile tabs (Compensation Plan pinned last); new screens for fee
   generation/payments/payroll runs/adjustments.
6. **Deprecation (later, optional)**: once the UI no longer calls `/api/teachers/*`, the
   aliases can be removed together with their permission rows — explicitly out of scope now.

### 11.3 Preserving existing fee & payroll data

- There are **no existing fee receipts or salary slips to migrate** — today's system never
  persisted any. History begins with the first generated month; months before adoption
  simply have no invoices/slips (the statement starts at the adoption month; if a school
  needs pre-adoption dues, they are entered as a `FeeAdjustment` `ADDITIONAL_CHARGE`
  ("opening balance") on the first generated month — document this in the operator guide).
- `FeeStructure`/`StudentDiscount`/`StudentScholarship`/`EnrollmentFeeSelection` rows feed
  generation unchanged. `EmployeeSalary` + line items feed runs unchanged. `EmployeeLoan`
  EMIs fold in unchanged.
- Teacher records: already `Employee` rows (shared-PK split, done). No data migration.

### 11.4 Backward compatibility summary

| Consumer | Impact |
|---|---|
| Existing role grants | preserved (menu `Id`s stable through re-parenting) |
| `/api/teachers/*` | unchanged (frozen surface) |
| `/api/enrollments/{id}/fee-structure` | unchanged (config view) |
| Payslip read endpoints | additive (`isProjection` flag; persisted slip when it exists) |
| Fee receipt / payslip document templates | additive new tokens (existing templates keep rendering; seeded defaults updated) |
| Frontend nav | self-adjusts from menu tree; breadcrumb text updates needed |

---

## 12. Implementation phases

| Phase | Contents | Depends on |
|---|---|---|
| **0. Menus & catalogs** | Setup module, teacher-menu merge, retire pass, catalogs 1016/1017, `fee_frequency` normalization, `FEE_DUE_DAY_OF_MONTH` | nothing (deployable alone) |
| **1. Schema** | user-owned migration for the 10 new tables | pending migration backlog |
| **2. Fee rules** | `FeeRule` entity/feature/evaluators (engine unit-testable standalone) | 1 |
| **3. Fee generation** | `FeeInvoice`/lines, `FeeAdjustment`, generate/review/finalize/statement | 1 (2 for rule previews) |
| **4. Fee payments** | `FeePayment`/allocations, preview, void, receipt template tokens | 3 |
| **5. Salary adjustments** | `SalaryAdjustment` CRUD on employee profile | 1 |
| **6. Payroll runs** | `PayrollRun`/`SalarySlip`/lines, approve/paid, payslip source-of-truth switch | 5 |
| **7. Frontend** | Setup nav, employee-tab merge (Compensation Plan last), fee & payroll screens | 0–6 per screen |

Each backend phase ships with: validators, permission rows, per-feature guide,
`UI-Implementation-Guide.md` update, and idempotent seeder changes.

---

## 13. Open questions / decisions needed

Deliberately deferred, with the v1 default stated:

1. **Over-payment / credit ledger** — v1 rejects over-payment. If schools routinely accept
   advance deposits, a `FeeCreditLedger` (per enrollment) is the follow-up; the FIFO
   allocator already gives it a natural consumption point.
2. **Late fees** — the `FeeRule` engine has the extension slot (`LateFee`, `OnGeneration`
   trigger adding a charge line for months with overdue prior invoices); not shipped in v1.
3. **Partial-month salary proration** (mid-month joiners/leavers) — v1 handles it via an
   `UNPAID_LEAVE`-style adjustment; a first-class proration rule can come later.
4. **Attendance integration** — unpaid leave/late fines are manual adjustments until an
   attendance module exists to feed them automatically (the `SalaryAdjustment` table is the
   designed integration point: an attendance module would create Pending rows).
5. **Bank disbursement export** — `mark-paid` is a single stamp in v1; per-slip payment
   status + bank-file export is a later increment.
6. **Bikram Sambat calendar** — fee billing months are Gregorian; payroll months remain the
   existing fiscal-month approximation. A real BS calendar library would refine both; out of
   scope, consistent with the documented `MonthlyBreakdownCalculator` trade-off.
7. **Fee generation automation** — manual, idempotent runs in v1; if a scheduler is added
   later, it calls the same generate endpoint logic.
