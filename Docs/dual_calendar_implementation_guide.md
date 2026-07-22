# Dual Calendar (AD / Bikram Sambat) & Meeting Scheduling — UI Implementation Guide

Backend feature reference for the frontend team (2026-07-16). Implements the design in
`Dual_Calendar_AD_BS_Implementation_Guide.md`, adapted to this codebase's conventions. Every
response uses the standard envelope `{ responseCode, responseMessage, data }`; enums are
serialized as **numbers**; times of day are `"HH:mm:ss"` strings; dates are ISO strings with a
zero time part (`"2026-07-16T00:00:00"`) — treat them as calendar dates, never timezone-shift
them.

**Architecture in one paragraph**: everything is stored AD-canonical (`adDate`) with the BS
date (`bsYear`/`bsMonth`/`bsDay`) denormalized next to it, so the UI never converts anything
itself — every payload already carries both calendars. BS month lengths are not formulaic;
they come from the admin-editable `bs_month_lengths` table (seeded for **BS 2000–2090**,
verified against known BS/AD checkpoints; anchor BS 2000-01-01 = AD **1943-04-14**). When the
government publishes BS 2091, an admin adds 12 rows via the config endpoint — no deployment.
Conversions outside the configured range fail with `VALIDATION_ERROR`, not a 500.

---

## Menu / permission codes (seeded)

| Code | Where | What it gates |
|---|---|---|
| `CALENDAR_CONFIG_LIST` | Setup → BS Calendar Setup (`/apps/calendar-config/list`) | `GET /api/calendar-configuration/bs-month-lengths` |
| `BS_MONTH_LENGTH_UPSERT`, `CALENDAR_LOCALIZATION`, `BS_WEEKDAY_UPDATE` | hidden permissions under it | the other config endpoints |
| `CALENDAR_VIEW` | Calendar → Calendar (`/apps/calendar`) | `GET /api/calendar/month-view` |
| `CALENDAR_TODAY`, `CALENDAR_CONVERT_*`, `CALENDAR_EVENT_*`, `FESTIVAL_*` | hidden permissions under it | conversion utilities + event/festival CRUD |
| `MEETING_LIST` | Calendar → Meetings (`/apps/meeting/list`) | `GET /api/meetings` |
| `MEETING_SCHEDULE`, `MEETING_DETAIL`, `MEETING_UPDATE`, `MEETING_CANCEL`, `MEETING_RESPOND` | hidden permissions under it | the other meeting endpoints |

**Open to any authenticated user without a permission grant** (via `DefaultEnabledMenu`):
`GET /api/calendar/today`, both `/api/calendar/convert/*` endpoints,
`GET /api/calendar-configuration/localization-data`, and `POST /api/meetings/respond` —
these are utility calls every screen with a date picker or an RSVP button needs.

---

## 1. Calendar configuration — `/api/calendar-configuration`

### GET `/api/calendar-configuration/localization-data`
Month and weekday display names — fetch once at app start and cache (SWR key), it changes
almost never.

```json
{
  "responseCode": "0",
  "data": {
    "months":   [ { "id": "…", "monthNumber": 1, "nameEn": "Baisakh", "nameNp": "वैशाख" }, …12 rows ],
    "weekdays": [ { "id": "…", "weekdayIndex": 0, "nameEn": "Sunday", "nameNp": "आइतबार", "isWeeklyHoliday": false }, …7 rows ]
  }
}
```
`weekdayIndex` is 0=Sunday..6=Saturday — same as JS `Date.getDay()`. Saturday is seeded with
`isWeeklyHoliday: true`.

### GET `/api/calendar-configuration/bs-month-lengths?bsYear=2083`
Admin grid data. `bsYear` optional (omit = all years, ordered year then month).
`data`: `[ { "id": "…", "bsYear": 2083, "bsMonth": 1, "daysInMonth": 31 }, … ]`

### POST `/api/calendar-configuration/bs-month-lengths`
Bulk upsert — the yearly "government published BS 2091" admin task. Existing (year, month)
pairs are updated, new ones inserted, in one call.

```json
{ "items": [ { "bsYear": 2091, "bsMonth": 1, "daysInMonth": 31 }, … 12 rows ] }
```
Failures: `VALIDATION_ERROR` — empty list, year outside 2000–2200, month outside 1–12, days
outside 29–32, or duplicate (year, month) pairs inside the payload.

