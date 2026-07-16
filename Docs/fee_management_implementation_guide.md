# Fee, Discount & Scholarship Management (2026-07-15, redesigned three times same day)

Class-wise fee structure (a **header per class** owning a **child table of categorized line
items** — redesigned from the earlier one-row-per-category shape, then briefly to free-text names,
then back to validated categories, see "Design history" below), per-enrollment discounts and
scholarships (each with a two-tier global-default-or-individual-override rate), and a computed
per-enrollment fee-structure API for the student detail page. Follows every convention in
`UI-Implementation-Guide.md` (envelope, camelCase, Bearer token, permission-gated). **Needs a
migration** — see the note at the bottom.

## Design history

1. **Original**: `FeeStructure` was one row per `(AcademicClass, FeeCategoryCode)`, forcing an admin
   to submit the create API **once per category** to set up a class's full fee list (11 separate
   calls for a full set).
2. **First redesign (same day)**: split into a **header** (`FeeStructure`, one row per class) owning
   a **child table** (`FeeStructureItem`) so the whole list could be created in one call — but that
   pass also dropped the category code entirely in favor of a free-text `name`, on the theory that
   an admin should be able to call a fee whatever their institution wants.
3. **Second redesign (same day, current)**: free-text `name` was reverted — `FeeStructureItem` now
   carries `feeCategoryCode` again, **validated against the `FeeCategory` Config catalog**
   (`typeCode 1010`), so only known, admin-approved categories can be charged. The header+items
   shape (and the one-call bulk create it enables) is kept — only the "what identifies a line item"
   question changed back. If a school needs a category that isn't in the catalog yet, add it via
   `POST /api/configs` first (see "Global config layer" below), then reference its code on the item.

## Global config layer: `FeeCategory` catalog (`typeCode 1010`)

The 11 "permitted" categories are a Config catalog (`ConfigTypeCodes.FeeCategory = 1010`) — this is
now the **authoritative whitelist** every `FeeStructureItem.FeeCategoryCode` is validated against
(`IUnitOfWork.Configs.CodeExistsAsync(ConfigTypeCodes.FeeCategory, code)`), not just a suggestions
list. Each option also carries a suggested `frequencyType`/`isOptional`/`isRefundable` in
`additionalValue1`/`additionalValue2`/`additionalValue3` (same reuse of Config's free-form slots as
the Discount/ScholarshipType default-rate convention) — a UI can use these to prefill a new item's
fields once the admin picks a category, but the item still stores its own values (they can diverge
from the suggestion, e.g. one school bills Computer Fee annually instead of monthly):

| Code | Label | `additionalValue1` (frequency) | `additionalValue2` (isOptional) | `additionalValue3` (isRefundable) |
|---|---|---|---|---|
| `TUITION` | Monthly Tuition Fee | Monthly | false | false |
| `ANNUAL` | Annual Fee | Annual | false | false |
| `ADMISSION` | Admission Fee | OneTime | false | false |
| `DEPOSIT` | Deposit (Refundable) | OneTime | false | **true** |
| `EXAMINATION` | Examination Fee | Annual | false | false |
| `COMPUTER` | Computer Fee | Monthly | false | false |
| `SPECIAL_TRAINING` | Special Training Fee | Monthly | **true** | false |
| `HOSTEL` | Hostel Fee | Monthly | **true** | false |
| `MEAL` | Meal Fee | Monthly | **true** | false |
| `TRANSPORTATION` | Transportation Fee | Monthly | **true** | false |
| `EDUCATIONAL_TOUR` | Educational Tour Fee | OneTime | **true** | false |

**Adding a new category**: `POST /api/configs` with `typeCode: 1010` and a new `code`/`label` (plus
optional `additionalValue1..3` defaults) — no backend/schema change needed. This is what "globally
configurable as per school requirement" means: the whitelist itself is admin data, editable through
the same generic Config CRUD every other dropdown in this system uses — schools that don't use
Transportation/Hostel simply never reference those codes; schools that need a Library Fee add it
once here and then use it on any class's fee structure.

