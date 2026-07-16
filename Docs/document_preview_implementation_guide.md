# Document Preview (Payslip, Fee Receipt, ID Cards) (2026-07-15)

Renders an admin-configurable HTML template into a final HTML string for four documents: an
employee payslip, a student fee receipt, and student/teacher ID cards. Follows every convention in
`UI-Implementation-Guide.md` (envelope, camelCase, Bearer token, permission-gated). **Needs a
migration** — see the note at the bottom.

## Core idea

The **layout lives in an admin-configurable HTML template** (`DocumentTemplate`, one row per
document type, edited via `/api/documenttemplates`). The **backend is the sole authority on what
data values exist** — every template type has a fixed, hardcoded catalog of `{{Token}}`
placeholders (discoverable via `GET /api/documenttemplates/placeholders/{templateType}`), and a
preview endpoint on the relevant feature computes those values and substitutes them into the
template, returning the final HTML string for the frontend to display/print.

Repeating sections (fee item rows, salary component rows, tax breakdown rows, discount/scholarship
rows) are **pre-rendered by the backend as `<tr>` HTML fragments** and injected through their own
single token (e.g. `{{FeeItemsRows}}`) — there is no loop/conditional syntax in the template
language itself, just plain token substitution.

**Out of scope, deliberately**: no PDF generation (HTML only — the frontend prints via the
browser); no photo/image support (neither `StudentDto` nor `TeacherDto` has a photo field today);
one template per document type, no versioning/multiple named templates; the payslip preview always
reflects the employee's **latest** salary revision, never a historical one.

## Template management — `/api/documenttemplates`

| Method/Route | Body / Query | Notes |
|---|---|---|
| `POST /api/documenttemplates` | `{ "templateType": 1, "name", "htmlContent" }` | `templateType` unique — `409` if one already exists for that type (update it instead). See enum below |
| `GET /api/documenttemplates?page=&pageSize=&templateType=` | | `templateType` filter optional |
| `GET /api/documenttemplates/{id}` | | |
| `PUT /api/documenttemplates/{id}` | `{ "name", "htmlContent" }` | `templateType` immutable |
| `DELETE /api/documenttemplates/{id}` | | |
| `GET /api/documenttemplates/placeholders/{templateType}` | | Returns `[{ "token", "description" }, ...]` — the full, backend-authoritative list of tokens usable in that type's `htmlContent` |

`DocumentTemplateType` enum: `1` Payslip, `2` FeeReceipt, `3` StudentIdCard, `4` TeacherIdCard.

`DocumentTemplateSeeder` seeds one simple default HTML template per type on first boot (idempotent,
create-if-missing by `templateType`) so every preview endpoint works out of the box; an admin's
hand-edited template survives every restart since the seeder never updates an existing row.

## Preview endpoints

| Method/Route | Notes |
|---|---|
| `GET /api/employees/{id}/salaries/payslip-preview?fiscalYearId=` | Renders the `Payslip` template using the employee's **latest** salary revision + `GetCurrentSalaryTaxCalculationAsync`'s output (`fiscalYearId` optional, defaults to the current fiscal year) |
| `GET /api/teachers/{id}/salaries/payslip-preview?fiscalYearId=` | Thin alias — the teacher's id *is* its Employee id (shared-PK trick) |
| `GET /api/enrollments/{id}/fee-receipt-preview` | Renders the `FeeReceipt` template from the same composed data as `GET /api/enrollments/{id}/fee-structure` |
| `GET /api/students/{id}/id-card-preview` | Renders the `StudentIdCard` template from the student's profile + primary guardian + current enrollment |
| `GET /api/teachers/{id}/id-card-preview` | Renders the `TeacherIdCard` template from the teacher's profile |

All five return `CommonResponse<DocumentPreviewDto>` — `{ "templateType", "html" }`. `404
NOT_FOUND` ("No document template is configured for '...' yet") if the relevant `DocumentTemplate`
row doesn't exist — create one via `POST /api/documenttemplates` (or rely on the seeder's default).