### PUT `/api/calendar-configuration/weekdays/{weekdayIndex}`
Rename a weekday / toggle the weekly holiday (e.g. add Sunday as a second holiday).
Body: `{ "nameEn": "Saturday", "nameNp": "शनिबार", "isWeeklyHoliday": true }` →
returns the updated weekday row. `NOT_FOUND` for an index outside 0–6.

---

## 2. Calendar view & conversion — `/api/calendar`

### GET `/api/calendar/month-view?year=2083&month=4&mode=BS`
The one call a calendar page needs. `mode` = `BS` (default) or `AD`; `year`/`month` are read
in that calendar. Returns one entry per day of the requested month with everything already
joined — both dates, localized day names, weekly-holiday flag, events, festivals, meetings.

```json
{
  "data": {
    "mode": "BS", "year": 2083, "month": 4,
    "monthNameEn": "Shrawan", "monthNameNp": "साउन",
    "totalDays": 31,
    "startAdDate": "2026-07-17T00:00:00", "endAdDate": "2026-08-16T00:00:00",
    "days": [
      {
        "adDate": "2026-07-17T00:00:00", "adYear": 2026, "adMonth": 7, "adDay": 17,
        "bsYear": 2083, "bsMonth": 4, "bsDay": 1,
        "dayOfWeekIndex": 5, "dayNameEn": "Friday", "dayNameNp": "शुक्रबार",
        "isWeeklyHoliday": false,
        "isToday": false,
        "events":    [ { "id": "…", "title": "Constitution Day", "eventType": 1, "colorCode": "#d32f2f", "iconKey": "flag" } ],
        "festivals": [ { "id": "…", "festivalName": "Dashain", "category": 1, "colorCode": "#ff9800", "isStartDay": true, "isEndDay": false } ],
        "meetings":  [ { "id": "…", "title": "Staff meeting", "startTime": "10:00:00", "endTime": "11:30:00", "isVirtual": true, "location": "https://meet…" } ]
      }
    ]
  }
}
```

Grid layout recipe (BS mode): pad the first week with `days[0].dayOfWeekIndex` blank cells,
then flow the `days` array into a 7-column grid; column headers come from
`localization-data` weekdays; shade `isWeeklyHoliday` columns and any day whose `events`
contain `eventType: 1` (PublicHoliday) or whose `festivals` are non-empty. `isToday` is
computed against **Nepal time** (UTC+05:45) on the server, so highlight that cell directly.

Failures: `VALIDATION_ERROR` — bad mode/month, or a BS year with no configured month lengths
("BS month-length configuration is missing for BS year X…" — surface this verbatim, it tells
the admin exactly what to do).

### GET `/api/calendar/today`
### GET `/api/calendar/convert/ad-to-bs?adDate=2026-07-16`
### GET `/api/calendar/convert/bs-to-ad?bsYear=2083&bsMonth=4&bsDay=1`
All three return the same `DualDateDto` — use it to power a dual date picker (user types in
either calendar; the other half of the display fills itself):

```json
{
  "data": {
    "adDate": "2026-07-16T00:00:00",
    "bsYear": 2083, "bsMonth": 3, "bsDay": 32,
    "bsMonthNameEn": "Ashadh", "bsMonthNameNp": "असार",
    "dayOfWeekIndex": 4, "dayNameEn": "Thursday", "dayNameNp": "बिहीबार"
  }
}
```
`VALIDATION_ERROR` when out of the configured range (before AD 1944-ish / BS 2000, or past
the last configured BS year) or when `bsDay` exceeds that month's real length.

### Calendar events (notes / public holidays / internal events)

Enums: `eventType` — 0 Note, 1 PublicHoliday, 2 InternalEvent.

| Endpoint | Notes |
|---|---|
| `GET /api/calendar/events?page=1&pageSize=10&eventType=&fromAdDate=&toAdDate=&bsYear=&isActive=` | paged envelope (`items/page/pageSize/totalCount/…`), ordered by `adDate` |
| `POST /api/calendar/events` | create — see body below |
| `GET /api/calendar/events/{id}` | single `CalendarEventDto` |
| `PUT /api/calendar/events/{id}` | same body as create |
| `DELETE /api/calendar/events/{id}` | soft delete |