**Caveat**: this catalog seeding is create-if-missing, like every other seeder in this project — a
database already seeded with the original (default-less) `FeeCategory` rows won't retroactively
gain `additionalValue1..3` on restart. Backfill those three fields by hand via `PUT
/api/configs/{id}` if this matters for an existing environment, or re-seed on a fresh database.

## Fee Structures — `/api/feestructures`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/feestructures` | `{ "academicClassId": "…", "items": [ { "feeCategoryCode": "TUITION", "amount": 2500, "frequencyType": 1, "isOptional": false, "isRefundable": false }, { "feeCategoryCode": "TRANSPORTATION", "amount": 800, "frequencyType": 1, "isOptional": true, "isRefundable": false } ] }` | Creates the class's fee header **and every submitted item in one call** — this is the direct fix for the old one-call-per-category flow. `items` may be empty (header-only; add items later one at a time). Each `feeCategoryCode` must exist in catalog `1010` and must not repeat within the same request or already exist on the header. `frequencyType`: `1` Monthly / `2` Annual / `3` OneTime. `409` if a header already exists for that class (possibly soft-deleted) — add/update items on it instead |
| `GET /api/feestructures?page=1&pageSize=20&academicYearId=…&academicClassId=…&status=1` | | All filters optional |
| `GET /api/feestructures/{id}` | | Returns the header with its full `items` array |
| `PUT /api/feestructures/{id}` | `{ "status": 1 }` | `academicClassId` immutable. Amount/frequency/optional/refundable now live per-item — this only toggles the whole class's fee structure Active/Inactive |
| `DELETE /api/feestructures/{id}` | | Soft delete; `409` if any item still has an enrollment opted into it (`fee-selections`) |
| `POST /api/feestructures/{id}/items` | `{ "feeCategoryCode", "amount", "frequencyType", "isOptional", "isRefundable" }` | Adds one item to an existing header. `400` if `feeCategoryCode` isn't a known catalog option; `409` if this header already charges that category |
| `PUT /api/feestructures/{id}/items/{itemId}` | `{ "amount", "frequencyType", "isOptional", "isRefundable" }` | `feeCategoryCode` is **immutable** — remove and re-add under a different category instead of moving one |
| `DELETE /api/feestructures/{id}/items/{itemId}` | | `409` if an enrollment has opted into this item via `fee-selections` — remove that selection first |

`FeeStructureDto`: `{ "id", "academicClassId", "academicYearId", "gradeCode", "status", "items": [ { "id", "feeStructureId", "feeCategoryCode", "amount", "frequencyType", "isOptional", "isRefundable" }, ... ] }`.

**`isOptional = true`** means the item doesn't apply to every student by default — only to
enrollments that opt in (see Fee Selections below). Mandatory items (`isOptional = false`) apply to
every enrollment in the class automatically, the same "mandatory applies automatically, optional
needs an explicit pick" shape as `ClassSubject`/`EnrollmentSubject`.

## Fee Selections (optional-item opt-in) — under `/api/enrollments/{id}/fee-selections`

| Method/Route | Notes |
|---|---|
| `POST /api/enrollments/{id}/fee-selections/{feeStructureItemId}` | Opts this enrollment into an optional item (by its `id`, not the category code — a class can only charge a given category once, but the id is what `EnrollmentFeeSelection` actually stores). `400` if the class doesn't charge that item, or if it isn't marked optional (mandatory items already apply — nothing to select). `409` if already selected |
| `GET /api/enrollments/{id}/fee-selections` | |
| `DELETE /api/enrollments/{id}/fee-selections/{feeSelectionId}` | `feeSelectionId` is the selection row's own `id`, not the item id |

`EnrollmentFeeSelectionDto`: `{ "id", "enrollmentId", "feeStructureItemId" }`.

## Discounts & Scholarships — under `/api/enrollments/{id}/...`

**Unaffected by the fee-structure redesigns** — discounts/scholarships never referenced fee
categories, they attach to the enrollment directly.

