# Salary structure entry & Payroll Run slip adjustments — implementation guide

Two related UI surfaces, both about entering compensation data, both now driven by backend
Config catalogs instead of frontend-hardcoded assumptions:

1. **Add Salary Revision** (Employee/Teacher profile → Compensation Plan → "+ Add Revision") —
   creates a whole new `EmployeeSalary` revision from scratch.
2. **Slip Lines** (Payroll Run → slip detail → "Add, edit or remove manual Earning/Deduction
   lines while this run is Draft") — ad-hoc adjustments (Bonus, Overtime, an extra deduction,
   ...) on one already-generated month's slip.

---

## 1. Add Salary Revision

```
POST /api/employees/{id}/salaries          permission: implied by EMPLOYEE_UPDATE-family grants
POST /api/teachers/{id}/salaries           (alias, same shape)
```

Body (`AddEmployeeSalaryCommand`):

```jsonc
{
  "effectiveFromDate": "2026-07-17",
  "assessmentType": 1,                 // 1 Individual | 2 Couple
  "components": [
    { "componentCode": "BASIC", "valueType": 2, "value": 18000, "frequencyType": 1, "isTaxable": true, "isRetirementContribution": false }
  ],
  "deductions": [
    { "deductionCode": "SSF_DEDUCTION", "valueType": 1, "value": 11, "frequencyType": 1, "isRetirementContribution": true }
  ],
  "insurancePremiums": []
}
```

### Where the fields come from (all catalog-driven — nothing hardcoded in the frontend)