Create/update body — the date may be entered on **either** calendar; the backend computes
and stores the other side:

```json
{
  "title": "Constitution Day",
  "eventType": 1,
  "isBsDate": true,
  "bsYear": 2083, "bsMonth": 6, "bsDay": 3,
  "adDate": null,
  "description": "…", "iconKey": "flag", "colorCode": "#d32f2f",
  "language": "en", "isActive": true
}
```
`isBsDate: false` → send `adDate` instead and leave the BS fields null. The returned DTO
always carries both (`adDate` + `bsYear/bsMonth/bsDay`).

### Festival occurrences (Dashain, Tihar, … — shift every AD year)

Festivals are **entered in BS only** (they are BS-anchored facts); the backend denormalizes
the AD range. One row per festival per BS year; a new BS year needs its occurrences entered
(or the year's admin does it once — there is no auto-repeat, the dates genuinely change).
`category`: 0 National, 1 Religious, 2 Regional.

| Endpoint | Notes |
|---|---|
| `GET /api/calendar/festivals?bsYear=2083&isActive=true` | flat list (unpaged), ordered by `adStartDate` |
| `POST /api/calendar/festivals` | `CONFLICT` if (name, bsYear) already exists |
| `GET /api/calendar/festivals/{id}` / `PUT …/{id}` / `DELETE …/{id}` | usual shapes; delete is soft |

```json
{
  "festivalName": "Dashain", "category": 1,
  "bsYear": 2083,
  "bsStartMonth": 6, "bsStartDay": 15, "bsEndMonth": 7, "bsEndDay": 8,
  "description": "…", "colorCode": "#ff9800", "isActive": true
}
```
Returned DTO adds the computed `adStartDate`/`adEndDate`. The occurrence must fit inside one
BS year (end ≥ start validated); a festival crossing the BS new year = two occurrences.

---

## 3. Meetings — `/api/meetings`

`AttendeeStatus`: 0 Pending, 1 Accepted, 2 Declined, 3 Tentative.

### POST `/api/meetings/schedule`

```json
{
  "title": "Parent-teacher conference",
  "description": "…",
  "isBsScheduled": true,
  "scheduledBsYear": 2083, "scheduledBsMonth": 5, "scheduledBsDay": 10,
  "scheduledAdDate": null,
  "startTime": "10:00:00", "endTime": "11:30:00",
  "isVirtual": false, "location": "Conference Room A",
  "hostUserId": null,
  "attendeeEmails": ["teacher@school.edu.np", "parent@example.com"]
}
```

- Same either-calendar date rule as events (`isBsScheduled` ↔ `isBsDate`).
- `hostUserId: null` → the authenticated caller becomes the host (the common case; only send
  a value when scheduling on someone else's behalf).
- Emails are normalized to lowercase and de-duplicated; every attendee starts `Pending`.
- **`CONFLICT` (HTTP 409)** when the host already has a meeting with an overlapping
  `[startTime, endTime)` block on the same date — show `responseMessage` and let the user
  pick another slot.

Response `data` (`MeetingDto` — same shape from every meeting endpoint):

```json
{
  "id": "…", "title": "…", "description": "…",
  "adDate": "2026-08-26T00:00:00", "bsYear": 2083, "bsMonth": 5, "bsDay": 10,
  "startTime": "10:00:00", "endTime": "11:30:00",
  "isVirtual": false, "location": "Conference Room A",
  "hostUserId": "…",
  "attendees": [ { "id": "…", "userId": null, "email": "parent@example.com", "status": 0 } ]
}
```

### Other meeting endpoints

| Endpoint | Notes |
|---|---|
| `GET /api/meetings?page=1&pageSize=10&fromAdDate=&toAdDate=&hostUserId=` | paged, ordered date then start time; attendees included |
| `GET /api/meetings/{id}` | single meeting with attendees |
| `PUT /api/meetings/{id}` | same body as schedule minus `hostUserId` (host is immutable). `attendeeEmails` is three-way: `null` = leave the list alone, `[]` = remove everyone, list = replace-sync (existing attendees keep their RSVP status). Re-runs the conflict check excluding itself. |
| `DELETE /api/meetings/{id}` | cancel (soft delete — vanishes from month-view and lists) |
| `POST /api/meetings/respond` | body `{ "meetingId": "…", "email": "…", "status": 1 }` → updated `MeetingDto`. `NOT_FOUND` if the email isn't on the meeting's register. Open to all authenticated users. |

`hostUserId` is a raw user id — resolve a display name via the users API if you need one;
the meeting payload deliberately doesn't embed it.

---

## 4. ClientUI integration notes (AppShell conventions)

1. **API service** — create `src/api/calendar-service.js` + `src/api/meeting-service.js`
   following `account-service.js`: an `endpoints` map, `useGet*` SWR hooks over `fetcher`
   (e.g. `useGetMonthView(year, month, mode)` keyed on the full query string,
   `useGetCalendarLocalization()` with a long `dedupingInterval` — it's static reference
   data), and plain async mutations that `mutate()` the month-view key(s) they affect.
   After any event/festival/meeting mutation, revalidate the month-view key for that item's
   month **in both modes** if you cache both.
2. **Routes** — the seeded menu urls expect: `/apps/calendar` (month grid),
   `/apps/meeting/list`, and `/apps/calendar-config/list` under Setup. Add lazy routes in
   `MainRoutes.jsx` as usual; the sidebar entries appear automatically from
   `/api/roles/user-menus` once the seeder has run and the role has grants.
3. **Month grid component** — don't reuse the Mantis FullCalendar demo's local-mock api
   (`api/calendar.js` if present is example code); build the grid from `month-view` directly.
   Each cell renders `bsDay` big + `adDay` small (or the reverse per a UI toggle); the
   mode toggle re-fetches with `mode=AD|BS`.
4. **Dual date picker** — for forms (events, meetings, student DOB later): two tab panes
   ("BS" / "AD"); the BS pane is three selects (year from configured range, month from
   `localization-data`, day 1..`daysInMonth` — fetch `bs-month-lengths?bsYear=` or read
   `totalDays` from a month-view call); on change call `convert/bs-to-ad` to display the
   Gregorian equivalent, and vice versa with `convert/ad-to-bs`. Submit whichever side the
   user actually edited with the matching `isBsDate`/`isBsScheduled` flag.
5. **Nepali digits** (optional polish): convert `0-9` → `०-९` client-side when the locale is
   `ne` — the API always returns Latin digits.
6. **Time fields** — send `"HH:mm:ss"` strings; MUI TimePicker values need formatting
   (`format(value, 'HH:mm:ss')`).

## 5. Failure table (common to all endpoints)

| responseCode | HTTP | Meaning / UI action |
|---|---|---|
| `0` (Success) | 200 | proceed |
| `VALIDATION_ERROR` | 400 | show `responseMessage` (includes missing BS-year configuration messages) |
| `CONFLICT` | 409 | duplicate festival year, or host double-booked — show message inline |
| `NOT_FOUND` | 404 | stale id / wrong weekday index / unknown attendee email |
| `FORBIDDEN` | 403 | role lacks the permission row — hide the action next time |

## 6. Migration & seeding status

The 7 new tables (`dbo.bs_month_lengths`, `dbo.bs_month_names`, `dbo.bs_weekday_names`,
`dbo.calendar_events`, `dbo.festival_occurrences`, `dbo.meetings`, `dbo.meeting_attendees`)
**need an EF migration that is user-created** (`dotnet ef migrations add …` — not created by
this change, per project convention). Until it's applied, every endpoint in this guide 500s
and `CalendarSeeder` logs-and-skips. On the first migrated boot the seeder loads the 12
month names, 7 weekdays (Saturday = weekly holiday), and the full BS 2000–2090 month-length
table; all of it is admin-editable afterwards and edits survive restarts.

**Deviation from the design doc worth knowing**: the AD anchor is **1943-04-14** (the doc
said 04-13, which is off by one against every published BS/AD reference date — verified
against Nepali New Year 2072/2077/2081/2082/2083 and Constitution Day 2072-06-03 =
2015-09-20). Attendee uniqueness is per (meeting, **email**), not (meeting, userId), since
invitees don't need accounts.