**Two-tier rate configuration**: every discount/scholarship "reason" (`DiscountType`/
`ScholarshipType` Config catalog, `1008`/`1009`) can carry a **global default rate** in its
`additionalValue1`/`additionalValue2` (`ValueType` name / `Value`, e.g. `"Percentage"` / `"10"` for
a 10% sibling discount) — set when creating/editing the type via `POST`/`PUT /api/configs`. When
adding an award to an enrollment, **omit `valueType`/`value` to use that type's configured
default**, or **supply both explicitly for an individual override** (e.g. this particular student
gets 15% instead of the type's usual 10%). Supplying only one of the pair is a validation error —
the service resolves an omitted *pair* from the catalog default, never a single field.

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/enrollments/{id}/discounts` | `{ "discountTypeCode": "SIBLING", "valueType": null, "value": null, "remarks": null }` | Omitting `valueType`/`value` uses `SIBLING`'s configured default (seeded: 10%). Pass both explicitly to override individually, e.g. `{ "discountTypeCode": "SIBLING", "valueType": 1, "value": 15 }` |
| `GET /api/enrollments/{id}/discounts` | | |
| `DELETE /api/enrollments/{id}/discounts/{discountId}` | | Soft delete |
| `GET /api/enrollments/discounts/summary?academicYearId=&discountTypeCode=` | | Cross-enrollment report, both filters optional — `[{ "typeCode", "studentCount" }]` |
| `POST /api/enrollments/{id}/scholarships` | `{ "scholarshipTypeCode": "CLASS_TOPPER", "valueType": null, "value": null, "remarks": "Rank 1, Grade 10" }` | Same two-tier resolution against `ScholarshipType`'s default (seeded: `CLASS_TOPPER` = 100%, i.e. a full waiver) |
| `GET /api/enrollments/{id}/scholarships` | | |
| `DELETE /api/enrollments/{id}/scholarships/{scholarshipId}` | | Soft delete |
| `GET /api/enrollments/scholarships/summary?academicYearId=&scholarshipTypeCode=` | | **"How many students got a scholarship"** — both filters optional; `[{ "typeCode", "studentCount" }]` |

`StudentDiscountDto`/`StudentScholarshipDto` always report the **resolved** `valueType`/`value` (never null in the response, even if the request omitted them) — `{ "id", "enrollmentId", "discountTypeCode", "valueType", "value", "remarks" }` (scholarship equivalent uses `scholarshipTypeCode`).

**Seeded default rates** (via `Config.additionalValue1`/`additionalValue2`, editable via `PUT /api/configs/{id}`):

| Catalog | Code | Default |
|---|---|---|
| Discount Type (`1008`) | `SIBLING` | 10% |
| | `STAFF_CHILD` | 50% |
| | `EARLY_PAYMENT` | 5% |
| | `FINANCIAL_HARDSHIP`, `OTHER` | none — always individually assessed |
| Scholarship Type (`1009`) | `CLASS_TOPPER` | 100% |
| | `MERIT_EXAM` | 25% |
| | `SOCIAL_CATEGORY` | 20% |
| | `SPORTS` | 15% |
| | `OTHER` | none |

If a caller omits `valueType`/`value` for a type with no configured default, the request fails with
`400 VALIDATION_ERROR` asking for them explicitly.

## Enrollment fee-structure API (student detail page)

`GET /api/enrollments/{id}/fee-structure` — the full priced view for one enrollment, bound to its
academic year through the enrollment's own class chain (no separate year parameter needed):

```json
{
  "enrollmentId": "…",
  "academicYearId": "…",
  "academicClassId": "…",
  "gradeCode": "GRADE_10",
  "feeItems": [
    { "feeStructureItemId": "…", "feeCategoryCode": "TUITION", "amount": 2500, "frequencyType": 1, "isOptional": false, "isRefundable": false, "applies": true },
    { "feeStructureItemId": "…", "feeCategoryCode": "TRANSPORTATION", "amount": 800, "frequencyType": 1, "isOptional": true, "isRefundable": false, "applies": true },
    { "feeStructureItemId": "…", "feeCategoryCode": "MEAL", "amount": 1200, "frequencyType": 1, "isOptional": true, "isRefundable": false, "applies": false },
    { "feeStructureItemId": "…", "feeCategoryCode": "ADMISSION", "amount": 5000, "frequencyType": 3, "isOptional": false, "isRefundable": false, "applies": true },
    { "feeStructureItemId": "…", "feeCategoryCode": "DEPOSIT", "amount": 3000, "frequencyType": 3, "isOptional": false, "isRefundable": true, "applies": true }
  ],
  "discounts": [ { "id": "…", "discountTypeCode": "SIBLING", "valueType": 1, "value": 10, "remarks": null } ],
  "scholarships": [],
  "summary": {
    "monthlyRecurringTotal": 3300,
    "annualTotal": 0,
    "oneTimeTotal": 5000,
    "refundableDepositTotal": 3000,
    "totalDiscountReduction": 330,
    "totalScholarshipReduction": 0,
    "netMonthlyRecurring": 2970
  }
}
```

`feeItems` lists **every** item the class charges, `applies` telling you whether it's actually
billed to this enrollment (mandatory items are always `true`; optional ones follow the
enrollment's `fee-selections`). `summary` groups by billing frequency, since a Monthly amount and an
Annual amount can't be meaningfully added together:

- `monthlyRecurringTotal`/`annualTotal`/`oneTimeTotal` — sums of **applicable, non-refundable**
  items by frequency.
- `refundableDepositTotal` — applicable refundable items (Deposit), kept separate since it isn't a
  cost.
- `totalDiscountReduction`/`totalScholarshipReduction` — **discounts and scholarships reduce only
  `monthlyRecurringTotal`**, not Annual/OneTime items. Percentage awards compute against the
  pre-discount monthly total (additive across multiple awards, not compounded); fixed-amount
  awards subtract directly. This is a deliberate simplification — a single percentage applied
  across mixed billing horizons (monthly vs. annual vs. one-time) doesn't have one sensible
  meaning, and "ongoing tuition relief" is the most common real-world meaning of "a scholarship."
- `netMonthlyRecurring` — `monthlyRecurringTotal` minus both reductions, floored at 0.

## Deliberately out of scope

- Invoicing and payment tracking — this module prices what's owed, it doesn't bill or collect.
- Discounting Annual/OneTime fee items — only the monthly recurring total is reduced (see above); a
  future iteration could add a `DiscountScope` if broader relief is needed.
- Automated eligibility detection (e.g. auto-flagging "class topper" from exam results) — no
  exam/marks-entry subsystem exists yet, so every award is a manual admin action.
- Fee item versioning/history — editing an item's amount changes it in place; there's no
  "effective from" concept like `EmployeeSalary` revisions. Model a new academic year's class (a
  new `AcademicClass` row) with its own fee structure instead, same as every other per-year fact in
  this codebase.

## Fee receipt preview

`GET /api/enrollments/{id}/fee-receipt-preview` renders an admin-configurable HTML template using
this same composed fee-structure data — see `Docs/document_preview_implementation_guide.md` for
the full mechanism and placeholder-token catalog.

## Migration required

Schema changes for the header+items redesign:
- `dbo.fee_structures` loses `fee_category_code`/`amount`/`frequency_type`/`is_optional`/
  `is_refundable` (now header-only: `id`, `academic_class_id`, `status` + audit/soft-delete
  columns); unique index changes from `(academic_class_id, fee_category_code)` to just
  `(academic_class_id)`.
- New table `dbo.fee_structure_items` (`id`, `fee_structure_id` FK with cascade delete,
  `fee_category_code`, `amount`, `frequency_type`, `is_optional`, `is_refundable` + audit columns,
  no soft delete); unique index on `(fee_structure_id, fee_category_code)` — a class's fee
  structure can't charge the same category twice.
- `dbo.enrollment_fee_selections.fee_category_code` (string) → `fee_structure_item_id` (Guid, FK to
  `fee_structure_items`); unique index changes from `(enrollment_id, fee_category_code)` to
  `(enrollment_id, fee_structure_item_id)`.
- No migration needed for the `Config`/`ConfigType` default-value changes — those are seeded data,
  not schema.

Not created here — run `dotnet ef migrations add` yourself. If `dbo.fee_structures`/
`dbo.enrollment_fee_selections` already have rows from any prior shape on a dev DB, they predate
this structure and will need clearing before the new tables/indexes can apply — same pragmatic
"clear dev data" path as the earlier fee-structure redesigns this same day.
