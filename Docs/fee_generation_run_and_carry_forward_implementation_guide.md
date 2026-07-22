# Fee Generation Run & Carry-Forward — UI Implementation Guide (2026-07-18)

Additions to the Fee Generation module: a **master table** grouping a billing month's invoices by
class → student (the fee-side counterpart of Payroll Runs), an **automatic carry-forward** that
voids a prior outstanding invoice and rebills its balance on the next bill as a
`CARRY_CORRECTION` adjustment, adjustment-list enrichment with student/class context, and a
reworked default view for the fee-module student search. Also: `GET /api/feeinvoices` now
accepts multiple `status` values, and two generation-time bugs were fixed (see §5). Complements
(never replaces) `Docs/setup_fee_payroll_redesign_implementation_guide.md` §4/§10 and the fee
sections of `UI-Implementation-Guide.md` — both updated alongside this guide.

All responses use the standard envelope `{ responseCode, responseMessage, data }`.

> **Migration required**: new table `dbo.fee_generation_runs`; new columns
> `dbo.fee_invoices.carried_forward_amount numeric NOT NULL DEFAULT 0` and
> `dbo.fee_invoices.carried_forward_to_invoice_id uuid NULL` — user-owned, not created by the
> backend. `/api/feegenerationruns` 500s and every read/write of `FeeInvoice` fails until all
> three exist.

## 1. Fee Generation Runs — `/api/feegenerationruns`

One live row per `(academicYearId, billingYear, billingMonth)`, found-or-created automatically
by `POST /api/feeinvoices/generate` — no separate create endpoint. Calling `generate` with a
class filter (or none — "all classes") always lands on the same run for that period, so the run
naturally accumulates across however many scoped generate calls an admin makes for the same
month (matches the "No class selected — this run covers EVERY class" generate-dialog banner).

| Endpoint | Notes |
|---|---|
| `GET /api/feegenerationruns?page=&pageSize=&academicYearId=&billingYear=&billingMonth=` | paged list, newest period first |
| `GET /api/feegenerationruns/{id}` | run header + one summary row per class (**no** nested students/invoices) |
| `GET /api/feegenerationruns/{id}/classes/{academicClassId}` | one class's student → invoice tree — call this when the UI expands a class row |

**2026-07-21: split into two endpoints, deliberately not one bulky response.** The detail
endpoint used to eagerly return the full class → student → invoice tree in a single payload,
which meant every run detail view paid for every class's every invoice even when the UI only
shows collapsed class rows until the user expands one. Now the detail endpoint returns rollups
only, and expanding a class fires a second, separate call scoped to just that class.

List row (`FeeGenerationRunDto`):

```json
{
  "id": "GUID",
  "academicYearId": "GUID",
  "academicYearCode": "AY2083",
  "billingYear": 2026,
  "billingMonth": 7,
  "generatedTs": "2026-07-01T00:00:00Z",
  "lastRegeneratedTs": null,
  "remarks": null,
  "invoiceCount": 42,
  "classCount": 6,
  "studentCount": 42,
  "totalNetAmount": 210000.00,
  "totalPaidAmount": 85000.00,
  "totalOutstandingAmount": 125000.00
}
```

Detail (`FeeGenerationRunDetailDto`) adds `classes` — summary rows only:

```json
{
  ...same fields as above...,
  "classes": [
    {
      "academicClassId": "GUID",
      "gradeCode": "NURSERY",
      "invoiceCount": 8,
      "studentCount": 8,
      "draftInvoiceCount": 5,
      "totalNetAmount": 40000.00,
      "totalPaidAmount": 12000.00,
      "totalOutstandingAmount": 28000.00
    }
  ]
}
```

`draftInvoiceCount` is new (2026-07-21) — the still-editable, not-yet-finalized invoices in that
class, so the UI can render "Finalize Drafts (N)" on the class row without a second call.

Class drill-down (`GET /api/feegenerationruns/{id}/classes/{academicClassId}`,
`FeeGenerationClassGroupDto`) — same rollup fields as one `classes[]` entry above, plus the
student → invoice tree:

```json
{
  "academicClassId": "GUID",
  "gradeCode": "NURSERY",
  "invoiceCount": 8,
  "studentCount": 8,
  "draftInvoiceCount": 5,
  "totalNetAmount": 40000.00,
  "totalPaidAmount": 12000.00,
  "totalOutstandingAmount": 28000.00,
  "students": [
    {
      "enrollmentId": "GUID",
      "studentId": "GUID",
      "studentName": "Anjali Rai",
      "admissionNo": "ADM2026101",
      "sectionCode": "A",
      "totalNetAmount": 5000.00,
      "totalPaidAmount": 0.00,
      "totalOutstandingAmount": 5000.00,
      "invoices": [ /* FeeInvoiceDto[], includeLines: false */ ]
    }
  ]
}
```

404s (`NotFound`) if the run doesn't exist, or if `academicClassId` doesn't exist or isn't in the
run's own academic year.

`gradeCode` is the raw Config code (catalog 1001) — resolve the Nursery/LKG/... display label
from the Grade dropdown, same convention as every other `gradeCode` field in the API.
`totalOutstandingAmount` at every level naturally excludes voided/carried invoices (see §2) since
they're `Cancelled`, already excluded from every outstanding sum, **and** excludes Draft invoices
by design (an unfinalized invoice's amount can still change and can't be paid against yet —
`POST /api/feeinvoices/finalize` moves it to `Generated` first). A class whose invoices are all
still Draft will correctly show `totalOutstandingAmount: 0` with a non-zero `draftInvoiceCount`
until they're finalized.

## 2. Automatic carry-forward voids the old invoice (`CARRY_CORRECTION`)

When `POST /api/feeinvoices/generate` builds an enrollment's invoice, it now looks at that
enrollment's invoices from **strictly earlier** billing periods (not Draft, not already
Cancelled) and sums their remaining balance. If that sum is greater than zero:

- A **Pending** `FeeAdjustment` is created for the invoice being generated, using the
  `CARRY_CORRECTION` code (already seeded under Config catalog 1017 — "Opening Balance / Carry
  Correction", Image #1) — `direction: 1 Increase`, `valueType: 2 FixedAmount`,
  `value` = the summed balance, `remarks` = `"Carried forward from INV20260008"` (every
  contributing invoice's number, comma-separated if more than one). It folds into the new
  invoice's lines exactly like any other Pending adjustment (a `MonthlyAdjustment`-sourced line)
  — no separate line-composition logic exists for it — and its line **description is that same
  "Carried forward from ..." text** instead of the generic "Adjustment - CARRY_CORRECTION", so
  the new invoice is self-explanatory about where the charge came from without opening anything
  else. It's visible/editable/cancellable through the existing
  `GET/PUT/DELETE /api/feeinvoices/adjustments...` endpoints like any other adjustment.
- **Every contributing older invoice is voided**: `status` flips straight to `6 Cancelled` (even
  if it was already `PartiallyPaid` — this is a system transition, not the user-facing
  `POST /{id}/cancel`, which still refuses to cancel an invoice with payments on it), and it's
  stamped `carriedForwardAmount` (its own `netAmount - paidAmount` at that moment, kept purely for
  display) and **`carriedForwardToInvoiceId`** (the new invoice's id — this is "the reference
  attached to the new fee," readable from either side: open the old invoice and see
  `carriedForwardToInvoiceId`, or open the new invoice and read the `CARRY_CORRECTION` line's
  description). Any of its own separate Applied adjustments (e.g. a one-off "special discount"
  that month) are reverted to Pending first, same as `POST /{id}/cancel` already does — available
  again for a future period instead of lost.
- Being genuinely `Cancelled` means the voided invoice is automatically excluded from every place
  that already filters `Cancelled` out — no separate exclusion logic needed:
  - `GET /api/feeinvoices/statement/{enrollmentId}` and `.../account-statement/{enrollmentId}`
    (the ledger no longer shows the old invoice's debit line at all once voided — its balance
    lives on the new invoice's `CARRY_CORRECTION` line instead; nothing is lost, the voided
    invoice is still readable directly via `GET /api/feeinvoices/{id}`)
  - `GET /api/feeinvoices/students?search=` (search-result `outstandingAmount`)
  - Fee payment allocation/advance-quote/advance-billing math (a voided invoice can no longer be
    paid directly — collect against the invoice that now represents that debt)
  - `GET /api/feeinvoices?status=...` still returns it if you explicitly filter for
    `status=6` (Cancelled) — it isn't deleted, just closed out.
- Regenerating a Draft invoice (`regenerateDrafts: true`) that already carried a balance forward
  does **not** recompute the amount (the invoices that fed it are already voided and gone from
  view) — it just repoints every voided invoice's `carriedForwardToInvoiceId` from the replaced
  Draft's id to its replacement's id, so the reference never dangles.

Nothing about this is a separate UI action — it happens invisibly inside the existing
`POST /api/feeinvoices/generate` call. The visible surface: the new `carriedForwardAmount`/
`carriedForwardToInvoiceId` fields on `FeeInvoiceDto` (0/null for a normal invoice), the voided
invoice showing `status: 6` in the Invoices list, and the `CARRY_CORRECTION` line/adjustment on
the newer invoice.

## 3. Fee adjustments now carry student/class context

`GET /api/feeinvoices/adjustments?...` rows (`FeeAdjustmentDto`) gained `studentName`,
`admissionNo`, `gradeCode`, `sectionCode` — previously a row only had `enrollmentId`, making a
broad query like `?billingYear=2026` (no `enrollmentId` filter) unreadable without a second
lookup per row. No shape change otherwise, and no new endpoints:
`PUT /api/feeinvoices/adjustments/{adjustmentId}` (edit) and
`DELETE /api/feeinvoices/adjustments/{adjustmentId}` (cancel) already exist and already work —
both are Pending-only, same as documented in the main guide's §4 adjustments section.

## 4. Student search default view (`GET /api/feeinvoices/students`)

- **Default page size raised 10 → 20.**
- **With no `search` text**: every enrolled student in scope is returned (a `?academicYearId=`
  call with no `search` is a full roster browse, not just a "who owes money" worklist — fixed
  2026-07-21, it previously dropped every zero-balance student silently), sorted by
  `outstandingAmount` descending (highest first) so the money-owed-first ordering still reads the
  same, it just no longer truncates the list.
- **With `search` text**: behavior is unchanged — matches by name/admission no/email regardless
  of balance (looking up one specific student shouldn't hide them just because they owe nothing),
  sorted alphabetically.

## 5. Multi-status filter on `GET /api/feeinvoices`

`status` now accepts more than one value:

- Repeated key: `?status=4&status=6`
- Comma-separated: `?status=4,6`
- A single `?status=4` still works exactly as before (binds to a one-item list).

Matching invoices are those whose status is any of the supplied values (an OR, not an AND).

## 6. Two generation bugs fixed (no request/response shape change)

- **Regenerating a Draft invoice used to silently drop any adjustment already applied to it** —
  the replacement invoice was built before the reverted-to-Pending adjustment was available,
  so it only reappeared on the *next* generate call. Fixed: the revert now happens before the
  replacement invoice is built, so it's included immediately.
- **`previousDueAmount` (and the Annual-installment "already billed" math) could be skewed by an
  out-of-order invoice** — e.g. a future month already billed via advance payment (§5 of the main
  guide) was being counted as "earlier." Both now correctly consider only invoices whose billing
  period is strictly before the one being generated.

## 7. Failure codes

Same table as the main guide (`Docs/setup_fee_payroll_redesign_implementation_guide.md` §11) —
nothing new. A missing `fee_generation_runs` table or `carried_forward_amount` column surfaces as
a `SERVER_ERROR` (500) until the pending migration is applied.

## 8. Full-name search + split run-detail endpoint (2026-07-21)

- **Full-name search fixed.** The shared student search (`IEnrollmentRepository`'s
  `SearchEnrolledByStudentAsync`/`SearchEnrolledByStudentAllAsync`, backing both
  `GET /api/feeinvoices/students` §4 above and the enrollment search used elsewhere) previously
  matched a search string against `FirstName` OR `LastName` as whole fields — so a two-word query
  like `"Sandhya Adhikari"` matched nothing, since neither field alone contains a space. It now
  splits the search text on whitespace and requires every word to match *something*
  (`FirstName`/`MiddleName`/`LastName`/`AdmissionNo`/`Email`) — so `"Sandhya Adhikari"` matches
  a student named Sandhya Adhikari via one word per name field, while a single-word search like
  `"Adhikari"` still works exactly as before.
- **`GET /api/feeinvoices/students` no longer drops zero-balance students on a no-search call** —
  see §4, updated the same day.
- **`GET /api/feegenerationruns/{id}` no longer returns students/invoices** — split into the
  lightweight detail endpoint plus the new `GET /api/feegenerationruns/{id}/classes/{academicClassId}`
  drill-down, and `draftInvoiceCount` was added at both levels — see §1 for the full shapes.
  Note this run-detail worklist is conceptually **different** from §4's student search: its
  `totalOutstandingAmount` excludes Draft invoices by design (an unfinalized invoice's amount can
  still change and can't be paid against yet — see
  `Docs/setup_fee_payroll_redesign_implementation_guide.md` on Draft vs. finalize), so a class
  whose invoices are still all Draft correctly shows `totalOutstandingAmount: 0` with a non-zero
  `draftInvoiceCount` until finalized via `POST /api/feeinvoices/finalize`. If the goal is "which
  students still owe money right now" across every status, §4's search endpoint (or the
  account-statement endpoint) is the one that answers that, not this one.

## 9. Refresh a run to pick up new adjustments (2026-07-21)

`FeeAdjustment` rows (single or bulk) are only folded into an invoice at generation time
(`FeeInvoiceFactory.BuildInvoice`, called from generation) — creating an adjustment never touches
an already-existing invoice by itself. If a period's invoices already exist when a new adjustment
is created (e.g. a Bulk Adjustment added the day after "Generate" was run), the adjustment sits
`Pending` and inert on the still-Draft invoices until generation re-runs for that exact period.
That was already possible via `POST /api/feeinvoices/generate` with `regenerateDrafts: true`, but
only from the separate Generate Invoices dialog — not from the run detail page where the stale
numbers are actually visible. Two new endpoints close that gap, both thin wrappers around the
existing generate logic (no new invoice-building rules):

| Endpoint | Scope |
|---|---|
| `POST /api/feegenerationruns/{id}/refresh` | every class in the run |
| `POST /api/feegenerationruns/{id}/classes/{academicClassId}/refresh` | one class only |

No request body — both resolve `academicYearId`/`billingYear`/`billingMonth` from the run itself
and always pass `regenerateDrafts: true` internally. Both return the same `FeeGenerationResultDto`
`POST /api/feeinvoices/generate` already returns (`generatedCount`, `skipped: [{ enrollmentId,
studentName, reason }]`) — the same shape the Generate Invoices dialog already knows how to
render. 404s (`NotFound`) if the run doesn't exist, or (class variant) if `academicClassId`
doesn't exist or isn't in the run's own academic year.

Because this reuses `GenerateAsync` unchanged, every existing generation rule still applies:

- **Already-finalized invoices are never touched** — if half a class was finalized before the
  adjustment was created, refresh folds it into the still-Draft half only; the finalized half
  needs a manual per-student adjustment or a line edit instead.
- **Already-`Applied` adjustments on a Draft invoice are reverted-and-remerged before the rebuild**
  (the 2026-07-18 generation-bug fix), so refreshing never silently drops something already on the
  invoice.
- **Carry-forward (`CARRY_CORRECTION`) re-pointing** and the strictly-earlier-period math for
  `previousDueAmount`/Annual installments both carry over unchanged.

New permission rows: `FEE_GENERATION_RUN_REFRESH` (`FeeGenerationRuns.RefreshRun`) and
`FEE_GENERATION_RUN_CLASS_REFRESH` (`FeeGenerationRuns.RefreshRunClass`). No migration — reuses
existing tables. Full design rationale: `Docs/fee_generation_run_refresh_implementation_plan.md`
(superseded by this section now that it's built; kept for the "why," not duplicated here).

**Refresh cannot and does not change an invoice's status** — worth spelling out since it's easy to
misread on the run detail page. `GenerateAsync` skips (never mutates) any invoice whose status
isn't `Draft`; finalizing only ever happens via the separate, explicit `POST
/api/feeinvoices/finalize`. If a class's invoices show mostly `Generated`/`Pending (Overdue)`
right after a refresh, that reflects an *earlier* Finalize click, not something refresh just did —
refresh only ever (a) rebuilds still-Draft invoices with current adjustments/config and (b)
creates new Draft invoices for enrollments that didn't have one yet for that period (the "fills
gaps" behavior). **2026-07-21 addendum**: since an already-finalized invoice is therefore
permanently out of refresh's reach, `POST /api/feeinvoices/{id}/unfinalize` (see the main guide's
endpoint table) is the way back to Draft for one invoice at a time, so it becomes eligible for the
next refresh — blocked once the invoice has a payment against it, same as `cancel`.
