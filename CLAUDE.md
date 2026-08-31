# CMSApp

Blog CMS backend built as a Clean Architecture solution on .NET 10, targeting PostgreSQL.

## Solution structure

`CMSApp.slnx` groups four projects under a `/src/` solution folder (virtual grouping only — the projects sit directly under the repo root on disk), referencing each other in one direction:

```
WebApi  -->  Infrastructure  -->  Application  -->  Domain
```

- **Domain** — entities, enums, auditing interfaces, repository *contracts* (`Interfaces/`), and shared domain-level types (`Common/`). No dependency on any other project. `ValueObjects/`, `Aggregates/`, `DomainServices/` exist as empty convention folders for when the domain grows past simple entities — don't force something into them prematurely.
- **Application** — use cases (`<Feature>/Commands`, `<Feature>/Queries`, `<Feature>/Validators`, `<Feature>/Dtos`), the cross-cutting contracts Infrastructure implements (`Common/Interfaces/`), and the shared API response envelope (`Common/Models/`). References `Domain` only.
- **Infrastructure** — EF Core persistence (`Persistence/`, including `Persistence/DataSeeder/`), ASP.NET Core Identity entities/config (`Identity/`, `Identity/EntityConfiguration/`), the `<Feature>Service` implementations that need Identity's managers (`Identity/Services/`), their DTO mappers (`Identity/Mapper/`), and small standalone helpers like the JWT generator (`Common/`). References `Application` (transitively `Domain`).
- **WebApi** — ASP.NET Core Web API host (`Controllers/`, `Middleware/`, `Program.cs`). References `Application` and `Infrastructure`.

Each `.csproj` targets `net10.0` with `<Nullable>disable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`. Keep new projects consistent with that.

