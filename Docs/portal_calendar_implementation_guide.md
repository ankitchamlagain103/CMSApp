# Teacher/Student Portal Calendar (2026-08-27) — implementation guide

Gives the Teacher and Student sides of the User Portal (see
`teacher_student_portal_implementation_guide.md`) a read-only "Calendar" entry showing the same
dual AD/BS month grid, and a Holiday/Events list, that the admin `/apps/calendar` page already has
— reusing that exact page and its existing endpoints. No new backend endpoint, no new frontend
page.

## Why this needed no new API

Every `Calendar` controller action the calendar page calls (`GetMonthView`, `GetYearView`,
`GetToday`, `GetCalendarEvents`, `GetFestivals`, `ConvertAdToBs`, `ConvertBsToAd`) was already
listed in `appsettings.json`'s `DefaultEnabledMenu` — any authenticated user can already call them,
with or without a menu claim. What was actually missing was purely a **menu-catalog/nav** gap:

1. The Teacher/Student portal shell (`PortalNavigation.jsx`) only ever renders the sub-menus of the
   **one** root main menu in the caller's claims tree (`claimsTree[0].children`). `Calendar`'s own
   admin sub-menu (`CALENDAR_VIEW`) lives under a *different* main menu (`CALENDAR_MANAGEMENT`), so
   even a Teacher/Student granted that claim would never see a portal sidebar item for it.
2. `DualCalendarPage` decides which of its three tabs (Month View / Events & Holidays / Festivals)
   to show by checking `hasClaim(MENU_CODES.CALENDAR_VIEW / CALENDAR_EVENT_LIST / FESTIVAL_LIST)`
   against the claims tree — independent of `DefaultEnabledMenu`, which only affects the backend
   authorization check, not what the frontend claims tree returns.
3. `RoleService.GetRoleMenuClaimsAsync`'s audience filter drops a granted menu from the claims tree
   entirely unless its own `MenuFor` is `Both` or matches the caller's resolved audience (`Admin` vs
   `User`). `Permission()`-built rows (every `PERMISSION`-type leaf, including
   `CALENDAR_EVENT_LIST`/`FESTIVAL_LIST`) never had a `menuFor` parameter before this change, so
   they all defaulted to `Admin` — meaning even a direct role-claim grant of `CALENDAR_EVENT_LIST`
   to a Teacher/Student role would have been silently filtered back out of their claims tree.

## What changed

- `MenuSeeder.Permission(...)` gained an optional `menuFor` parameter (mirroring `MainMenu`/
  `SubMenu`, which already had one) — defaults to `MenuAudience.Admin`, unchanged for every existing
  call site except the two below.
- `CALENDAR_EVENT_LIST` and `FESTIVAL_LIST` (both under the admin `CALENDAR_VIEW` sub-menu) are now
  seeded with `menuFor: MenuAudience.Both` — a festival in Nepal (Dashain, Tihar, ...) usually *is*
  the public holiday, so both are treated as one "Holiday and Events" concern. `CALENDAR_VIEW`
  itself was already `Both`.
- New `SubMenu("USER_PORTAL", "MY_CALENDAR", "Calendar", "/apps/calendar", null, "Calendar",
  "GetMonthView", 11, menuFor: MenuAudience.User)` — a portal-facing entry point into the existing
  `/apps/calendar` route. Shared by both Teacher and Student (the data isn't role-scoped), same
  reasoning as the existing shared-controller MY_* rows.
- `PortalNavigation.jsx`'s `PORTAL_ITEM_ICONS` map gained `MY_CALENDAR: CalendarOutlined`.

## What an admin still has to do

Menu-catalog rows only make a claim *grantable* — they don't grant it to anyone. To actually turn
this on for the Teacher and/or Student role, grant these codes via the existing Role → Claims
screen (`POST`/`PUT /api/roles/{roleId}/claims`), same manual step every other portal `MY_*` item
has always needed:

- `MY_CALENDAR` — makes the "Calendar" item appear in the portal sidebar.
- `CALENDAR_VIEW` — shows the Month View tab (the day grid with holidays/festivals/meetings joined).
- `CALENDAR_EVENT_LIST` — shows the Events & Holidays tab.
- `FESTIVAL_LIST` (optional) — shows the Festivals tab.

Without `CALENDAR_VIEW`/`CALENDAR_EVENT_LIST`/`FESTIVAL_LIST`, `MY_CALENDAR` alone still gets a
Teacher/Student to `/apps/calendar`, but `DualCalendarPage` renders zero tabs (`visibleTabs` would
be empty) — grant at least one of the three alongside it.

## Files touched

- `Infrastructure/Persistence/DataSeeder/MenuSeeder.cs` — `Permission()` signature, `CALENDAR_EVENT_LIST`/`FESTIVAL_LIST` audience, new `MY_CALENDAR` row.
- `ClientUI/src/layout/Dashboard/Drawer/DrawerContent/PortalNavigation.jsx` — icon mapping.