- **Component/Deduction code dropdowns**: `GET /api/configs/dropdown/1013` (components) /
  `/1014` (deductions). Each `DropdownItemDto` row carries `additionalValue1`/`2`/`3` alongside
  `value`/`label` — **this is the whole point of this rewrite**: the dropdown response itself
  tells the form how to behave for that code (2026-07-22, see
  `Docs/UI-Implementation-Guide.md`'s "Catalog-locked percentage components/deductions" entry
  and `CLAUDE.md`'s payroll-fixes Round 8 for the backend side).
- **Insurance type dropdown**: `GET /api/configs/dropdown/1015` — unchanged, still just
  `Life`/`Health`/`Housing`/`Education`, each with its own cap (`additionalValue1`) and eligible
  percentage (`additionalValue2`, from the 2026-07-22 Children's Education work).

### `additionalValue1` format (updated 2026-07-23)

As of 2026-07-23, `additionalValue1` on a `/1013` (Salary Component Type), `/1014` (Deduction
Type), or `/1016` (Salary Adjustment Type) dropdown row is a composite, pipe-delimited value:

```
CALCULATE_TYPE|TYPE|FREQUENCY
```

e.g. `"ADDITION|FIXED|MONTHLY"` or `"ADDITION|PERCENTAGE|MONTHLY"` (`SSF_CONTRIBUTION`). This
replaces two narrower single-value conventions in one move: 1013/1014's old bare
`"PERCENTAGE"`/`"FIXED"` value, and 1016's old bare `"EARNING"`/`"DEDUCTION"` suggested-direction
value — every row in all three catalogs now carries the full triple (a row with no
`additionalValue1` at all, or one that doesn't parse as exactly three `|`-separated segments,
means "no catalog rule for this code," i.e. fully free-form entry, same fallback as before).

- **CALCULATE_TYPE** (`ADDITION`/`DEDUCTION`): on 1013/1014 this is inherent to the catalog (every
  Salary Component Type row is `ADDITION`, every Deduction Type row is `DEDUCTION`) — it's a
  read-only fact, not something the form needs to act on differently than before. On 1016 it
  **replaces the old direction hint** and is now server-enforced against the adjustment's
  `direction` field (`ADDITION` requires `direction: 1` / Increase, `DEDUCTION` requires
  `direction: 2` / Decrease) — see §4.
- **TYPE** (`FIXED`/`PERCENTAGE`): unchanged meaning, see the percentage-lock behavior below —
  just read as the *second* segment now instead of the whole value.
- **FREQUENCY** (`MONTHLY`/`ONE_TIME`): new. When present on a 1013/1014 row, the submitted line's
  `frequencyType` must match it exactly (`1` Monthly / `3` OneTime) — **prefill and disable the
  Frequency field** for a code whose catalog locks it, same UX treatment as the percentage lock
  below. A code with no locked frequency keeps free `frequencyType` entry (including `2` Annual,
  which no catalog row locks to today).

### Percentage-locked codes (`TYPE == "PERCENTAGE"`)

When the admin picks a code from the Component/Deduction dropdown whose `additionalValue1`'s
middle segment is `PERCENTAGE` (e.g. `"ADDITION|PERCENTAGE|MONTHLY"`):

- **Force `valueType` to Percentage** and **prefill `value` from `additionalValue2`** — then
  **disable the Value input** (or make it read-only) for that row. The backend now rejects any
  other value for this code with `400 VALIDATION_ERROR` (`'SSF_DEDUCTION' is locked to
  Percentage, 11% (of BASIC) by its catalog configuration...`), so letting the admin type
  something else just produces a round-trip failure — better to make it structurally
  impossible in the form itself.
- **Show "% of `<label of additionalValue3>`"** next to the field (e.g. "20% of Basic Salary")
  — resolve `additionalValue3`'s code (default `"BASIC"` if blank) against the same
  component-type dropdown's labels for display.
- A code with no catalog rule at all (blank/unparseable `additionalValue1`), or whose `TYPE`
  segment is `FIXED`, is **completely unaffected** for `valueType`/`value` — free entry exactly as
  before this feature existed. `frequencyType` is separately locked or free per the `FREQUENCY`
  segment described above, independent of whether `TYPE` is locked.

### "+ Add SSF (20% Employer / 11% Employee)" quick-add button

Don't hardcode "20%"/"11%" as frontend literals — read them **live** from the same dropdown
responses used above: find `SSF_CONTRIBUTION` (1013) and `SSF_DEDUCTION` (1014) rows,
read their `additionalValue2` for the button's label and the values it inserts. This way, if an
admin ever changes the statutory rate via `PUT /api/configs/{id}` (a real Finance-Act scenario —
SSF rates have changed before), the button updates itself with zero frontend deploy. The button
inserts one component (`SSF_CONTRIBUTION`, Percentage, rate from catalog, `isTaxable: true`,
`isRetirementContribution: true`) and one deduction (`SSF_DEDUCTION`, Percentage, rate from
catalog, `isRetirementContribution: true`) in one click — the correct, non-double-counted shape
(see `Docs/payroll_fixes_implementation_guide.md` for why this exact shape matters).

### "Live Gross (Monthly)" preview

Recompute on every field change, client-side, before the admin ever submits:

1. Find the `BASIC` component's period `value` (Fixed-valued; if the admin hasn't added one yet,
   treat as 0 and show the preview as incomplete).
2. For each **Monthly**-frequency component: if `valueType` is Fixed, its amount is `value`
   as-is. If Percentage, **resolve the base per the row's own catalog `additionalValue3`** (not
   always Basic!) — look up that component's own Fixed value in the current form state, default
   to the `BASIC` amount from step 1 if `additionalValue3` is blank or unresolvable, then
   `amount = base * (value / 100)`.
3. Sum every Monthly component's resolved amount → that's "Live Gross (Monthly)". (Annual/OneTime
   components — a festival bonus — are deliberately excluded from this monthly figure, same
   convention the backend's own `grossMonthly` uses for the *taxable* annual-income view; this
   preview is answering "what recurs every month," not "what's the annualized average.")

This mirrors `TaxCalculator.ResolvePercentageBaseAmount` server-side exactly — the whole reason
the backend generalized away from a hardcoded Basic-only resolution was so a frontend preview
like this one can ask the *same* dropdown metadata "what does this percentage resolve against"
instead of assuming.

### Failure the admin will see if they bypass the form and craft their own request