Repo-level `Docs/` holds consumer-facing documentation: `UI-Implementation-Guide.md` (the frontend integration reference — response envelope, auth/token lifecycle, every endpoint with request/response shapes; **update it whenever an endpoint's route, request, or response shape changes** — it is the contract a UI team builds against) and `CancellationToken-Guide.md` (how request cancellation flows through the layers and the rules for new code). **Additionally, every new feature/endpoint change gets its own short per-feature guide** — `Docs/<feature>_implementation_guide.md`, lowercase snake_case filename (existing examples: `app_config_implementation_guide.md`, `menu_filter_implementation_guide.md`) — a focused, immediately-implementable page for the UI team: endpoints, request/response JSON, allowed values, failure table, typical UI uses. The per-feature guide complements (never replaces) the corresponding section in `UI-Implementation-Guide.md`; keep both in sync.

## Getting the app running (usage)

1. Set real values in `WebApi/appsettings.json` for anything still a placeholder (`Smtp:*`, `Authentication:Google:ClientId`) — DB connection string, `Jwt:Key`, and `Seed:*` credentials are dev-ready as committed.
2. Create and apply EF Core migrations (**user-owned step** — never done from here). Both the Identity tables and the domain tables live in one `ApplicationDbContext`, so it's a single migration chain.
3. `dotnet run` the `WebApi` project. On startup it runs six seeders (idempotent, safe on every boot; logged-and-skipped if the DB isn't migrated yet), in order: `IdentitySeeder` (roles + one account per role), `MenuSeeder` (menu/permission catalog + full grant to SuperAdmin — **synced**, see Menu & permission seeding below), `AppConfigSeeder` (baseline UI settings), `ConfigCatalogSeeder` (dropdown catalog types + near-universal options), `PayrollSeeder` (2026-07-15: a placeholder `FY-SAMPLE` fiscal year + illustrative Individual/Couple tax slabs; 2026-07-20 addition: a second fiscal year, `2084/85`, with the 1/10/20/27/29% slab structure the user actually configured and exercised — **still verify/replace before real payroll use**, see `Docs/payroll_implementation_guide.md`), and `SampleDataSeeder` (2026-07-13: development/demo school data — grade/section/subject dropdown options, one class per grade Nursery–Twelve with sections A/B and the Nepali-curriculum subject mapping for the current academic year, 20 teachers with qualifications/assignments/class-teachers, 100 students across 50 families with father+mother guardians, per-section enrollments, and grade 9–12 electives; idempotent by natural keys — admission no `ADM2026101`–`200`, employee no `EMP2026001`–`020`, guardian email; dataset reference in `Docs/sample_data_seeder_implementation_guide.md`; **remove its `Program.cs` call for production**).
4. Log in as the seeded superadmin (`Seed:SuperAdmin` credentials) — that account works end-to-end immediately, including every protected endpoint. Use it to create menus/roles/claims for everyone else. Swagger UI is at `/swagger` in Development.

**Database provider is PostgreSQL** (`Npgsql.EntityFrameworkCore.PostgreSQL`), not SQL Server — `ConnectionStrings:DefaultConnection` in `WebApi/appsettings.json` is a Postgres connection string. **Migrations are owned by the user, not created here** — don't run `dotnet ef migrations add`/`database update` or otherwise touch migration files unless explicitly asked.

## Domain layer conventions

- Auditing is modeled through interfaces in `Domain/Entities/Interfaces/`: `IAuditableEntity` (`CreatedBy`/`CreatedTs`/`UpdatedBy`/`UpdatedTs`) and `ISoftDeleteAuditableEntity : IAuditableEntity` (adds `IsDeleted`/`DeletedBy`/`DeletedTs`). `AuditableEntity`/`SoftDeleteAuditableEntity` are the plain-class implementations that domain entities inherit directly (`Menu : SoftDeleteAuditableEntity`). ASP.NET Core Identity entities implement the interface directly instead (can't multiple-inherit from `IdentityUser<T>`).
- **Audit stamping is automatic** — `ApplicationDbContext.SaveChangesAsync` stamps `CreatedBy`/`CreatedTs`/`UpdatedBy`/`UpdatedTs` on every tracked `IAuditableEntity`, and converts a `Remove()` on an `ISoftDeleteAuditableEntity` into a `Modified` update that sets `IsDeleted = true` + `DeletedBy`/`DeletedTs`, instead of an actual `DELETE`. **Never set these audit fields manually in a handler or repository** — the `DbContext` does it from `ICurrentUserService.UserName` (falls back to `"system"` when there's no authenticated caller).
- `Domain/Interfaces/IRepository<TEntity, TKey>` is the generic repository contract (`GetByIdAsync`, `GetAllAsync`, `GetPagedAsync` → `Domain/Common/PagedResult<TEntity>`, `AddAsync`, `Update`, `Remove`). Entity-specific repositories extend it (`IMenuRepository : IRepository<Menu, int>`) and add only the queries that don't fit the generic shape (`CodeExistsAsync`).
- Enums live in `Domain/Enums/` as plain `enum` types, stored by EF as their underlying `int`.
- Identity primary key type is `Guid` throughout — keep new identity-related types on `Guid`.

## Identity model (`Infrastructure/Identity/`)

| Entity | Base | Extra state |
|---|---|---|
| `ApplicationUser` | `IdentityUser<Guid>`, `ISoftDeleteAuditableEntity` | profile fields (`FirstName`, `Gender`, `UserType`, `Dob` — nullable, ...), per-user IP allowlist (`IsIpRestricted` + comma-separated `UserIpAllowed` — managed via `PUT /api/users/{id}`, **enforced as of 2026-07-12** at token issuance (login/Google/refresh) and on every authenticated request in `AuthorizedAction`, via the shared `Infrastructure/Common/IpAllowlistChecker`; see `Docs/user_ip_restriction_implementation_guide.md`), nav collections to tokens/roles/logins |
| `ApplicationRole` | `IdentityRole<Guid>`, `IAuditableEntity` | `Description`, nav collections to user-roles/role-claims |
| `ApplicationUserRole` | `IdentityUserRole<Guid>`, `IAuditableEntity` | nav to `ApplicationUser`/`ApplicationRole` |
| `ApplicationUserClaim` | `IdentityUserClaim<Guid>`, `IAuditableEntity` | `MenuId`/`Menu` — claims are menu/permission-scoped, not free-form |
| `ApplicationRoleClaim` | `IdentityRoleClaim<Guid>` | `MenuId`/`Menu`, nav to `ApplicationRole` — **not** auditable |
| `ApplicationUserLogin` | `IdentityUserLogin<Guid>`, `IAuditableEntity` | nav to `ApplicationUser` |
| `ApplicationUserToken` | `IdentityUserToken<Guid>` | `RefreshTokenExpiryDate`, `TokenRefreshTokenRevokedDate`, computed `IsExpired`/`IsRevoked` — **not** auditable. **These custom fields are no longer used** — refresh tokens now live in the dedicated `RefreshToken` entity below; this type still exists only because Identity's own `AddEntityFrameworkStores` needs *a* `TUserToken` type for its internal token storage. Note the table is currently always **empty**: email-confirmation/password-reset tokens are stateless `DataProtectorTokenProvider` tokens (validated cryptographically, never persisted); rows would only appear if MFA is added (authenticator key + recovery codes are stored here by `UserManager`) or if `SetAuthenticationTokenAsync` is used to store external-provider tokens |
| `RefreshToken` | `AuditableEntity` | **Not** an ASP.NET Core Identity type — a project-specific entity (`Id`, `UserId`, `Token`, `ExpiresAtUtc`, `RevokedAtUtc`, `ReplacedByToken`, computed `IsExpired`/`IsRevoked`/`IsActive`, nav to `ApplicationUser`). One row per issued token (not one row per user), which is what makes multi-device sessions possible — see Refresh tokens section below |

**Why `RefreshToken` lives in `Infrastructure/Identity/` and not `Domain/Entities/`:** it holds a navigation property to `ApplicationUser`, which is an Infrastructure type (it inherits `IdentityUser<Guid>` from an ASP.NET Core package Domain doesn't reference) — putting `RefreshToken` in Domain would force a Domain→Infrastructure reference, which inverts the dependency direction. More fundamentally, a refresh token is an *authentication session artifact*, not a business concept: nothing in the blog/CMS domain knows or cares about token rotation. The rule generalizes: an entity belongs in `Domain/Entities/` only if it's a business concept with no compile-time ties to Identity types; anything that references `ApplicationUser`/`ApplicationRole` directly must live in `Infrastructure/Identity/` beside them (it can still reuse Domain's `AuditableEntity` base — Infrastructure→Domain is the allowed direction).

The `Menu` entity (`Domain/Entities/Menu.cs`) doubles as the permission catalog: a claim's `MenuId` points at the menu/permission node it grants, instead of Identity's default free-text `ClaimType`/`ClaimValue` alone.

### Menu hierarchy (`MenuType`/`MenuFor`)

`Menu.MenuType`/`Menu.MenuFor` are plain `string` columns (not a C# `enum` — the DB column is untyped `string`, so changing this to a real enum would need a migration, which is off-limits here), but their allowed values are pinned down as constants rather than free-form: `Domain/Constants/MenuTypes.cs` (`MainMenu = "MAIN_MENU"`, `SubMenu = "SUB_MENU"`, `Permission = "PERMISSION"`, plus an `All` array) and `Domain/Constants/MenuAudience.cs` (`Admin`/`User`/`Both`, same shape). Always construct/compare against these constants, never inline the string literals.

`Menu` is a self-referencing tree via `ParentId`/`MainMenu`/`Childrens` (see `MenuConfiguration`'s self-referencing FK note above), and the three `MenuType` values are the tree's levels:

- `MAIN_MENU` — top-level, **must not** have a parent.
- `SUB_MENU` — **must** have a parent, and that parent must be a `MAIN_MENU`.
- `PERMISSION` — the leaf that `AuthorizedAction` actually checks against (`Menu.Controller`/`Menu.Action`) — **must** have a parent, and that parent may be **either** a `MAIN_MENU` or a `SUB_MENU`. Both `MAIN_MENU → SUB_MENU → PERMISSION` and the shorter `MAIN_MENU → PERMISSION` (skipping the sub-menu level) are valid.

`MenuService.CreateMenuAsync` enforces exactly this via a private `ValidateHierarchyAsync` (fetches the parent through `IUnitOfWork.Menus.GetByIdAsync` and checks its `MenuType`), returning `ResponseCodes.ValidationError` on violation — same pattern as the rest of the validation pipeline (validate → check invariants → mutate). `CreateMenuCommandValidator` separately restricts `MenuType`/`MenuFor` to `MenuTypes.All`/`MenuAudience.All` (the usual `NotEmpty()` in one `RuleFor`, `Must(...)` guarded by `.When(!string.IsNullOrEmpty(...))` in a second `RuleFor` — see the note below about why `.NotEmpty()` and a null-guarded `.Must()` can't share one chain). `UpdateMenuAsync` runs the same `ValidateHierarchyAsync` (now taking `(menuType, parentId)` primitives so create and update share it) plus a self-parent guard and a code-uniqueness re-check only when `Code` changes. `DeleteMenuAsync` is a **soft** delete (`Menu` is `SoftDeleteAuditableEntity`) refused with `Conflict` while the menu still has children — a hidden parent would orphan its visible children in every tree build (`IMenuRepository.HasChildrenAsync`). Corollary of soft delete: a deleted menu's `Code` stays reserved — the unique DB index on `code` still sees the soft-deleted row, so `CodeExistsAsync` uses `IgnoreQueryFilters()` (fixed 2026-07-12; it was previously subject to the `!IsDeleted` filter, letting the service check pass and the insert die at the database as a 500) and recreating a deleted code now fails cleanly with `Conflict` ("is already in use (possibly by a soft-deleted menu)").

**Heads up when editing `DefaultEnabledMenu`/`CriticalChanges` keys**: ASP.NET Core's default route derives the controller-name route value by stripping the `Controller` suffix and keeping the rest as-is (no singularization) — `MenusController` → `"Menus"`, `AuthController` → `"Auth"` — and `AuthorizedAction` compares against that raw route value, so a key that doesn't exactly match it silently never applies. This bit once: the section shipped with dead keys (`"Account"`, `"Role"`, `"Menu"`, `"Common"`) that matched no controller, which meant every non-SuperAdmin user got a 403 on `auth/logout` and `auth/change-password` until the keys were fixed (2026-07-10) to `"Auth": "Logout,ChangePassword,SetPassword"`, `"Roles": "GetUserRoles"`, `"Configs": "GetConfigsByTypeCode"`. When adding an entry, verify both halves against the real controller class and action method names.

**Any property backing a value type must actually be nullable if the EF config marks it optional** — `ApplicationUser.Dob` was originally a non-nullable `DateTime` while its config called `.IsRequired(false)`, which throws `InvalidOperationException` at model-build time (`DateTime` can't be "nullable/optional" without being `DateTime?`). Fixed by making the property `DateTime?`. Watch for this whenever a config calls `.IsRequired(false)` on a value-typed property.

## EF Core entity configuration pattern

Two shared generic base configurations in `Infrastructure/Persistence/EntityConfigurations/` do the auditing column mapping so it isn't repeated per entity: `AuditableEntityConfiguration<T>` and `SoftDeleteAuditableEntityConfiguration<T>` (adds `is_deleted`/`deleted_ts`/`deleted_by`, a global query filter `!IsDeleted`, and an index on `is_deleted`).

Concrete configurations live next to the entities they configure (`Infrastructure/Identity/EntityConfiguration/` for identity types, `Infrastructure/Persistence/EntityConfigurations/` for domain types) and follow this recipe:

1. Inherit the matching generic base **only if the entity implements the matching interface**; otherwise implement `IEntityTypeConfiguration<TEntity>` directly (`ApplicationRoleClaim`, `ApplicationUserToken`).
2. `override void Configure(...)` starts with `base.Configure(builder);` when there is a base to call.
3. `builder.ToTable("snake_case_plural_name", schema: "identity")` for identity tables; **no schema argument** for domain tables (`MenuConfiguration` has none).
4. Every mapped property gets an explicit `.HasColumnName("snake_case")`.
5. Composite keys for Identity join/link tables mirror ASP.NET Core Identity's own defaults: `ApplicationUserRole` on `(UserId, RoleId)`, `ApplicationUserLogin` on `(LoginProvider, ProviderKey)`, `ApplicationUserToken` on `(UserId, LoginProvider, Name)`.
6. Relationships are configured from the "many"/child side only — never duplicated from the parent side. **Always write the `HasOne(...).WithMany(...).HasForeignKey(...)` explicitly when the FK property name doesn't literally match the navigation name + `Id`** — e.g. `RefreshToken.UserId` with a nav named `ApplicationUser`: EF's convention only looks for `ApplicationUserId`, so without the explicit mapping it silently creates a *shadow* `ApplicationUserId` FK column and leaves `user_id` as unrelated data (`RefreshTokenConfiguration` documents this in a comment).
7. `LoginProvider`/`ProviderKey`/`Name` use `HasMaxLength(128)` (Identity's own default); other strings without a business rule are left unbounded.
8. Unique/lookup indexes get an explicit `HasDatabaseName("ix_<table>_<column>")`.
9. Self-referencing hierarchies (`Menu.ParentId` → `MainMenu`/`Childrens`) use `.OnDelete(DeleteBehavior.Restrict)` to avoid multiple-cascade-path errors.

New entity → add the entity + a sibling `*Configuration.cs`. Nothing else needs registering — see DbContext section below.

## DbContext (`Infrastructure/Persistence/ApplicationDbContext.cs`)

`ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid, ApplicationUserClaim, ApplicationUserRole, ApplicationUserLogin, ApplicationRoleClaim, ApplicationUserToken>` — **one combined context** for both Identity and domain aggregates (deliberately not split into a separate Identity context). Constructor takes `ICurrentUserService` for audit stamping (see above). `OnModelCreating` calls `base.OnModelCreating` then `builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly())` — every `IEntityTypeConfiguration<T>` in the `Infrastructure` assembly is picked up automatically, no manual registration.

Non-Identity `DbSet`s: `public DbSet<Menu> Menus => Set<Menu>();`.

## Application layer: response envelope (`Application/Common/Models/`)

**Every API response is wrapped — controllers never return a bare DTO, an anonymous object, or `dynamic`.**

- `CommonResponse<T>` — `ResponseCode`, `ResponseMessage`, `Data`. Build via `CommonResponse<T>.Success(data, message)` / `CommonResponse<T>.Fail(responseCode, message)`, never `new CommonResponse<T> { ... }` directly outside those factory methods.
- `ResponseCodes` — the string constants passed to `.Fail(...)` (`ValidationError`, `NotFound`, `Conflict`, `Unauthorized`, `Forbidden`, `TooManyRequests`, `ServerError`); add new codes here rather than inlining string literals.
- `PaginatedResponse<T>` — `Items`, `Page`, `PageSize`, `TotalCount`, plus computed `TotalPages`/`HasPreviousPage`/`HasNextPage`. List endpoints return `CommonResponse<PaginatedResponse<TDto>>`.
- **Before every `return` in a handler or controller, build a named local (`var response = ...;` / `var menuDto = ...;`) and return that** — don't construct the return value inline in the `return` statement. This applies to the response wrapper *and* to whatever `Data` holds: define a real class for it (a DTO under `<Feature>/Dtos/`), never an anonymous type.

`Application/Common/Interfaces/` holds the contracts Infrastructure implements: `IUnitOfWork` (`Menus` repository + `SaveChangesAsync`), `ICurrentUserService` (`UserId`/`UserName`/`IsAuthenticated`, read from `HttpContext.User` claims), `IIdentityService` (`GetUserNameAsync`, `IsInRoleAsync`, `CreateUserAsync` → `IdentityOperationResult`, `DeleteUserAsync`). `IIdentityService.CreateUserAsync` is a **minimal low-level primitive** — as of 2026-07-12 it takes username/email/password/firstName/lastName (the two name fields are `NOT NULL` in the DB, so creates through it now succeed), hardcodes `UserType.User`, and still doesn't cover roles/ToS/profile fields — `IUserService.CreateUserAsync` remains the full-featured path.

## Application layer: feature pattern (`Application/<Feature>/`)

**No MediatR, no per-operation handler classes.** Each feature is one `I<Feature>Service` / `<Feature>Service` pair — a single service class with one method per use case, injected by constructor and called directly from the controller. Commands/Queries still exist as plain input classes for operations with more than one parameter, but they're method *parameters*, not the trigger for a dedicated handler class.

`Application/Menus/` is the reference implementation, copy its shape for new features:

```
Menus/
├── Commands/
│   └── CreateMenuCommand.cs      # plain DTO-shaped input (multi-field create/update payloads)
├── Queries/
│   └── GetMenusQuery.cs          # plain DTO-shaped input (Page/PageSize, filters, ...)
├── Validators/
│   └── CreateMenuCommandValidator.cs   # FluentValidation AbstractValidator<TCommand>
├── Dtos/
│   └── MenuDto.cs                # outward-facing shape, never return the entity itself
├── MenuMapper.cs                 # static ToDto(entity) -> Dto, shared by every method on the service
├── IMenuService.cs               # CreateMenuAsync(command), GetMenuByIdAsync(id), GetMenusAsync(query)
└── MenuService.cs                # the one implementation, injected wherever the feature is needed
```

A single-field lookup (`GetMenuByIdQuery` for just an `Id`) doesn't get its own class — take the primitive (`int id`) directly as a method parameter instead. Only give an operation a dedicated Command/Query class once it has enough fields that a bare parameter list would be unwieldy (`CreateMenuCommand`, `GetMenusQuery`).

`MenuService` method responsibilities, in order: validate (`_createMenuCommandValidator.Validate(command)`, fail with `ResponseCodes.ValidationError` and every `ValidationResult.Errors` message joined via a private `BuildValidationErrorMessage` helper), check domain invariants via the repository (e.g. `CodeExistsAsync` → `ResponseCodes.Conflict`), mutate through `IUnitOfWork` (`AddAsync` + `SaveChangesAsync` — never call `DbContext` directly from a service), map to DTO via the feature's mapper, wrap in `CommonResponse<T>.Success(...)`.

Registration in `Application/DependencyInjection.cs` (`AddApplication()`) is one `services.AddScoped<IMenuService, MenuService>();` line per feature — validators are still picked up automatically via `AddValidatorsFromAssembly`, but the service interface/implementation pair needs its own explicit registration (no assembly scanning for services).

**Where the `<Feature>Service` implementation lives depends on what it's built on.** `MenuService` lives entirely in `Application/Menus/` because it's built on `IUnitOfWork`/`IMenuRepository` — abstractions Application already owns. `Application/Users/`, `Application/Roles/`, and `Application/Auth/` only hold the *contracts* (`IUserService`/`IRoleService`/`IAuthService`, their Commands/Queries/Validators/Dtos) — the implementations (`UserService`, `RoleService`, `AuthService`) live in `Infrastructure/Identity/Services/`, with their DTO mappers (`UserMapper`, `RoleMapper`, `RoleClaimMapper`) in `Infrastructure/Identity/Mapper/`, because they're built directly on `UserManager<ApplicationUser>`/`RoleManager<ApplicationRole>`/`SignInManager<ApplicationUser>`, and those Identity types are Infrastructure-only — Application can't reference them. Registration for these still happens the same way, just in `Infrastructure/DependencyInjection.cs` instead of `Application/DependencyInjection.cs` (alongside `ICurrentUserService`/`IIdentityService`). Rule of thumb: if the feature's persistence goes through the generic repository/`IUnitOfWork`, the service belongs in `Application`; if it goes through ASP.NET Core Identity's managers, the service belongs in `Infrastructure/Identity/Services/`.

## Coding style

- 4-space indentation, one class per file, file-scoped `namespace X { }` block (braces, not `namespace X;`) matching the folder path.
- Public auto-properties, no backing fields, `= new List<T>();` default for nav/collection properties.
- **No expression-bodied methods and no ad-hoc lambda-as-logic** in handlers/services/controllers — write full block-bodied methods with explicit `return` statements, and prefer `foreach` over `.Select()`/`.Where()` chains when building a result list (see `GetMenusQueryHandler` mapping `pagedMenus.Items` into DTOs via `foreach`, not LINQ). Framework-required delegate parameters (`AddDbContext<T>(options => ...)`, FluentValidation's `RuleFor(x => x.Prop)`, EF's `Where(x => x.Id == id)`) are the unavoidable exception — keep those to a single simple expression, don't chain further logic inside them.
- Object construction: object-initializer syntax is fine and preferred (`var entity = new Menu { Code = ..., DisplayName = ... };`), including for the response envelope's `Data` payload — just make sure the payload's type is a real class, not an anonymous type.
- Fluent EF configuration calls are chained one member per line, aligned under the receiver.
- Nullable reference types are disabled project-wide — don't add `?` on reference types or assume NRT warnings apply (but *do* make value-type properties nullable when the business rule is "optional", per the `Dob` note above).
- **CancellationToken everywhere** (full rationale + flow diagram in `Docs/CancellationToken-Guide.md`): every async interface method ends with `CancellationToken cancellationToken = default`; every controller action declares `CancellationToken cancellationToken` and forwards it; implementations pass it to every EF Core/SMTP call that accepts one; private async helpers take it as a *required* parameter. Identity's `UserManager`/`RoleManager`/`SignInManager` calls can't take one (no such overloads) — that's expected, not a bug. **Security cleanup after an irreversible step runs with `CancellationToken.None`** (revoking sessions after a password change, retiring the old token after refresh rotation) — a client disconnect must not be able to skip those. `ExceptionHandlingMiddleware` treats `OperationCanceledException` from a genuinely aborted request as an info-level non-event, not a 500.

## Infrastructure: repositories, unit of work, identity services

- `Infrastructure/Persistence/Repositories/Repository<TEntity, TKey>` — generic `IRepository<TEntity, TKey>` implementation over `ApplicationDbContext.Set<TEntity>()`. Entity-specific repositories (`MenuRepository`) inherit it and add only their extra query methods.
- `Infrastructure/Persistence/UnitOfWork.cs` — implements `IUnitOfWork`, lazily constructs repositories (`Menus` property, explicit null-check + assign, not `??=`), and delegates `SaveChangesAsync` to the `DbContext`. Add one property per aggregate repository as new features are built — don't add a generic `Repository<T>` escape hatch to `IUnitOfWork`.
- `Infrastructure/Identity/Services/CurrentUserService.cs` — implements `ICurrentUserService` off `IHttpContextAccessor`. `UserId` reads `ClaimTypes.NameIdentifier` first, then raw `sub` as a fallback — the JWT bearer middleware's default inbound claim mapping renames the token's `sub` claim to `ClaimTypes.NameIdentifier` before it reaches `HttpContext.User`, so reading only the raw JWT name silently returns null on every request (this exact bug once made `AuthorizedAction` 403 everything). `UserName` reads the custom `userName` claim, which the mapping leaves untouched. Requires `<FrameworkReference Include="Microsoft.AspNetCore.App" />` in `Infrastructure.csproj` (a plain class library needs that to see `IHttpContextAccessor`/`Microsoft.AspNetCore.Http` types). JWT bearer auth is now configured (see Authentication & Authorization below), so these claims are populated for any request carrying a valid token.
- `Infrastructure/Identity/Services/IdentityService.cs` — implements `IIdentityService` off `UserManager<ApplicationUser>`.
- `Infrastructure/DependencyInjection.cs` (`AddInfrastructure(services, configuration)`) — registers the `DbContext` (`UseNpgsql`, connection string from `ConnectionStrings:DefaultConnection`), `AddIdentityCore<ApplicationUser>().AddRoles<ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>()`, `IHttpContextAccessor`, and the `ICurrentUserService`/`IIdentityService`/`IUnitOfWork` implementations. Called from `WebApi/Program.cs` alongside `AddApplication()`.

## WebApi

- `Controllers/` — one controller per aggregate/feature (`MenusController`), constructor-injects the feature's `I<Feature>Service` directly (no base controller, no mediator). Every action returns `ActionResult<CommonResponse<T>>`; branch on `response.ResponseCode` to pick `Ok`/`BadRequest`/`NotFound` — the service already decided success/failure, the controller just picks the HTTP status.
- `Middleware/ExceptionHandlingMiddleware.cs` — catches unhandled exceptions, logs them, and writes a `CommonResponse<object>.Fail(ResponseCodes.ServerError, ...)` JSON body with a 500 status, instead of leaking the default ASP.NET Core error page/stack trace. Registered first in the pipeline (`app.UseMiddleware<ExceptionHandlingMiddleware>();`, before `UseOpenApi`/`UseHttpsRedirection`) so it wraps everything, including DI activation failures inside a controller.
- `Middleware/SecurityHeadersMiddleware.cs` (2026-07-12) — stamps `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` on every response; registered right after the exception middleware. Deliberately no CSP (would break Swagger UI, and a JSON API serves no other HTML). `UseHsts()` runs outside Development.
- **CORS is an explicit allowlist** (2026-07-12) — `Program.cs` reads `App:AllowedOrigins` (defaults: `http://localhost:3000`, `http://localhost:5173`) and only calls `UseCors` when the list is non-empty; an empty list means no cross-origin access (fail-closed — never reintroduce `SetIsOriginAllowed(origin => true)` alongside `AllowCredentials()`). Per-environment frontend origins go in that config section. See `Docs/security_hardening_implementation_guide.md`.
- `Program.cs` calls `builder.Services.AddApplication()` and `builder.Services.AddInfrastructure(builder.Configuration)` before `builder.Build()`.
- OpenAPI/Swagger is done via **NSwag** (`NSwag.AspNetCore`, `AddOpenApiDocument` / `UseOpenApi` / `UseSwaggerUi`) — deliberately not `Microsoft.AspNetCore.OpenApi`/`Microsoft.OpenApi` or Swashbuckle, switched because of a vulnerable/deprecated `Microsoft.OpenApi` transitive version. Don't reintroduce those packages. `AddOpenApiDocument` also registers a `"JWT"` API-key security scheme (`Authorization` header) with `AspNetCoreOperationSecurityScopeProcessor` so Swagger UI's "Authorize" button works — paste `Bearer {token}`.

## Authentication & authorization

The user-management implementation follows two written guides the user provided (`UserManagement-Implementation.md` and `ASP.NET-Core-Identity-Implementation-Guide.md`) fairly closely; deviations from them are called out explicitly below since they were deliberate engineering judgment calls, not oversights.
