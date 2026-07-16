# CMSApp — Auto-generated Admission / Employee Numbers (UI)

**What shipped (2026-07-13)**: `admissionNo` (students) and `employeeNo` (teachers) are now **optional on create** — leave them out (or blank) and the backend assigns the next number. No new endpoints, no migration.

## Behavior

| Field | Endpoint | Generated format | Example |
|---|---|---|---|
| `admissionNo` | `POST /api/students` | `ADM{year}{seq}` — per-calendar-year sequence, zero-padded to 3 (grows past 999 naturally) | `ADM2026001`, `ADM2026002`, … `ADM2027001` |
| `employeeNo` | `POST /api/teachers` | `EMP{year}{seq}` — same scheme | `EMP2026001` |

- **Omit the field** (or send `""`) → backend generates. The created record in the response carries the assigned number — show it on the success screen ("Admission No: ADM2026003").
- **Send a value** → behaves exactly as before: honored as-is, `409 CONFLICT` if already in use (soft-deleted records still reserve numbers). Use this for records migrated from an older system.
- Sequences count soft-deleted records too, so a number is never reused after a delete.
- Both fields remain **immutable on update**, generated or not.

## UI suggestions

- Make the Admission/Employee No input optional with placeholder "Auto-generated if left blank", or hide it behind an "Enter manually" toggle.
- Don't try to predict the next number client-side — read it from the create response.

## Backend note

Generation is a read-then-insert (max existing suffix + 1), not a database sequence — two truly simultaneous creates could collide, in which case the unique index rejects one with a 500. For an admin-operated back office this is acceptable; if bulk imports ever run in parallel, move the counter to a real DB sequence then.
