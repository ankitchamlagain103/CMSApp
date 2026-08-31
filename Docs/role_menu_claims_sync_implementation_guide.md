# Role menu-claims sync + role audience (`UserType`) — 2026-08-24

Fixes a real bug reported against the Role edit screen (`GET /api/Roles/{id}` shown alongside a
screenshot of unchecking a menu in the permission tree and saving): **unchecking a menu did not
remove its claim from the role.** Root cause, confirmed by reading the API surface: there was never
an endpoint that lets a caller say "here is the complete desired set of menus for this role" —
only two granular ones, `POST /api/roles/claims` (assign one) and
`DELETE /api/roles/{roleId}/claims/{menuId}` (remove one). A "Save" action on a permission-tree
editor has to diff the checkbox state itself and fire one call per changed row; when that diff
step is skipped or only partially wired up on the frontend (as it evidently was here — the network
tab only showed `PUT /api/roles/{id}` firing, which only updates `name`/`description`, never
`ROLE_CLAIM_ASSIGN`/`ROLE_CLAIM_REMOVE`), unchecked menus never get their `DELETE` call and the
claim survives.

This round adds a single **full-replace sync** endpoint so a permission-tree "Save" button only
ever needs to make one call, removing the class of bug entirely (there's no longer a
per-checkbox diff to forget). It also adds a `userType` field to `Role` so the frontend can filter
which menus it even offers to a given role, addressed in its own section below.

## `PUT /api/roles/{roleId}/claims` — sync a role's menu claims (new)

**This is the endpoint your permission-tree "Save" button should call — send the complete set of
checked menu ids every time, not just what changed.** The two existing granular endpoints
(`POST /api/roles/claims`, `DELETE /api/roles/{roleId}/claims/{menuId}`) are unchanged and still
work for a single toggle-one-checkbox-immediately UI, but they are no longer the recommended path
for a batch "Save" action — this new endpoint is.

**Request**:
```json
{ "menuIds": [4, 5, 12, 18] }
```
`menuIds` may be empty (`[]`) to clear every claim from the role. Duplicate ids in the array are
silently de-duplicated.