| Condition | `responseCode` | Message shape |
|---|---|---|
| Code not in the catalog at all | `ValidationError` | `"ComponentCode 'X' is not a known salary component option."` |
| Code is percentage-locked, submitted as Fixed or wrong rate | `ValidationError` | `"'X' is locked to Percentage, N% (of BASE) by its catalog configuration -- it cannot be entered as a different value or as a Fixed amount."` |
| Code carries a locked FREQUENCY, submitted with a different `frequencyType` (2026-07-23) | `ValidationError` | `"'X' is locked to Monthly frequency by its catalog configuration."` |
| Code seeded under the wrong catalog (defensive, shouldn't happen from normal use) (2026-07-23) | `ValidationError` | `"'X' is catalogued as DEDUCTION, not ADDITION -- check its catalog configuration."` |

---

## 2. Slip Line adjustments (Payroll Run → Draft slip)

```
POST   /api/payrollruns/{runId}/slips/{slipId}/lines            add a manual line
PUT    /api/payrollruns/{runId}/slips/{slipId}/lines/{lineId}    edit description/amount
DELETE /api/payrollruns/{runId}/slips/{slipId}/lines/{lineId}    remove
```

All three require the slip to still be `Draft` (`409 CONFLICT` otherwise, same as every other
Draft-only mutation in this system).

### What changed (2026-07-22) — tax now actually recalculates

**Before**: adding/editing/removing a manual Earning or Deduction line updated
`GrossEarnings`/`TotalDeductions`/`NetPay` (a plain re-sum of whatever lines existed), but the
`TDS (income tax)` line itself was frozen at whatever the run's original generation computed —
so `NetPay` silently ignored the tax impact of the change. Reported symptom: adding a Festival
Bonus as a manual line left `NetPay` overstated by the bonus's marginal tax.

**Now**: every add/update/remove of a manual line re-runs the exact same `TaxCalculator`
pipeline generation/refresh already use (reading the employee's **current** salary structure,
the run's fiscal year, and its tax slabs) and overwrites the slip's `TDS` line with the freshly
computed `MonthlyTax` before recomputing the aggregate totals. No UI change needed for this part
— it's transparent; the returned `SalarySlipDto` after any of the three calls already reflects
the corrected tax and `NetPay`.

### What else changed — manual lines now sync into the real compensation plan

Per instruction: a manual **Earning** or **Deduction** line that names a real
`componentCode`/`deductionCode` (the "Code" dropdown in the Slip Lines add-row, backed by the
same `/1013`/`/1014` dropdowns as §1) is no longer slip-only — it's folded into the employee's
actual `EmployeeSalaryComponent`/`EmployeeSalaryDeduction` rows too:

- **Add**: if a component/deduction with that code already exists on the salary revision this
  slip snapshotted, its `Value` is **increased by the manual amount**. If the code doesn't exist
  yet, a **new Fixed, OneTime** component/deduction row is inserted with that amount.
  Percentage-locked codes (SSF) and Percentage-valued existing rows reject the add outright —
  same validation as §1, since merging a raw cash amount into a percentage doesn't mean
  anything.
- **Edit**: best-effort — the *change* in amount (`newAmount - oldAmount`) is applied as a
  further delta to whatever structure row the code matches. This is matched by code, not by a
  stored link back to the exact row the original add touched (no schema change for this — see
  Known gaps below), so it's a reasonable approximation, not a guaranteed-exact mirror.
- **Remove**: best-effort — the line's full amount is subtracted back out of the matching
  structure row (clamped to a minimum of 0, never negative; a row that nets to exactly 0 is left
  in place as a real, harmless zero-value line rather than being deleted — an admin can remove
  it from the Compensation Plan tab directly if they don't want it lingering).
- A line with **no code selected** (a free-text description only) is **never** synced to the
  structure — it stays a pure slip-only annotation, same as before this feature.

### Double-counting bug found and fixed (2026-07-22, same day)

The first cut of this feature marked a synced line's `Source` as `Manual` (like any other manual
line), which is what `RebuildSlip`/`Refresh` is documented to always preserve untouched. That was
wrong: since the sync had *also* just written the amount into the real structure, a subsequent
**Refresh of this same run** would regenerate a fresh `SalaryStructure` line reflecting the
now-larger structure value **in addition to** the surviving `Manual` line — the same amount
counted twice. This is almost certainly what "refresh is not working" was reporting. Verified
concretely: whenever the salary's own `EffectiveFromDate` falls in the same fiscal month as the
slip being edited (a common case — e.g. a new hire's first month), the newly-inserted `OneTime`
component lands right back on this exact slip on the next regeneration.

**Fix, part 1**: a successfully-synced line is now created with `Source = SalaryStructure` (not
`Manual`) immediately. The next Refresh/Regenerate then correctly replaces it with the one true
regenerated line instead of keeping both. Practical consequence: once a manual line syncs
successfully, treat further changes to *that specific amount* through the Compensation Plan tab
(§1), not by re-editing the slip line — editing/removing a `SalaryStructure`-sourced line via the
Slip Lines UI is (like any other structure-sourced line, always was) only a **temporary,
this-slip-only override** that a future refresh/regenerate will discard in favor of the real
structure value. A line that failed to sync (unknown code, percentage-locked, or no code given
at all) still stays `Manual` and still behaves exactly as documented above.

**Fix, part 2 (2026-07-22, same day)**: part 1 alone only prevents the double-count for lines
added *after* the fix shipped — a `Manual` line created before it (still labeled `Manual` in the
database, its amount already permanently in the structure from the old code path) still
duplicated on Regenerate, exactly reproduced with a real Festival Bonus: "Manual, 26,821.92" and
"Salary Structure, 26,821.92" both showing on the same slip. `PopulateSlipLines` (used by both
`BuildSlip` and `RebuildSlip`/Regenerate) now checks, for every regenerated Salary Structure
Earning/Deduction line, whether a **surviving line with that exact `ComponentCode` already
exists** on the slip — if so, that line is **updated in place** (new amount, label, and `Source`
promoted to `SalaryStructure`) instead of a second row being added. This is a general "at most
one row per code per slip" guarantee, not limited to lines this feature created — it retroactively
self-heals any legacy `Manual` line left over from before part 1, the moment that slip is next
rebuilt (Refresh or Regenerate).

### UI implication: the "Code" field in the Slip Lines add-row is no longer cosmetic

Before this change, leaving "Code" blank and just typing a Description/Amount was a perfectly
normal way to add a one-off line. **Now it matters**: picking a real code is what makes the
addition permanent (and what makes tax recalculate against the updated annual figure); leaving
it blank keeps the old cosmetic-only behavior. Consider labeling the field something like "Code
(optional — leave blank for a one-off note, pick one to also update the compensation plan)" so
this distinction is visible instead of surprising.

### Known gaps (flagged honestly, not hidden)

- **A brand-new component/deduction inserted this way follows the code's catalog-locked
  FREQUENCY when it has one (2026-07-23), otherwise falls back to `OneTime`** — e.g. syncing a
  manual `COMMUNICATION_ALLOWANCE` line (catalog-locked `MONTHLY`) now inserts a recurring Monthly
  structure row, not a one-off; a code with no locked frequency (or that doesn't resolve to a
  1013/1014 code at all) keeps the old `OneTime` behavior, which (per `MonthlyBreakdownCalculator`'s
  existing rule) only lands in the fiscal month containing the salary revision's own
  `EffectiveFromDate` unless *other* months are generated/refreshed later. If you want a code that
  isn't catalog-locked to Monthly to recur anyway, add it from the Compensation Plan tab (§1)
  instead, with `frequencyType: Monthly` explicitly.
- **No FK links a `SalarySlipLine` back to the exact structure row it created/adjusted** — Edit
  and Remove match by code, which is right the overwhelming majority of the time but can drift
  if, say, two different manual lines used the same code on the same slip, or someone edited the
  matching structure component directly (via the Compensation Plan tab) in between. Adding a
  proper link would need a new nullable FK column + migration — not done here since migrations
  are user-owned; flag if this proves to be a real problem in practice and it's a small
  follow-up.
- **Deleting the whole `EmployeeSalary` revision** this slip snapshotted (`slip.EmployeeSalaryId`)
  would break both features silently — not expected to happen in normal use (revisions are
  append-only history), so no special-cased guard was added.

---

## 3. Individual slip Approve and Regenerate (2026-07-22, new)

```
POST /api/payrollruns/{runId}/slips/{slipId}/approve       Draft slip -> Approved
POST /api/payrollruns/{runId}/slips/{slipId}/regenerate    Cancelled or Draft slip -> rebuilt, Draft
```

Both are **per-slip**, independent of the whole run's own bulk actions (`POST .../approve`,
`POST .../refresh`) — a UI action button per row in the Salary Slips table, alongside the
existing per-row Cancel and the view (eye) icon.

### Approve

Locks just that one slip (`Draft → Approved`) without touching the run's status or any other
slip. `409 CONFLICT` if the slip isn't currently `Draft`. Once Approved, every Slip Line
add/edit/remove call already rejects it (same "only Draft slips can be edited" guard every other
mutation uses) — approving is what locks the lines, same effect as the bulk run-level approve,
just scoped to one employee. The bulk "Approve Run" button remains the way to lock everyone at
once; this is a finer-grained option, not a replacement.

### Regenerate

The deliberate, admin-triggered counterpart to the existing per-slip Cancel. Fully rebuilds **one
slip** from the current compensation plan and tax configuration — same pipeline
`CreateRunAsync`/`RefreshRunAsync` use (fresh `TaxCalculator` + `MonthlyBreakdownCalculator` run,
fresh lines, fresh totals) — and flips the slip back to `Draft` in the process. Works on:

- A **Cancelled** slip — "actually, un-cancel and rebuild this one." This is intentionally **not**
  automatic during a whole-run Refresh: `RefreshRunAsync` still leaves an individually-cancelled
  slip cancelled by design (it may represent a real leaver — silently reviving their pay on every
  refresh would be dangerous). Regenerating one specific slip is an explicit, deliberate action an
  admin opts into here instead.
- An already-**Draft** slip — equivalent to refreshing just this one employee instead of the
  whole run.

`409 CONFLICT` for `Approved`/`Paid` slips (regenerate never unlocks something already signed
off). If the employee is no longer payroll-eligible (the same check `CreateRunAsync`/
`RefreshRunAsync` use), regeneration correctly refuses with `400 VALIDATION_ERROR` rather than
silently reviving pay for someone who shouldn't be paid — this is what protects the "leaver"
cancellation case even though regenerate *can* target a Cancelled slip.

## 4. Composite catalog format, CalculateType enforcement & the unified Add Salary Line endpoint (2026-07-23)

Three related changes, all driven by the `CALCULATE_TYPE|TYPE|FREQUENCY` format introduced above:

### Salary Adjustment Type direction is now server-enforced

`POST /api/employees/{id}/adjustments`, `PUT .../adjustments/{adjustmentId}`, and
`POST /api/employees/adjustments/bulk` now validate the submitted `direction` against the
`/1016` catalog row's `CALCULATE_TYPE` segment: a `LATE_FINE` adjustment (catalogued `DEDUCTION`)
submitted with `direction: 1` (Increase) is rejected with `400 ValidationError`
(`"'LATE_FINE' is catalogued as DEDUCTION, not ADDITION -- check its catalog configuration."`).
`OTHER` is seeded with a blank `additionalValue1` specifically so it keeps accepting either
direction. **UI implication**: when the admin picks an adjustment type, prefill/lock the
Direction toggle from that row's `additionalValue1` the same way §1 already prefills/locks
`valueType` for a percentage-locked component — don't let the admin pick a direction the type
catalog disagrees with, the round-trip will fail.

### Unified "Add Salary Line" endpoint

```
POST   /api/employees/{id}/salaries/{salaryId}/lines             add, code-driven
DELETE /api/employees/{id}/salaries/{salaryId}/lines/{lineId}     remove, code-driven
```

A code-driven alternative to the separate `/components`/`/deductions` endpoints from §1 — body is
just `{ code, valueType, value, frequencyType, isTaxable, isRetirementContribution }` (no
`componentCode`/`deductionCode` split; `isTaxable` is ignored when `code` resolves to a
deduction). The service resolves `code` against `/1013` first, then `/1014`, and dispatches to
whichever table it belongs to — same validation either way (catalog existence, percentage lock,
`CalculateType`, frequency lock). Response is the unified `SalaryLineDto` shape: `id`,
`employeeSalaryId`, `code`, `label`, `calculateType` (`"ADDITION"`/`"DEDUCTION"`), `valueType`,
`value`, `frequencyType`, `isTaxable` (`null` for a deduction), `isRetirementContribution`.
`DELETE .../lines/{lineId}` tries the component table, then the deduction table, and 404s if
neither matches. **The original `/components`/`/deductions` endpoints from §1 are unchanged and
not deprecated** — this is an additional entry point for a UI that would rather not ask the admin
which catalog a code came from, not a replacement.

Permissions: `EMPLOYEE_SALARY_LINE_ADD`, `EMPLOYEE_SALARY_LINE_REMOVE` (`EMPLOYEE_LIST` submenu,
same as every other Employees permission).

## Where this lives in the codebase

- `Application/PayrollRuns/PayrollRunService.cs`: `AddSlipLineAsync`/`UpdateSlipLineAsync`/
  `RemoveSlipLineAsync` (public entry points), `ApplyDeltaToSalaryStructureAsync` (structure
  upsert/reversal, now also frequency-resolving via `SalaryLineCalculationHelper.ResolveFrequency`),
  `RecalculateSlipTaxAsync` (re-runs `TaxCalculator.CalculateFromSalary` and rewrites the slip's
  `TDS` line), `ApproveSlipAsync`/`RegenerateSlipAsync` (§3).
- `Application/Employees/EmployeeService.cs`: `AddSalaryLineAsync`/`RemoveSalaryLineAsync` (§4),
  and the `CalculateType` check added to `CreateSalaryAdjustmentAsync`/`UpdateSalaryAdjustmentAsync`/
  `CreateBulkSalaryAdjustmentsAsync`.
- `Application/Common/Helpers/SalaryLineCalculationHelper.cs` / `Application/Payroll/Dtos/
  SalaryLineCalculationConfig.cs` / `Domain/Constants/SalaryLineCalculationModes.cs` +
  `SalaryLineCalculateTypes.cs` + `SalaryLineFrequencyCodes.cs`: the parser and the three
  structural guards (`ValidatePercentageLock`/`ValidateCalculateType`/`ValidateFrequencyLock`)
  shared by §1 (entry validation), §2 (structure sync), and §4 (adjustment direction, unified
  add-line endpoint).
- New permission rows: `SALARY_SLIP_APPROVE`, `SALARY_SLIP_REGENERATE`, `EMPLOYEE_SALARY_LINE_ADD`,
  `EMPLOYEE_SALARY_LINE_REMOVE`.
- **Catalog migration note**: `ConfigCatalogSeeder` is create-if-missing, same caveat as every
  other catalog change in this project — a fresh database seeds the new composite
  `additionalValue1` format automatically; an **already-seeded database's 1013/1014/1016 rows
  keep their old single-value format** (still parsed correctly as "no rule" by the new parser,
  since it doesn't match the 3-segment shape) until each row is updated via
  `PUT /api/configs/{id}`. Until that happens, the new CalculateType/frequency enforcement simply
  doesn't fire for those rows (falls back to fully free-form entry) — nothing breaks, the new
  guards just aren't active yet on unmigrated data.
