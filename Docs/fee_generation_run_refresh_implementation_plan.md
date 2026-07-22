# Fee Generation Run — Refresh (implementation plan)

**Status: implemented (2026-07-21), backend + frontend.** Built as designed below — §§4–7
(endpoints, service sketch, permissions, edge cases) shipped exactly as written; §8's frontend
wiring landed separately. The shipped reference is now
`Docs/fee_generation_run_and_carry_forward_implementation_guide.md` §9 and the corresponding
`UI-Implementation-Guide.md` bullet — this file is kept as the design rationale ("why a thin
wrapper, why two scopes"), not duplicated there.

## 1. The problem

Reported 2026-07-21 against a live run: a `Bulk Adjustment` (`Educational Tour Fee`, Rs 2,000
`Charge`) was created for every Nursery enrollment via `POST /api/feeinvoices/adjustments/bulk`
(Monthly Adjustments tab). The new adjustment rows show up correctly as `Pending` in the
adjustments list. But the Fee Generation Run detail page for that same period
(`/apps/fee-invoice/generation-run/{id}` → Nursery class drill-down) still shows every student's
original invoice amounts (e.g. Nabin Adhikari: Net Rs 21,083.33, unchanged) — the Rs 2,000 charge
never appears on the invoice.

## 2. Root cause (confirmed by reading the current code, not guessed)

`FeeAdjustment` rows are **not** folded into a `FeeInvoice` at creation time. They're folded in
exactly one place: `FeeInvoiceFactory.BuildInvoice`, called from
`FeeInvoiceService.GenerateAsync` (`Application/FeeInvoices/FeeInvoiceService.cs:53`), which only
touches an enrollment's invoice for a given billing period if:

- there's no invoice yet for that period (normal generation), or
- there is one, it's still `Draft`, **and** the caller passes `RegenerateDrafts = true`.

Creating a `FeeAdjustment` (single or bulk) only ever inserts a `Pending` adjustment row — it
never re-runs generation. So a bulk adjustment created *after* a period's invoices already exist
sits `Pending` and inert until someone calls `POST /api/feeinvoices/generate` again for that exact
`(academicYearId, billingYear, billingMonth)` with `regenerateDrafts: true`. That capability
already exists and already works (`GenerateInvoicesModal.jsx` has a "Regenerate Drafts" checkbox
on the Generation Runs tab) — the gap is purely that **the Generation Run detail page has no way
to trigger it**. The admin has to know to go back to the Generation Runs tab, re-enter the same
academic year/billing period/class, tick "Regenerate Drafts", and re-submit — a completely
disconnected flow from the page where they're actually looking at the stale numbers.

This is the same shape of problem `PayrollRun` already solved on the payroll side: **§ "Payroll
fixes & salary calculator (2026-07-18)"** in `CLAUDE.md` describes `POST
/api/payrollruns/{id}/refresh` — a Draft-only, in-place regeneration that "picks up
compensation-plan edits, tax-slab fixes, newly Pending adjustments, and newly approved loans made
after generation." This plan proposes the direct fee-side analog.

## 3. What "refresh" means here, precisely

Refreshing a Fee Generation Run = calling the **existing** `FeeInvoiceService.GenerateAsync` logic
again for that run's own `(AcademicYearId, BillingYear, BillingMonth)`, with `RegenerateDrafts =
true` and no new invoice-building logic. `GenerateAsync` already:

- only rebuilds invoices still in `Draft` (an already-`Generated`/`Pending`/`PartiallyPaid`/`Paid`
  invoice is left untouched, reported as skipped with a reason — see
  `Application/FeeInvoices/FeeInvoiceService.cs:280`);
- reverts that Draft's already-`Applied` adjustments back to `Pending` and re-merges them
  *before* rebuilding, so nothing already folded in is silently dropped (the exact bug fixed
  2026-07-18, documented in `CLAUDE.md`'s Fee generation run & carry-forward section);
- picks up any *other* `Pending` adjustment for that period too — which is exactly the newly
  created bulk-adjustment row from §1;
- creates a Draft invoice for any newly eligible enrollment that didn't have one yet (a new
  admission after the initial generate) — same "re-running a month only fills gaps" behavior the
  Fee Generation page's own subtitle already advertises;
- stamps `feeGenerationRun.LastRegeneratedTs` (line 143) — already wired, no extra work.

So this plan is **not** proposing new invoice-building rules. It's proposing a thin, discoverable
entry point into logic that already exists, reachable from the page where the stale data is
visible — mirroring `PayrollRunService.RefreshRunAsync`.

## 4. Proposed endpoints

Two, mirroring the two "Finalize Drafts" scopes the Generation Run detail page already has (run
header banner, and each class's own expanded-accordion banner — see
`ClientUI/src/sections/apps/fee-invoice/FeeGenerationRunDetail.jsx`):

| Endpoint | Scope | Notes |
|---|---|---|
| `POST /api/feegenerationruns/{id}/refresh` | every class in the run | run-level "Refresh" button, next to the existing run-level "Finalize Drafts (N)" |
| `POST /api/feegenerationruns/{id}/classes/{academicClassId}/refresh` | one class only | per-class "Refresh" button, next to that class's own "Finalize Drafts (N)" |

Both take **no request body** — everything they need (`AcademicYearId`/`BillingYear`/
`BillingMonth`) comes from the run itself (`IFeeGenerationRunRepository.GetByIdWithYearAsync`,
already used by the existing detail endpoints), and `RegenerateDrafts` is always `true` (a refresh
that didn't regenerate drafts wouldn't be a refresh). The per-class variant additionally validates
`academicClassId` belongs to the run's academic year, same check
`FeeGenerationRunService.GetFeeGenerationRunClassDetailAsync` already does (2026-07-21 split).

Both return the existing `FeeGenerationResultDto` (`Application/FeeInvoices/Dtos/`, already
returned by `POST /api/feeinvoices/generate`) — no new DTO needed. The frontend already knows how
to render this shape (`GenerateInvoicesModal.jsx`'s result screen: generated count + a skipped
list with per-enrollment reasons).

## 5. Service implementation sketch

`FeeGenerationRunService` gains one new dependency, `IFeeInvoiceService` — precedent for one
Application service injecting another feature service already exists
(`SalaryCalculatorService` → `IEmployeeService`, per `CLAUDE.md`'s Payroll fixes section). No new
business logic is written; the new methods just assemble a `GenerateFeeInvoicesCommand` from the
already-loaded run and delegate:

```csharp
public async Task<CommonResponse<FeeGenerationResultDto>> RefreshRunAsync(Guid id, CancellationToken cancellationToken = default)
{
    var run = await _unitOfWork.FeeGenerationRuns.GetByIdWithYearAsync(id, cancellationToken);
    if (run == null)
    {
        return CommonResponse<FeeGenerationResultDto>.Fail(ResponseCodes.NotFound, "...");
    }

    var command = new GenerateFeeInvoicesCommand
    {
        AcademicYearId = run.AcademicYearId,
        BillingYear = run.BillingYear,
        BillingMonth = run.BillingMonth,
        RegenerateDrafts = true
    };

    return await _feeInvoiceService.GenerateAsync(command, cancellationToken);
}

public async Task<CommonResponse<FeeGenerationResultDto>> RefreshRunClassAsync(Guid id, Guid academicClassId, CancellationToken cancellationToken = default)
{
    // same run lookup + academicClassId/academic-year cross-check as
    // GetFeeGenerationRunClassDetailAsync, then the same command with
    // AcademicClassId = academicClassId set.
}
```

`IFeeGenerationRunService`/`FeeGenerationRunsController` gain the two matching methods/actions,
same 404-on-missing-run shape as the existing `GetFeeGenerationRunByIdAsync`/
`GetFeeGenerationRunClassDetailAsync`.

## 6. Permissions

Two new seeded rows in `MenuSeeder.BuildMenuCatalog()`, alongside the existing
`FEE_GENERATION_RUN_LIST`/`FEE_GENERATION_RUN_DETAIL`/`FEE_GENERATION_RUN_CLASS_DETAIL` block
(`Infrastructure/Persistence/DataSeeder/MenuSeeder.cs`, currently around line 275):

```csharp
catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_GENERATION_RUN_REFRESH", "Refresh Fee Generation Run", "FeeGenerationRuns", "RefreshRun", 28));
catalog.Add(Permission("FEE_INVOICE_LIST", "FEE_GENERATION_RUN_CLASS_REFRESH", "Refresh Fee Generation Run Class", "FeeGenerationRuns", "RefreshRunClass", 29));
```

(Order numbers continue from the existing `27` used by `FEE_GENERATION_RUN_CLASS_DETAIL`.)

## 7. Edge cases / behavior to carry over from `GenerateAsync` unchanged

These are already handled by the logic being reused — called out here only so the refresh
endpoints aren't accidentally built as a fresh reimplementation that reintroduces a bug already
fixed once:

- **Already-finalized invoices are never touched.** If half of Nursery was finalized before the
  bulk adjustment was created, refresh will fold the Rs 2,000 charge into the still-Draft half
  only; the finalized half needs a manual per-student `FeeAdjustment` or a `POST
  /api/feeinvoices/{id}/lines` edit instead (same limitation `PayrollRun`'s refresh has for
  already-approved slips). Worth surfacing in the refresh result/snackbar so the admin isn't
  surprised the "before" and "after" invoice counts don't match the adjustment's original scope.
- **Applied adjustments already on a Draft invoice must be reverted-and-remerged before the
  rebuild, not after** — this is the exact fix from `CLAUDE.md`'s "Two pre-existing generation
  bugs fixed" bullet (2026-07-18). Since refresh calls the same `GenerateAsync`, this is automatic
  — just don't build a parallel code path that skips it.
- **`previousDueAmount`/Annual-installment math only considers strictly-earlier periods** — same
  reasoning, automatic via reuse.
- **Carry-forward (`CARRY_CORRECTION`) re-pointing** — if this period's Draft already carried a
  balance forward from an earlier voided invoice, refresh must not double-bill it; this is
  `FeeInvoiceService.EnsureCarryForwardAdjustmentAsync`'s existing already-revived-adjustment
  detection, automatic via reuse.

## 8. Frontend wiring (not built yet — described for the next session)

- `api/fee-generation-run-service.js`: two new plain async functions, `refreshFeeGenerationRun(id)`
  and `refreshFeeGenerationRunClass(id, academicClassId)`, POSTing to the two new endpoints and
  returning the response (same shape as the existing `generateFeeInvoices` helper in
  `fee-invoice-service.js`) — then calling `mutate` on both the run-detail key
  (`endpoints.key + endpoints.byId(id)`) and the affected class-group key(s) so the page reflects
  the refreshed numbers immediately instead of waiting for the next natural revalidation.
- `FeeGenerationRunDetail.jsx`:
  - A "Refresh" `Button` (icon: `ReloadOutlined` or similar) next to the existing run-level
    "Finalize Drafts (N)" button (around line 192 today) — calls `refreshFeeGenerationRun(runId)`,
    shows a snackbar with `generatedCount`/skipped summary, then re-fetches
    `useGetFeeGenerationRunById` and every currently-expanded class's
    `useGetFeeGenerationClassGroup`.
  - A matching "Refresh" button inside each class accordion, next to that class's own "Finalize
    Drafts (N)" (around line 430 today) — calls `refreshFeeGenerationRunClass(runId,
    academicClassId)`, same snackbar/refetch pattern scoped to just that class.
  - Disable state while in flight (`refreshingRun`/`refreshingClassId` local state), same pattern
    `finalizingDrafts` already uses.

## 9. Migration

None. No new tables/columns — this reuses `FeeInvoice`/`FeeAdjustment`/`FeeGenerationRun` exactly
as they exist today.

## 10. Out of scope for this plan

- Automatically refreshing a run the moment a bulk adjustment is created (i.e. making adjustment
  creation itself trigger regeneration) — rejected as a default: a Pending adjustment is meant to
  be reviewable before it lands on an invoice (the Monthly Adjustments tab's whole point), and
  auto-regenerating on every adjustment create would silently rebuild Draft invoices an admin
  might be mid-edit on. An explicit Refresh action keeps that a deliberate step, same as
  `PayrollRun`'s refresh being manual, not automatic.
- Touching non-Draft invoices at all (would require a real edit/adjustment flow on a locked
  invoice, out of scope here).