**Behavior**: loads the role's current claims, diffs them against the submitted `menuIds` in
memory, adds whatever is missing and removes whatever is no longer present, and commits both sides
of the diff in **one** `SaveChangesAsync` call — so a save is atomic (never a state where some
additions landed but a removal didn't). Returns the role's final claim list after the sync.

**Response** (`200`):
```json
{
  "responseCode": "SUCCESS",
  "responseMessage": "Role menu claims synced successfully.",
  "data": [
    { "id": 12, "roleId": "c2222222-0000-0000-0000-000000000001", "menuId": 4, "menuCode": "USER_LIST", "menuDisplayName": "View Users" },
    { "id": 15, "roleId": "c2222222-0000-0000-0000-000000000001", "menuId": 5, "menuCode": "ROLE_LIST", "menuDisplayName": "Roles & Permissions" }
  ]
}
```
Same `RoleClaimDto` shape as `GET /api/roles/{roleId}/claims` / `POST /api/roles/claims`.

**Failures**:
- `404 NOT_FOUND` — role doesn't exist, or one or more submitted `menuIds` don't exist (message
  names the missing ids).
- `400 VALIDATION_ERROR` — `menuIds` null, or contains a value `<= 0`.
- `400 FORBIDDEN` — **same privilege-escalation guard as `POST /api/roles/claims`**: a
  non-SuperAdmin caller can only grant menus they hold themselves. Only checked against the menus
  being *added* by this call — removing a claim never hands out a permission, so it's never
  escalation-checked. Message: `"You cannot grant the following menus because you do not hold
  them yourself: <names>."`

**Frontend migration**: replace whatever per-checkbox `POST`/`DELETE` diffing logic exists on the
role-permissions Save action with one `PUT /api/roles/{roleId}/claims` call carrying every
currently-checked menu id. This is the actual fix for the reported bug — it removes the
opportunity for an unchecked box's `DELETE` call to be missed.

Seeded permission: `ROLE_CLAIM_SYNC` (under `ROLE_LIST`, same tier as `ROLE_CLAIM_ASSIGN`/
`ROLE_CLAIM_REMOVE`) — grant it to any non-SuperAdmin role that manages other roles' permissions.

## `Role.UserType` — role audience, for filtering the menu picker (new)

Added a `userType` field to `Role` (`RoleDto`, `CreateRoleCommand`, `UpdateRoleCommand`) so a role
can be tagged with which audience it's meant for, letting the frontend filter which menus it
offers in that role's permission-tree editor.

**Not `Domain.Enums.UserType`** (the `SuperAdmin`/`Admin`/`User` account-type enum) — this reuses
the existing `Domain.Constants.MenuAudience` catalog instead (`ADMIN` / `USER` / `BOTH`), the same
one `Menu.MenuFor` already uses to tag individual menus. A role's `userType` is a hint for which
`Menu.MenuFor` values make sense to show when editing that role's claims — e.g. a `Teacher`/
`Student`-facing role tagged `USER` should only offer `USER`/`BOTH` menus in its picker, while an
`Admin`/`Accountant`/`HR` role tagged `ADMIN` should only offer `ADMIN`/`BOTH` menus. Named
`userType` (not `menuAudience`) per explicit product request — don't confuse it with the unrelated
`ApplicationUser.UserType` enum property.

**This is purely a frontend filtering aid.** `AuthorizedAction` never reads `Role.UserType` and it
does not gate anything server-side — the existing `Menu.MenuFor` audience check
(`RoleService.GetUserRolesAsync` → `ResolveMenuAudienceAsync`) already controls what actually ends
up in a *user's* nav tree at login time, based on the user's own account linkage, not their role's
tag. `Role.UserType` only helps a permissions-editor screen decide what to display as pickable.

**`POST /api/roles`** — `userType` is optional; omitted/empty defaults to `BOTH`. Validated against
`ADMIN`/`USER`/`BOTH` when supplied (`400 VALIDATION_ERROR` otherwise).

**`PUT /api/roles/{id}`** — `userType` is optional; omitted/empty **leaves the role's current
value unchanged** (same "null = unchanged" convention as `UpdateUserCommand.RoleIds` and other
three-way-sync fields in this codebase). Supply it explicitly to change it.

**`RoleDto`** gained `userType`:
```json
{ "id": "c2222222-0000-0000-0000-000000000001", "name": "Editor", "description": "Can edit content", "userType": "ADMIN" }
```

**Needs a migration** — new column `identity.application_roles.user_type varchar(20) NOT NULL
DEFAULT 'BOTH'`. Until it exists, every `Role` create/read/update 500s (EF selects the mapped
column), same caveat every other new-column round in this codebase carries (`is_quick_link`,
`is_dashboard_widget`, ...). Existing roles get `BOTH` as their default value at the database
level, so nothing needs a manual backfill for the column to exist correctly.

## Round 2 (2026-08-24, same day): Teacher self-service is `USER` audience, not `ADMIN`

Found while testing the round above: `RoleService.ResolveMenuAudienceAsync` treated any
Employee-linked login (Teacher, Accountant, HR, ...) as `ADMIN` audience, and `MY_WORKSPACE`'s
whole menu tree (`MenuSeeder.BuildMenuCatalog`) was tagged `MenuAudience.Admin` to match. Per
explicit correction: **Teacher and Student are both plain `UserType.User` accounts and both belong
on the `USER` side of the audience split — not `ADMIN`.**

Fixed in two places that have to move together:

1. **`RoleService.ResolveMenuAudience`** (renamed from `ResolveMenuAudienceAsync` — no longer
   needs to be async, the Employee-linkage DB lookup it used to make is gone): now only
   `UserType.SuperAdmin`/`UserType.Admin` resolve `ADMIN` audience; every other account —
   including an Employee-linked self-service login — resolves `USER` audience. **Accepted
   trade-off**: an Employee later promoted to `UserType.Admin`/`SuperAdmin` (e.g. a Principal who
   also administers the panel) would resolve `ADMIN` audience and lose `MY_WORKSPACE` from their
   nav tree. No such account exists in this codebase's seed/sample data today; revisit if one is
   ever created.
2. **`MenuSeeder.BuildMenuCatalog`** — `MY_WORKSPACE` and all six of its children (`MY_DASHBOARD`,
   `MY_PROFILE`, `MY_LEAVE`, `MY_PAYSLIPS`, `MY_CLASSES`, `MY_STUDENTS`) retagged
   `menuFor: MenuAudience.User`, matching `STUDENT_PORTAL`'s existing tagging. The sync pass
   (`MenuService`'s pass 2, `menu.MenuFor = definition.MenuFor;`) rewrites this into the DB
   automatically on next boot — confirmed against the live dev DB, no manual data fix needed
   for the menu rows themselves once the app restarts with this code.
3. **`IdentitySeeder.SeedRoleAsync`** gained an optional `userType` parameter; the seeded
   `Student` role now passes `MenuAudience.User` explicitly instead of falling through to the
   column's `BOTH` default, so a **fresh** database seeds it correctly without a manual fix. The
   `Teacher` role isn't seeded code (it's an admin-created role via `POST /api/roles`, per this
   codebase's "no hardcoded department roles" convention) — its `userType` was corrected directly
   in the dev DB (`ADMIN` → `USER`), same for the pre-existing `Student` role row (`BOTH` → `USER`)
   since both already existed before this round's IdentitySeeder fix could apply.

**Immediate dev-DB sync applied by hand** (same values the seeder will write on next boot, applied
now via `psql` so testing doesn't have to wait for a restart):
```sql
UPDATE dbo.menus SET menu_for = 'USER'
  WHERE code IN ('MY_WORKSPACE','MY_DASHBOARD','MY_PROFILE','MY_LEAVE','MY_PAYSLIPS','MY_CLASSES','MY_STUDENTS');

UPDATE identity.application_roles SET user_type = 'USER' WHERE name IN ('Teacher','Student');
```

**The `RoleService.cs` code fix still needs the running `dotnet run` process restarted** to take
effect — the DB values above are correct now, but `ResolveMenuAudience`'s logic only changes once
the app reloads the new build.

No new migration this round — `identity.application_roles.user_type` already existed (applied on
your end after the previous round).

## Summary of code changes

- `Infrastructure/Identity/ApplicationRole.cs` — new `UserType` property (string).
- `Infrastructure/Identity/EntityConfiguration/ApplicationRoleConfiguration.cs` — maps
  `user_type`, `HasMaxLength(20)`, `HasDefaultValue(MenuAudience.Both)`.
- `Application/Roles/Dtos/RoleDto.cs`, `Commands/CreateRoleCommand.cs`,
  `Commands/UpdateRoleCommand.cs` — `UserType` field.
- `Application/Roles/Validators/CreateRoleCommandValidator.cs` /
  `UpdateRoleCommandValidator.cs` — `UserType` validated against `MenuAudience.All` when supplied.
- `Application/Roles/Commands/SyncRoleMenuClaimsCommand.cs` (new),
  `Application/Roles/Validators/SyncRoleMenuClaimsCommandValidator.cs` (new).
- `Application/Roles/IRoleService.cs` — new `SyncRoleMenuClaimsAsync`.
- `Infrastructure/Identity/Services/RoleService.cs` — `SyncRoleMenuClaimsAsync` implementation;
  `CreateRoleAsync`/`UpdateRoleAsync` set/preserve `UserType`.
- `Infrastructure/Identity/Mapper/RoleMapper.cs` — maps `UserType`.
- `WebApi/Controllers/RolesController.cs` — new `PUT {roleId}/claims` action.
- `Infrastructure/Persistence/DataSeeder/MenuSeeder.cs` — new `ROLE_CLAIM_SYNC` permission row
  (`Roles`/`SyncRoleMenuClaims`, order 11 under `ROLE_LIST`).