## Placeholder-token catalogs

### Payslip (`templateType: 1`)

| Token | Meaning |
|---|---|
| `{{EmployeeName}}` | Employee's full name |
| `{{EmployeeCode}}` | Employee's unique code |
| `{{JobPositionCode}}` | Employee's job position code |
| `{{EffectiveFromDate}}` | Latest salary revision's effective-from date |
| `{{FiscalYearCode}}` | Fiscal year used for the tax calculation |
| `{{GrossMonthly}}` / `{{NetMonthly}}` | Gross / net monthly pay |
| `{{GrossAnnualIncome}}` | Gross annual taxable income |
| `{{RetirementContributionAnnual}}` / `{{RetirementExemption}}` | Retirement-fund least-of-three inputs |
| `{{InsuranceDeduction}}` | Capped insurance-premium deduction |
| `{{AnnualTaxableIncome}}` / `{{AnnualTax}}` / `{{MonthlyTax}}` | Final tax figures |
| `{{ComponentsRows}}` / `{{DeductionsRows}}` / `{{InsurancePremiumsRows}}` / `{{TaxBreakdownRows}}` | Pre-built `<tr>` rows, one per line item / tax slab |

### Fee receipt (`templateType: 2`)

| Token | Meaning |
|---|---|
| `{{StudentName}}` / `{{AdmissionNo}}` / `{{GradeCode}}` / `{{SectionCode}}` / `{{RollNumber}}` | Student/enrollment identity |
| `{{FeeItemsRows}}` / `{{DiscountsRows}}` / `{{ScholarshipsRows}}` | Pre-built `<tr>` rows |
| `{{MonthlyRecurringTotal}}` / `{{AnnualTotal}}` / `{{OneTimeTotal}}` / `{{RefundableDepositTotal}}` | Fee totals by billing frequency |
| `{{TotalDiscountReduction}}` / `{{TotalScholarshipReduction}}` / `{{NetMonthlyRecurring}}` | Final monthly figure after awards |

### Student ID card (`templateType: 3`)

`{{StudentName}}`, `{{AdmissionNo}}`, `{{GradeCode}}`, `{{SectionCode}}`, `{{RollNumber}}`,
`{{DateOfBirth}}`, `{{GuardianName}}`, `{{GuardianPhone}}` (primary guardian only).

### Teacher ID card (`templateType: 4`)

`{{EmployeeCode}}`, `{{TeacherName}}`, `{{JobPositionCode}}`, `{{TeachingLicenseNo}}`,
`{{Specialization}}`, `{{JoinDate}}`, `{{Phone}}`, `{{Email}}`.

## Sample default template (Student ID card, seeded)

```html
<div style="font-family:Arial,sans-serif;width:340px;padding:12px;border:1px solid #333;border-radius:8px;">
  <h3>Student ID Card</h3>
  <p><strong>{{StudentName}}</strong></p>
  <p>Admission No: {{AdmissionNo}}</p>
  <p>Grade {{GradeCode}} {{SectionCode}} | Roll No {{RollNumber}}</p>
  <p>Date of Birth: {{DateOfBirth}}</p>
  <p>Guardian: {{GuardianName}} ({{GuardianPhone}})</p>
</div>
```

The other three seeded defaults follow the same shape — see `DocumentTemplateSeeder.cs` for the
exact HTML strings, or `GET /api/documenttemplates` after first boot.

## Known limitations

- No PDF generation — HTML string only.
- No photo/image support on Student/Teacher (no schema field exists for it yet).
- One template per document type — no versioning.
- Payslip preview always reflects the latest salary revision, never a historical one (avoids a
  payslip whose line items and tax numbers come from two different revisions).

## Migration required

New table `dbo.document_templates` (unique index on `template_type`, `html_content` as an
unbounded `text` column). Not created here, per this repo's convention — run
`dotnet ef migrations add` yourself.
