# Leave Configurability (day-count caps + emergency override) — implementation guide

**2026-07-24, revised same day.** Extends `LeaveType`
(`Docs/leave_management_and_employee_profile_implementation_guide.md`) from three flat facts
(`daysPerYear`/`carryForward`/`isPaid`) with a maximum number of consecutive days per request and a
cap on days taken per week/month — plus two new leave types, **Bereavement Leave** and
**Marriage Leave**. Nothing here changes the existing approval workflow (Manager/HR independent
one-shot decisions) — this is entirely about what's *allowed to be submitted* in the first place.

**Revision note**: an earlier pass of this feature also added a per-type "requires a document
above N days" rule and a per-type "allow emergency override" toggle. Both were removed the same
day, by request — whether a supporting document backs a request is left to the requester/HR
conversation (the requester can attach one at apply time, or HR can ask for one before deciding),
not a system-enforced gate. `LeaveRequest.AttachmentPath` stays exactly what it was before this
feature: always optional, for every leave type.

## New `LeaveType` fields

| Field | Type | Meaning | `null`/unset behavior |
|---|---|---|---|
| `maxConsecutiveDays` | int, optional | The most days a **single request** of this type may span. | No cap. |
| `maxDaysPerWeek` | decimal, optional | The most days of this type an employee may have Pending+Approved within the calendar week containing the request's `fromDate`. | No cap. |
| `maxDaysPerMonth` | decimal, optional | Same, but for the calendar month containing `fromDate`. | No cap. |

Both are optional/independent — a leave type with neither configured behaves exactly as it did
before this feature existed.

## New `LeaveRequest` field: `isEmergency`

The Apply Leave form gains one boolean field, `isEmergency` (default `false`), sent alongside the
existing multipart fields (`leaveTypeId`/`fromDate`/`toDate`/`reason`/`substituteEmployeeId`/
`attachment`). This is **the requester's own claim, not independently verified**, and it
**unconditionally bypasses `maxConsecutiveDays`/`maxDaysPerWeek`/`maxDaysPerMonth` for any leave
type** — there's no per-type toggle to gate this (that's exactly the "allow emergency override"
field removed in the revision above). Reviewers see the flag on `LeaveRequestDto.isEmergency` and
can factor it into their manager/HR decision like any other field — it does not auto-approve
anything, and the normal one-shot `managerStatus`/`hrStatus` workflow is untouched.

**Two unrelated "emergency" concepts — don't conflate them** (see the same callout in
`leave_management_and_employee_profile_implementation_guide.md`): this `isEmergency` flag only
affects whether the day-count caps apply. It's completely separate from HR's pre-existing ability
to decide a request regardless of `managerStatus`.

## Worked example (matches the user's own scenario)

`Casual Leave` is seeded with `maxConsecutiveDays = 3` and `maxDaysPerMonth = 5` — the "prevent
abuse" example:

- A normal 2-day Casual Leave request is fine.
- A 5-day Casual Leave request is rejected (`VALIDATION_ERROR`, exceeds the 3-day consecutive cap)
  — unless submitted with `isEmergency = true`, which lets it through regardless of the cap. The
  requester can still attach a supporting document if they have one, or HR can ask for one before
  deciding — neither is enforced by the API.
- A 6th day of Casual Leave requested within the same calendar month (bringing the month's total
  over 5) is rejected the same way, with the same emergency escape hatch.

## Week/month cap math

A week is the calendar week containing `fromDate` (Sunday–Saturday); a month is the calendar month
containing `fromDate`. The check sums:

1. Every existing Pending/Approved (`hrStatus != Rejected`) request of the **same employee and
   same leave type** whose date range overlaps the window, clipped to the days that actually fall
   inside it (a request spanning a month boundary only counts its portion inside that month).
2. The new request's own portion inside the same window.

If the total exceeds `maxDaysPerWeek`/`maxDaysPerMonth`, the request is rejected — unless
`isEmergency = true`, which skips this check (and the `maxConsecutiveDays` check) entirely. This
runs **before** any attachment is saved to disk — a policy violation never leaves an orphaned file
behind.

## Seed data (`LeaveTypeSeeder`, illustrative — verify against actual policy)

| Name | Days/Year | Carry Forward | Consecutive Cap | Week Cap | Month Cap |
|---|---|---|---|---|---|
| Annual Leave | 18 | Yes | — | — | — |
| Sick Leave | 12 | No | — | — | — |
| Casual Leave | 12 | No | 3 days | — | 5 days |
| **Bereavement Leave** (new) | 13 | No | 13 days | — | — |
| **Marriage Leave** (new) | 7 | No | 7 days | — | — |

Create-if-missing by `Name`, same as before — **an already-seeded leave type on an existing
database does not retroactively get these policy values**; edit it via `PUT /api/leavetypes/{id}`
if you want them applied.

## Failure cases (new, on `POST /api/employees/{id}/leaverequests`)

| Condition | Response |
|---|---|
| Request longer than `maxConsecutiveDays`, not submitted as an emergency | `VALIDATION_ERROR` — "`<LeaveType>` cannot exceed N consecutive day(s) in a single request. Submit as an emergency application if this is unavoidable." |
| Adding this request would push the week/month total over `maxDaysPerWeek`/`maxDaysPerMonth`, not submitted as an emergency | `VALIDATION_ERROR` — same shape, naming the week/month cap |
| `isEmergency = true` | Both checks above are skipped entirely for this request, regardless of the leave type. |

## Suggested "New/Edit Leave Type" form change

The existing modal (Name / Days per Year / Carry Forward / Paid Leave) gains one optional
"Request limits" section: `Max Consecutive Days` (number), `Max Days / Week` (number),
`Max Days / Month` (number) — all blank = no cap.

## Suggested "Apply Leave" form change

Add a "This is an emergency application" toggle (`isEmergency`) near the attachment field — helper
text along the lines of *"Lets this request exceed the leave type's day-limit policies. Attach a
supporting document if you have one, or HR may ask for one before deciding."*

## Needs a migration

Three new columns on `dbo.leave_types` (`max_consecutive_days integer NULL`,
`max_days_per_week decimal(6,2) NULL`, `max_days_per_month decimal(6,2) NULL`) and one new column
on `dbo.leave_requests` (`is_emergency boolean NOT NULL`). Every leave-type create/update and
leave-request create call fails until these exist.
