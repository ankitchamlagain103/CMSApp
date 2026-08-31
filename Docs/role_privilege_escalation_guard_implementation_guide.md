# CMSApp — Role/Menu Access Control Hardening, Dashboard Widgets & Per-User Overrides (UI)

**2026-08-07.** Four related changes shipped the same day, all building on the same
role→menu-claim data: a privilege-escalation guard on role/claim assignment, audience-based
filtering of the menu tree (`GET /api/roles/user-menus`), a generic dashboard widget registry
built on top of both, and (Part 4, same day, follow-up) per-user menu overrides. Parts 1, 2, and 4
don't change actual API authorization surface area beyond what they explicitly add — Part 4 is the
one that *does* touch `AuthorizedAction` (the real per-request gate), since a "user-specific menu"
that only showed in the sidebar without actually being callable would be worse than not building
it. Part 3 is a new read endpoint.

## Part 1 — Privilege-escalation guard

Before this round, the only gate on `POST /api/roles/claims` (grant a menu to a role) and
`POST /api/roles/users` (assign a role to a user) was the *caller's own* permission row for those
two actions (`AssignMenuToRole`/`AssignRoleToUser`). Nothing checked whether the caller already
held what they were about to hand out — an Admin granted only the `AssignMenuToRole` permission
could grant any role every permission in the system, including ones the Admin themselves never
had. This closes that gap.

### What changed

Both `RoleService.AssignMenuToRoleAsync` and `RoleService.AssignRoleToUserAsync` now compute the
caller's own **effective granted menu set** — the union of every `Menu` granted to any role the
caller currently holds — and reject the request if what's being granted isn't already covered by
that set. **SuperAdmin is exempt** (same bypass reasoning as `AuthorizedAction`'s own SuperAdmin
check, and read from the database via `ICurrentUserService.UserId` → `ApplicationUser.UserType`,
never a token claim — a demoted SuperAdmin loses this bypass on their very next request).

- **`POST /api/roles/claims`**: the target `menuId` must be in the caller's own granted-menu set.
  Otherwise: `400 FORBIDDEN` — `"You cannot grant '<menu display name>' because you do not hold it
  yourself."`
- **`POST /api/roles/users`**: **every** `MenuId` the target role currently grants (via its
  `ApplicationRoleClaim` rows) must be in the caller's own granted-menu set. A role with zero
  claims (a freshly created empty role, or the seeded zero-permission `Student` role) always
  passes — there's nothing to escalate. Otherwise: `400 FORBIDDEN` — `"You cannot assign this role
  because it grants permissions you do not hold yourself: <comma-separated menu display names>."`

Both are **reject-the-whole-action** — there's no partial grant/assign, matching this codebase's
existing all-or-nothing validation style elsewhere (role creation, bulk endpoints).

### Why role creation itself needed no change

`POST /api/roles` only ever creates an empty `ApplicationRole` (`Name`/`Description`) — it never
grants claims in the same call. The actual escalation vector is entirely in
`AssignMenuToRoleAsync`, so a caller creating a brand-new role and then granting it permissions
one at a time via `POST /api/roles/claims` is already covered by the guard above; no separate
check was needed on the create endpoint itself.

### UI implications

- The role-permission editor (checkbox tree against `GET /api/roles/{roleId}/claims`) should
  expect a non-SuperAdmin admin to get `400 FORBIDDEN` on any checkbox for a permission they
  themselves don't hold. Consider disabling those checkboxes client-side using the caller's own
  `GET /api/roles/user-menus` tree as the ceiling, to avoid a round-trip just to learn a checkbox
  is off-limits — this is a UX nicety, not a security boundary (the guard is the actual boundary).
- The "assign role to user" picker can pre-filter the role dropdown the same way: a role is
  assignable by the current admin only if every one of its granted menus is also in the admin's
  own `user-menus` tree.
- SuperAdmin's own `user-menus` tree already includes everything (menu-sync grants SuperAdmin
  every menu with a non-empty `Controller`), so this filtering is a no-op for that account.

## Part 2 — Menu audience filtering (`GET /api/roles/user-menus`)

`Menu.MenuFor` (`ADMIN`/`USER`/`BOTH`, `Domain/Constants/MenuAudience.cs`) has existed since the
menu catalog was built, but was previously **dead weight for access purposes** — only used to
filter the admin's own menu-management CRUD grid, never consulted when building a user's actual
nav tree. It's now enforced there.

### Why this isn't a straight `UserType` filter

`ApplicationUser.UserType` (`SuperAdmin`/`Admin`/`User`) and `Menu.MenuFor` don't map 1:1. Every
Employee self-service login (Teacher, Accountant, HR, Principal — provisioned via the portal
account feature) is hardcoded `UserType.User`, same as a Student login — but an Employee account
needs to see `MY_WORKSPACE` (an `ADMIN`-tagged menu subtree) while a Student account shouldn't see
admin screens at all. Filtering on the raw `UserType` value can't tell those two apart.

### What "audience" actually means here

`RoleService.ResolveMenuAudienceAsync` derives an audience from **account linkage**, not the raw
claim:

1. `UserType.SuperAdmin` or `UserType.Admin` → `ADMIN` audience.
2. `UserType.User` **and** linked to an `Employee` row (`Employee.UserId == user.Id`) → `ADMIN`
   audience (self-service staff logins use the same admin-panel API surface).
3. Everything else (a Student-linked account, or an unlinked `User`-type account) → `USER`
   audience.

`GetUserRolesAsync` resolves this once per call and passes it into `GetRoleMenuClaimsAsync`, which
now filters the caller's **directly granted** menus down to `MenuFor == BOTH || MenuFor ==
callerAudience` *before* walking the ancestor chain — filtering before the ancestor walk (rather
than after building the tree) means an audience-mismatched grant is dropped cleanly and can never
orphan itself by losing a filtered-out parent; the walk still includes every real ancestor of a
kept menu regardless of the ancestor's own tag, since ancestors are structural tree nodes, not
separate grants.

### Practical effect today

Every menu in the seeded catalog is currently tagged `ADMIN` (`MenuSeeder` hardcodes
`MenuFor = MenuAudience.Admin` on every row). So today this is a **no-op for every SuperAdmin/
Admin/Employee-linked account** (their audience is always `ADMIN`, matching every menu) and a
**defense-in-depth guard for Student accounts** — which already see an empty tree today anyway,
since the seeded `Student` role ships with zero grants (see the Portal Account Provisioning
section of `Docs/UI-Implementation-Guide.md`). The filter starts actively mattering the day a real
Student-portal menu is added and tagged
`MenuFor = USER` — it (and only it, plus anything tagged `BOTH`) will be the only thing a Student
account's tree can ever surface, no matter what gets granted to their role by mistake.

### What this does *not* change

`AuthorizedAction` (the actual per-request API authorization gate) is completely untouched —
it still resolves access purely from `ApplicationRoleClaim`/`Menu.Controller`/`Menu.Action`, with
no `UserType` or `MenuFor` involvement beyond its existing SuperAdmin bypass. This round only
changes what `GET /api/roles/user-menus` *returns* for rendering a sidebar — an API call to an
endpoint the caller's role has a real claim for still succeeds even if that menu got filtered out
of their nav tree for an audience mismatch. If that combination ever needs to be a hard block too,
it's a separate, deliberate change to `AuthorizedAction`, not implied by this one.

## Part 3 — Generic dashboard widget registry (`GET /api/dashboard/widgets`)

Building a new persona dashboard used to mean a new bespoke composite service method + DTO +
controller action + permission row + menu row, every time (`GetSummaryAsync`,
`GetAccountsSummaryAsync`, `GetHrSummaryAsync`, `EmployeeService.GetMyDashboardAsync` were each
built this way). This adds a **generic** alternative: one endpoint that returns exactly the
widgets the caller's role currently grants, with no new endpoint needed per future persona — only
a new provider registration.

### How it works

1. `Menu` gained `isDashboardWidget` (bool) — same admin-curated flag shape as `isQuickLink`,
   settable via the existing `POST`/`PUT /api/menus`. Currently flagged on `DASHBOARD_SUMMARY`,
   `DASHBOARD_ACCOUNTS_SUMMARY`, `DASHBOARD_HR_SUMMARY`, and `MY_DASHBOARD`.
2. `GET /api/dashboard/widgets?take=` (permission `DASHBOARD_WIDGETS`) resolves the caller's own
   granted-and-audience-filtered menu tree (Part 2's `GetUserRolesAsync`, so a widget respects the
   exact same grant + audience rules a real nav item does), walks it for every
   `isDashboardWidget` node, and — for each one whose `code` matches a registered
   `IDashboardWidgetProvider` (`Application/Dashboard/Widgets/`) — invokes that provider and
   returns its result.
3. A provider is a thin adapter: it calls the *same* existing service method its dedicated
   endpoint already calls (`IDashboardService.GetAccountsSummaryAsync`, `IEmployeeService.GetMyDashboardAsync`,
   ...) and repackages the response. There is exactly one implementation of each widget's real
   logic — the provider never computes anything new.

### Response shape

```jsonc
{
  "responseCode": "SUCCESS",
  "responseMessage": "Request processed successfully.",
  "data": [
    {
      "widgetCode": "MY_DASHBOARD",
      "displayName": "My Dashboard",
      "success": true,
      "message": "Request processed successfully.",
      "data": { /* EmployeeDashboardDto -- see employee_self_service_implementation_guide.md */ }
    },
    {
      "widgetCode": "DASHBOARD_ACCOUNTS_SUMMARY",
      "displayName": "View Accounts Dashboard Summary",
      "success": true,
      "message": "Request processed successfully.",
      "data": { /* AccountsDashboardSummaryDto */ }
    }
  ]
}
```

`data[].data` is intentionally polymorphic per widget — the frontend must already know how to
render each `widgetCode`'s shape (this mechanism only makes the *set of widgets shown* dynamic per
role, not the rendering itself). A widget whose underlying call failed for this caller (e.g.
`MY_DASHBOARD` for a non-Employee-linked account) still appears in the array with `success: false`
and the same message its dedicated endpoint would have returned — filter on `success` before
rendering. A menu flagged `isDashboardWidget` with no matching registered provider is silently
omitted (not an error) — expected only for a brand-new widget flag added before its provider
ships.

### Existing dedicated endpoints are unchanged

`GET /api/dashboard/summary`, `/accounts-summary`, `/hr-summary`, and
`GET /api/employees/me/dashboard` all still work exactly as before — this is an *additional*
surface, not a replacement. Use whichever fits: the dedicated endpoint for a page that only ever
shows that one widget, `/widgets` for a role-agnostic dashboard shell that renders whatever comes
back.

### Adding a new widget going forward

1. Flag the relevant `Menu` row `isDashboardWidget: true` (via `PUT /api/menus/{id}` or
   `MenuSeeder`).
2. Add one class implementing `IDashboardWidgetProvider` in `Application/Dashboard/Widgets/`
   wrapping whatever service method already backs that capability, with `WidgetCode` matching the
   menu's `Code`.
3. Register it: `services.AddScoped<IDashboardWidgetProvider, YourNewWidgetProvider>();` in
   `Application/DependencyInjection.cs`.

No new route, permission, or DTO is required unless the widget's own underlying capability is
itself brand new.

## Part 4 — Per-user menu overrides (2026-08-07, same day)

Before this, the *only* way to grant access was through a role — every user sharing a role saw
exactly the same menus. This adds a second, narrower lever: grant (or later revoke) one specific
menu to one specific user, on top of whatever their roles already give them.

### The mechanism already existed in the schema, unused

`ApplicationUserClaim` (`identity.application_user_claims` — `UserId` + `MenuId`, same shape as
`ApplicationRoleClaim`) has existed since this project's Identity model was first built, but
nothing ever read or wrote it — `AuthorizedAction` only ever consulted `UserRoles → RoleClaims →
Menus`. This round wires it up as an **additive** layer, never a substitute for role grants.

### New endpoints (mirror `RolesController`'s claim routes exactly, user-scoped instead of role-scoped)

```
GET    /api/users/{id}/claims                a user's direct menu grants
POST   /api/users/claims                      { "userId": "…", "menuId": 5 }
DELETE /api/users/{id}/claims/{menuId}        remove a direct grant
```

Permissions `USER_CLAIM_LIST`/`USER_CLAIM_ASSIGN`/`USER_CLAIM_REMOVE` (under `USER_LIST`).
`AssignMenuToUserAsync` is subject to the **exact same privilege-escalation guard** as
`AssignMenuToRoleAsync` (Part 1) — a non-SuperAdmin caller can only grant a menu they already hold
themselves, `400 FORBIDDEN` otherwise. Response shapes and failure cases (`404` unknown user/menu,
`409` already-assigned) mirror `POST/DELETE/GET /api/roles/{roleId}/claims` one-for-one — see that
section of `UI-Implementation-Guide.md` for the exact JSON shape, just with `userId` instead of
`roleId`.

### Where "additive" is actually enforced (both places, not just one)

- **`GET /api/roles/user-menus`** (`RoleService.GetRoleMenuClaimsAsync`) — a user's granted-menu
  set is now `(their roles' RoleClaims) ∪ (their own UserClaims)`, computed before the ancestor
  walk and the Part 2 audience filter (both apply identically to a direct grant as to a role
  grant). A user with **zero roles** but a direct claim now gets a real, non-empty tree — the old
  "`roleIds.Count == 0` → empty tree" short-circuit is gone.
- **`AuthorizedAction.ValidateUserRoleClaimsAsync`** — same union, same removed short-circuit.
  This is the part that makes a user-level grant a *real* permission, not just a sidebar entry:
  the underlying API call actually succeeds now, purely off the direct grant, even with no role
  at all.

### Deliberately not built (yet)

**No "deny" semantics** — this can only ever add a menu a user's role(s) don't already grant, not
subtract one they do. A per-user *revocation* of something their role grants would need to define
an ordering/precedence rule across a user's several roles and their own override, which is a
meaningfully different (and more error-prone) feature; nothing here needed it, so it wasn't built.
If a specific person shouldn't have something their role grants everyone else, the fix is still a
role change (a narrower role, or move them off the shared role), not a per-user deny.

## No migration needed for Parts 1, 2, and 4; Part 3 needs one

Parts 1, 2, and 4 are pure service-layer logic over existing `ApplicationRoleClaim`/
`ApplicationUserClaim`/`ApplicationUserRole`/`Menu`/`Employee` data — `application_user_claims`
already exists in the schema (it's `IdentityDbContext`'s built-in `TUserClaim` table, present
since the very first Identity migration), just previously unused. No schema change for any of
these three. Part 3 needs one new column:
`dbo.menus.is_dashboard_widget boolean NOT NULL DEFAULT false` — until it exists, every `Menu`
read/write 500s (EF selects the mapped column), same caveat every `Menu` column addition in this
codebase carries.
