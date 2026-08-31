using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Roles;
using Application.Roles.Commands;
using Application.Roles.Dtos;
using Application.Roles.Queries;
using Application.Roles.Validators;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using FluentValidation.Results;
using Infrastructure.Identity.Mapper;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Services
{
    public class RoleService : IRoleService
    {
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CreateRoleCommandValidator _createRoleCommandValidator;
        private readonly UpdateRoleCommandValidator _updateRoleCommandValidator;
        private readonly AssignMenuToRoleCommandValidator _assignMenuToRoleCommandValidator;
        private readonly AssignRoleToUserCommandValidator _assignRoleToUserCommandValidator;
        private readonly SyncRoleMenuClaimsCommandValidator _syncRoleMenuClaimsCommandValidator;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public RoleService(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            CreateRoleCommandValidator createRoleCommandValidator,
            UpdateRoleCommandValidator updateRoleCommandValidator,
            AssignMenuToRoleCommandValidator assignMenuToRoleCommandValidator,
            AssignRoleToUserCommandValidator assignRoleToUserCommandValidator,
            SyncRoleMenuClaimsCommandValidator syncRoleMenuClaimsCommandValidator,
            ApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _createRoleCommandValidator = createRoleCommandValidator;
            _updateRoleCommandValidator = updateRoleCommandValidator;
            _assignMenuToRoleCommandValidator = assignMenuToRoleCommandValidator;
            _assignRoleToUserCommandValidator = assignRoleToUserCommandValidator;
            _syncRoleMenuClaimsCommandValidator = syncRoleMenuClaimsCommandValidator;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<CommonResponse<RoleDto>> CreateRoleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createRoleCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<RoleDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var roleAlreadyExists = await _roleManager.RoleExistsAsync(command.Name);
            if (roleAlreadyExists)
            {
                var conflictMessage = "Role '" + command.Name + "' already exists.";
                var conflictResponse = CommonResponse<RoleDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            var userType = string.IsNullOrEmpty(command.UserType) ? MenuAudience.Both : command.UserType;
            var role = new ApplicationRole
            {
                Name = command.Name,
                Description = command.Description,
                UserType = userType
            };

            var createResult = await _roleManager.CreateAsync(role);
            if (!createResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in createResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var createFailureResponse = CommonResponse<RoleDto>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return createFailureResponse;
            }

            var roleDto = RoleMapper.ToDto(role);
            var successResponse = CommonResponse<RoleDto>.Success(roleDto, "Role created successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<RoleDto>> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                var notFoundMessage = "Role with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<RoleDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var roleDto = RoleMapper.ToDto(role);
            var successResponse = CommonResponse<RoleDto>.Success(roleDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<RoleDto>>> GetRolesAsync(GetRolesQuery query, CancellationToken cancellationToken = default)
        {
            var totalCount = await _roleManager.Roles.CountAsync(cancellationToken);
            var skipCount = (query.Page - 1) * query.PageSize;
            var roles = await _roleManager.Roles
                .OrderBy(role => role.Name)
                .Skip(skipCount)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            var roleDtos = new List<RoleDto>();
            foreach (var role in roles)
            {
                var roleDto = RoleMapper.ToDto(role);
                roleDtos.Add(roleDto);
            }

            var paginatedResponse = new PaginatedResponse<RoleDto>
            {
                Items = roleDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<RoleDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateRoleCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<RoleDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                var notFoundMessage = "Role with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<RoleDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var nameIsChanging = !string.Equals(role.Name, command.Name, StringComparison.Ordinal);
            if (nameIsChanging)
            {
                var roleWithNewNameAlreadyExists = await _roleManager.RoleExistsAsync(command.Name);
                if (roleWithNewNameAlreadyExists)
                {
                    var conflictMessage = "Role '" + command.Name + "' already exists.";
                    var conflictResponse = CommonResponse<RoleDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                    return conflictResponse;
                }
            }

            role.Name = command.Name;
            role.Description = command.Description;
            if (!string.IsNullOrEmpty(command.UserType))
            {
                role.UserType = command.UserType;
            }

            var updateResult = await _roleManager.UpdateAsync(role);
            if (!updateResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in updateResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var updateFailureResponse = CommonResponse<RoleDto>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return updateFailureResponse;
            }

            var roleDto = RoleMapper.ToDto(role);
            var successResponse = CommonResponse<RoleDto>.Success(roleDto, "Role updated successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                var notFoundMessage = "Role with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var deleteResult = await _roleManager.DeleteAsync(role);
            if (!deleteResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in deleteResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var deleteFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return deleteFailureResponse;
            }

            var successResponse = CommonResponse<bool>.Success(true, "Role deleted successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<List<MenuClaimDto>>> GetUserRolesAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService.UserId;
            if (currentUserId == null)
            {
                var unauthorizedResponse = CommonResponse<List<MenuClaimDto>>.Fail(ResponseCodes.Unauthorized, "No authenticated user was found.");
                return unauthorizedResponse;
            }

            var user = await _userManager.FindByIdAsync(currentUserId.Value.ToString());
            if (user == null)
            {
                var notFoundResponse = CommonResponse<List<MenuClaimDto>>.Fail(ResponseCodes.NotFound, "User was not found.");
                return notFoundResponse;
            }

            var roleNames = await _userManager.GetRolesAsync(user);

            var roleIds = await _dbContext.Roles
                .Where(role => roleNames.Contains(role.Name))
                .Select(role => role.Id)
                .ToListAsync(cancellationToken);

            var callerAudience = ResolveMenuAudience(user);
            var menuClaims = await GetRoleMenuClaimsAsync(user.Id, roleIds, callerAudience, cancellationToken);
            var successResponse = CommonResponse<List<MenuClaimDto>>.Success(menuClaims);
            return successResponse;
        }

        // "Audience" mirrors the account's own UserType directly -- SuperAdmin/Admin resolve
        // Admin audience, everything else resolves User audience. Reversed 2026-08-24 from the
        // original "Employee-linked account -> Admin audience" special case: Teacher self-service
        // logins (Employee-linked) and Student self-service logins are both plain UserType.User
        // accounts and both belong on the User side of the nav split, same as each other -- My
        // Workspace's own menu tree moved to MenuAudience.User to match (see
        // MenuSeeder.BuildMenuCatalog's "My Workspace" section). Accepted trade-off: an Employee
        // account later promoted to UserType.Admin/SuperAdmin (e.g. a Principal who also
        // administers the panel) would resolve Admin audience and stop seeing My Workspace in
        // their nav -- no such account exists in this codebase's seed/sample data today.
        private string ResolveMenuAudience(ApplicationUser user)
        {
            if (user.UserType == UserType.SuperAdmin || user.UserType == UserType.Admin)
            {
                return MenuAudience.Admin;
            }

            return MenuAudience.User;
        }

        private async Task<List<MenuClaimDto>> GetRoleMenuClaimsAsync(Guid userId, List<Guid> roleIds, string callerAudience, CancellationToken cancellationToken)
        {
            var rootMenuDtos = new List<MenuClaimDto>();

            var grantedMenuIds = new HashSet<int>();
            if (roleIds.Count > 0)
            {
                var roleMenuIds = await _dbContext.RoleClaims
                    .Where(roleClaim => roleIds.Contains(roleClaim.RoleId))
                    .Select(roleClaim => roleClaim.MenuId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                foreach (var roleMenuId in roleMenuIds)
                {
                    grantedMenuIds.Add(roleMenuId);
                }
            }

            // Per-user overrides (2026-08-07) -- additive on top of role grants, so a user with
            // zero roles but a direct menu grant still sees it in their tree.
            var directMenuIds = await _dbContext.UserClaims
                .Where(userClaim => userClaim.UserId == userId)
                .Select(userClaim => userClaim.MenuId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var directMenuId in directMenuIds)
            {
                grantedMenuIds.Add(directMenuId);
            }

            if (grantedMenuIds.Count == 0)
            {
                return rootMenuDtos;
            }

            // Audience filter (2026-08-07): a granted menu only seeds the tree if it's tagged for
            // this caller's audience (MenuAudience.Both, or exactly their own) -- defense-in-depth
            // on top of the role-claim grant itself, and what actually separates a future
            // Student-portal menu from an Admin-panel one. Applied here, before the ancestor walk,
            // not after the tree is built -- so an audience-mismatched leaf can never orphan
            // itself by losing a filtered-out parent (its ancestors are still included below
            // regardless of their own MenuFor, since they're structural, not separate grants).
            var grantedMenus = await _dbContext.Menus
                .Where(menu => grantedMenuIds.Contains(menu.Id))
                .ToListAsync(cancellationToken);

            var allowedMenuIds = new List<int>();
            foreach (var grantedMenu in grantedMenus)
            {
                if (grantedMenu.MenuFor == MenuAudience.Both || grantedMenu.MenuFor == callerAudience)
                {
                    allowedMenuIds.Add(grantedMenu.Id);
                }
            }

            if (allowedMenuIds.Count == 0)
            {
                return rootMenuDtos;
            }

            // Granted menus are usually PERMISSION leaves -- walk their ancestor chain so the
            // SUB_MENU/MAIN_MENU nodes above them are included in the tree too. The hierarchy is
            // at most three levels deep, so this loop runs at most twice.
            var includedMenuIds = new HashSet<int>(allowedMenuIds);
            var currentLevelIds = new List<int>(allowedMenuIds);
            while (currentLevelIds.Count > 0)
            {
                var parentIds = await _dbContext.Menus
                    .Where(menu => currentLevelIds.Contains(menu.Id) && menu.ParentId != null)
                    .Select(menu => menu.ParentId.Value)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                var newlyIncludedIds = new List<int>();
                foreach (var parentId in parentIds)
                {
                    if (includedMenuIds.Add(parentId))
                    {
                        newlyIncludedIds.Add(parentId);
                    }
                }

                currentLevelIds = newlyIncludedIds;
            }

            // Soft-deleted menus are excluded automatically by the global !IsDeleted query filter.
            var menus = await _dbContext.Menus
                .Where(menu => includedMenuIds.Contains(menu.Id))
                .OrderBy(menu => menu.Order)
                .ToListAsync(cancellationToken);

            var menuDtos = new List<MenuClaimDto>();
            var menuDtosById = new Dictionary<int, MenuClaimDto>();
            foreach (var menu in menus)
            {
                var menuDto = MenuClaimMapper.ToDto(menu);
                menuDtos.Add(menuDto);
                menuDtosById.Add(menuDto.Id, menuDto);
            }

            foreach (var menuDto in menuDtos)
            {
                if (menuDto.ParentId != null && menuDtosById.TryGetValue(menuDto.ParentId.Value, out var parentDto))
                {
                    parentDto.Children.Add(menuDto);
                }
                else
                {
                    rootMenuDtos.Add(menuDto);
                }
            }

            foreach (var menuDto in menuDtos)
            {
                menuDto.HasChildren = menuDto.Children.Count > 0;
            }

            return rootMenuDtos;
        }

        public async Task<CommonResponse<List<RoleClaimDto>>> GetRoleClaimsAsync(Guid roleId, CancellationToken cancellationToken = default)
        {
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                var notFoundMessage = "Role with id '" + roleId + "' was not found.";
                var notFoundResponse = CommonResponse<List<RoleClaimDto>>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var roleClaims = await _dbContext.RoleClaims
                .Where(roleClaim => roleClaim.RoleId == roleId)
                .Include(roleClaim => roleClaim.Menu)
                .OrderBy(roleClaim => roleClaim.MenuId)
                .ToListAsync(cancellationToken);

            var roleClaimDtos = new List<RoleClaimDto>();
            foreach (var roleClaim in roleClaims)
            {
                var roleClaimDto = RoleClaimMapper.ToDto(roleClaim);
                roleClaimDtos.Add(roleClaimDto);
            }

            var successResponse = CommonResponse<List<RoleClaimDto>>.Success(roleClaimDtos);
            return successResponse;
        }

        public async Task<CommonResponse<RoleClaimDto>> AssignMenuToRoleAsync(AssignMenuToRoleCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _assignMenuToRoleCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<RoleClaimDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var role = await _roleManager.FindByIdAsync(command.RoleId.ToString());
            if (role == null)
            {
                var roleNotFoundMessage = "Role with id '" + command.RoleId + "' was not found.";
                var roleNotFoundResponse = CommonResponse<RoleClaimDto>.Fail(ResponseCodes.NotFound, roleNotFoundMessage);
                return roleNotFoundResponse;
            }

            var menu = await _dbContext.Menus.FirstOrDefaultAsync(m => m.Id == command.MenuId, cancellationToken);
            if (menu == null)
            {
                var menuNotFoundMessage = "Menu with id '" + command.MenuId + "' was not found.";
                var menuNotFoundResponse = CommonResponse<RoleClaimDto>.Fail(ResponseCodes.NotFound, menuNotFoundMessage);
                return menuNotFoundResponse;
            }

            // No privilege escalation: a caller can only hand out a permission they already hold
            // themselves (SuperAdmin bypasses -- same reasoning as AuthorizedAction's own bypass).
            // Without this, an Admin holding only the "AssignMenuToRole" grant could hand any role
            // every permission in the system, including ones they don't have.
            var callerIsSuperAdmin = await IsCallerSuperAdminAsync();
            if (!callerIsSuperAdmin)
            {
                var callerMenuIds = await GetCallerGrantedMenuIdsAsync(cancellationToken);
                if (!callerMenuIds.Contains(command.MenuId))
                {
                    var escalationMessage = "You cannot grant '" + menu.DisplayName + "' because you do not hold it yourself.";
                    var escalationResponse = CommonResponse<RoleClaimDto>.Fail(ResponseCodes.Forbidden, escalationMessage);
                    return escalationResponse;
                }
            }

            var alreadyAssigned = await _dbContext.RoleClaims
                .AnyAsync(roleClaim => roleClaim.RoleId == command.RoleId && roleClaim.MenuId == command.MenuId, cancellationToken);
            if (alreadyAssigned)
            {
                var conflictMessage = "This menu is already assigned to the role.";
                var conflictResponse = CommonResponse<RoleClaimDto>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            var newRoleClaim = new ApplicationRoleClaim
            {
                RoleId = command.RoleId,
                MenuId = command.MenuId,
                Menu = menu,
                ApplicationRole = role,
                ClaimType = "Permission",
                ClaimValue = menu.Code
            };

            _dbContext.RoleClaims.Add(newRoleClaim);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var roleClaimDto = RoleClaimMapper.ToDto(newRoleClaim);
            var successResponse = CommonResponse<RoleClaimDto>.Success(roleClaimDto, "Menu assigned to role successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveMenuFromRoleAsync(Guid roleId, int menuId, CancellationToken cancellationToken = default)
        {
            var roleClaim = await _dbContext.RoleClaims
                .FirstOrDefaultAsync(rc => rc.RoleId == roleId && rc.MenuId == menuId, cancellationToken);

            if (roleClaim == null)
            {
                var notFoundMessage = "This menu is not assigned to the role.";
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            _dbContext.RoleClaims.Remove(roleClaim);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Menu removed from role successfully.");
            return successResponse;
        }

        // Full-replace sync: the caller sends the complete desired set of MenuIds for the role in
        // one call, and this diffs it against what's currently assigned -- adding what's missing,
        // removing what's no longer wanted -- in a single SaveChangesAsync. Added specifically so a
        // "Save" button on a role-permissions screen doesn't have to reliably fire one
        // POST/DELETE per individual checkbox change (a missed DELETE call for an unchecked menu
        // was the actual root cause of claims appearing to "never get removed" -- see
        // Docs/role_menu_claims_sync_implementation_guide.md).
        public async Task<CommonResponse<List<RoleClaimDto>>> SyncRoleMenuClaimsAsync(Guid roleId, SyncRoleMenuClaimsCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _syncRoleMenuClaimsCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<List<RoleClaimDto>>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                var roleNotFoundMessage = "Role with id '" + roleId + "' was not found.";
                var roleNotFoundResponse = CommonResponse<List<RoleClaimDto>>.Fail(ResponseCodes.NotFound, roleNotFoundMessage);
                return roleNotFoundResponse;
            }

            var requestedMenuIds = new List<int>();
            foreach (var menuId in command.MenuIds)
            {
                if (!requestedMenuIds.Contains(menuId))
                {
                    requestedMenuIds.Add(menuId);
                }
            }

            var requestedMenus = new List<Menu>();
            if (requestedMenuIds.Count > 0)
            {
                requestedMenus = await _dbContext.Menus
                    .Where(menu => requestedMenuIds.Contains(menu.Id))
                    .ToListAsync(cancellationToken);

                if (requestedMenus.Count != requestedMenuIds.Count)
                {
                    var foundMenuIds = new List<int>();
                    foreach (var foundMenu in requestedMenus)
                    {
                        foundMenuIds.Add(foundMenu.Id);
                    }

                    var missingMenuIds = new List<int>();
                    foreach (var requestedMenuId in requestedMenuIds)
                    {
                        if (!foundMenuIds.Contains(requestedMenuId))
                        {
                            missingMenuIds.Add(requestedMenuId);
                        }
                    }

                    var missingMenuMessage = "Menu(s) with id(s) '" + string.Join(", ", missingMenuIds) + "' were not found.";
                    var missingMenuResponse = CommonResponse<List<RoleClaimDto>>.Fail(ResponseCodes.NotFound, missingMenuMessage);
                    return missingMenuResponse;
                }
            }

            var existingClaims = await _dbContext.RoleClaims
                .Where(roleClaim => roleClaim.RoleId == roleId)
                .ToListAsync(cancellationToken);

            var existingMenuIds = new List<int>();
            foreach (var existingClaim in existingClaims)
            {
                existingMenuIds.Add(existingClaim.MenuId);
            }

            var menuIdsToAdd = new List<int>();
            foreach (var requestedMenuId in requestedMenuIds)
            {
                if (!existingMenuIds.Contains(requestedMenuId))
                {
                    menuIdsToAdd.Add(requestedMenuId);
                }
            }

            var claimsToRemove = new List<ApplicationRoleClaim>();
            foreach (var existingClaim in existingClaims)
            {
                if (!requestedMenuIds.Contains(existingClaim.MenuId))
                {
                    claimsToRemove.Add(existingClaim);
                }
            }

            // Same privilege-escalation guard as AssignMenuToRoleAsync -- only checked against
            // menus being newly added, since removing a claim never hands out a permission.
            var callerIsSuperAdmin = await IsCallerSuperAdminAsync();
            if (!callerIsSuperAdmin && menuIdsToAdd.Count > 0)
            {
                var callerMenuIds = await GetCallerGrantedMenuIdsAsync(cancellationToken);
                var forbiddenMenuIds = new List<int>();
                foreach (var menuIdToAdd in menuIdsToAdd)
                {
                    if (!callerMenuIds.Contains(menuIdToAdd))
                    {
                        forbiddenMenuIds.Add(menuIdToAdd);
                    }
                }

                if (forbiddenMenuIds.Count > 0)
                {
                    var forbiddenMenuNames = new List<string>();
                    foreach (var requestedMenu in requestedMenus)
                    {
                        if (forbiddenMenuIds.Contains(requestedMenu.Id))
                        {
                            forbiddenMenuNames.Add(requestedMenu.DisplayName);
                        }
                    }

                    var escalationMessage = "You cannot grant the following menus because you do not hold them yourself: " + string.Join(", ", forbiddenMenuNames) + ".";
                    var escalationResponse = CommonResponse<List<RoleClaimDto>>.Fail(ResponseCodes.Forbidden, escalationMessage);
                    return escalationResponse;
                }
            }

            foreach (var menuIdToAdd in menuIdsToAdd)
            {
                var menuToAdd = requestedMenus.First(menu => menu.Id == menuIdToAdd);
                var newRoleClaim = new ApplicationRoleClaim
                {
                    RoleId = roleId,
                    MenuId = menuIdToAdd,
                    Menu = menuToAdd,
                    ApplicationRole = role,
                    ClaimType = "Permission",
                    ClaimValue = menuToAdd.Code
                };

                _dbContext.RoleClaims.Add(newRoleClaim);
            }

            foreach (var claimToRemove in claimsToRemove)
            {
                _dbContext.RoleClaims.Remove(claimToRemove);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var finalClaims = await _dbContext.RoleClaims
                .Where(roleClaim => roleClaim.RoleId == roleId)
                .Include(roleClaim => roleClaim.Menu)
                .OrderBy(roleClaim => roleClaim.MenuId)
                .ToListAsync(cancellationToken);

            var finalClaimDtos = new List<RoleClaimDto>();
            foreach (var finalClaim in finalClaims)
            {
                var finalClaimDto = RoleClaimMapper.ToDto(finalClaim);
                finalClaimDtos.Add(finalClaimDto);
            }

            var successResponse = CommonResponse<List<RoleClaimDto>>.Success(finalClaimDtos, "Role menu claims synced successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> AssignRoleToUserAsync(AssignRoleToUserCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _assignRoleToUserCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var user = await _userManager.FindByIdAsync(command.UserId.ToString());
            if (user == null)
            {
                var userNotFoundMessage = "User with id '" + command.UserId + "' was not found.";
                var userNotFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, userNotFoundMessage);
                return userNotFoundResponse;
            }

            var role = await _roleManager.FindByIdAsync(command.RoleId.ToString());
            if (role == null)
            {
                var roleNotFoundMessage = "Role with id '" + command.RoleId + "' was not found.";
                var roleNotFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, roleNotFoundMessage);
                return roleNotFoundResponse;
            }

            var alreadyInRole = await _userManager.IsInRoleAsync(user, role.Name);
            if (alreadyInRole)
            {
                var conflictMessage = "This user is already in the role.";
                var conflictResponse = CommonResponse<bool>.Fail(ResponseCodes.Conflict, conflictMessage);
                return conflictResponse;
            }

            // No privilege escalation: a caller can only hand out a role whose entire grant set
            // they already hold themselves (SuperAdmin bypasses). A role with zero claims (e.g. a
            // freshly created empty role, or the seeded zero-permission Student role) always
            // passes, since there's nothing to escalate.
            var callerIsSuperAdmin = await IsCallerSuperAdminAsync();
            if (!callerIsSuperAdmin)
            {
                var roleMenuIds = await _dbContext.RoleClaims
                    .Where(roleClaim => roleClaim.RoleId == command.RoleId)
                    .Select(roleClaim => roleClaim.MenuId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                if (roleMenuIds.Count > 0)
                {
                    var callerMenuIds = await GetCallerGrantedMenuIdsAsync(cancellationToken);
                    var missingMenuIds = new List<int>();
                    foreach (var roleMenuId in roleMenuIds)
                    {
                        if (!callerMenuIds.Contains(roleMenuId))
                        {
                            missingMenuIds.Add(roleMenuId);
                        }
                    }

                    if (missingMenuIds.Count > 0)
                    {
                        var missingMenuNames = await _dbContext.Menus
                            .Where(menu => missingMenuIds.Contains(menu.Id))
                            .Select(menu => menu.DisplayName)
                            .ToListAsync(cancellationToken);

                        var escalationMessage = "You cannot assign this role because it grants permissions you do not hold yourself: " + string.Join(", ", missingMenuNames) + ".";
                        var escalationResponse = CommonResponse<bool>.Fail(ResponseCodes.Forbidden, escalationMessage);
                        return escalationResponse;
                    }
                }
            }

            // UserManager is safe here (unlike role-claims): ApplicationUserRole's only custom
            // columns are the IAuditableEntity fields, which the DbContext stamps automatically.
            var addResult = await _userManager.AddToRoleAsync(user, role.Name);
            if (!addResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in addResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var addFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return addFailureResponse;
            }

            var successResponse = CommonResponse<bool>.Success(true, "Role assigned to user successfully.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                var userNotFoundMessage = "User with id '" + userId + "' was not found.";
                var userNotFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, userNotFoundMessage);
                return userNotFoundResponse;
            }

            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role == null)
            {
                var roleNotFoundMessage = "Role with id '" + roleId + "' was not found.";
                var roleNotFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, roleNotFoundMessage);
                return roleNotFoundResponse;
            }

            var isInRole = await _userManager.IsInRoleAsync(user, role.Name);
            if (!isInRole)
            {
                var notAssignedMessage = "This user is not in the role.";
                var notAssignedResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, notAssignedMessage);
                return notAssignedResponse;
            }

            var removeResult = await _userManager.RemoveFromRoleAsync(user, role.Name);
            if (!removeResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in removeResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var removeFailureResponse = CommonResponse<bool>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return removeFailureResponse;
            }

            var successResponse = CommonResponse<bool>.Success(true, "Role removed from user successfully.");
            return successResponse;
        }

        // The caller's type is read from the database (not a token claim), matching
        // AuthorizedAction/UserService.IsCallerSuperAdminAsync: a demoted SuperAdmin loses this
        // bypass on their very next request.
        private async Task<bool> IsCallerSuperAdminAsync()
        {
            var callerId = _currentUserService.UserId;
            if (callerId == null)
            {
                return false;
            }

            var caller = await _userManager.FindByIdAsync(callerId.Value.ToString());
            if (caller == null)
            {
                return false;
            }

            return caller.UserType == UserType.SuperAdmin;
        }

        // Every MenuId granted to any role the caller currently holds -- the caller's own
        // "ceiling" for the privilege-escalation guards above. Empty for a caller with no roles
        // or no authenticated user (never expected to be reached in that case, since both call
        // sites already require an authenticated caller to get this far).
        private async Task<HashSet<int>> GetCallerGrantedMenuIdsAsync(CancellationToken cancellationToken)
        {
            var callerId = _currentUserService.UserId;
            if (callerId == null)
            {
                return new HashSet<int>();
            }

            var caller = await _userManager.FindByIdAsync(callerId.Value.ToString());
            if (caller == null)
            {
                return new HashSet<int>();
            }

            var callerRoleNames = await _userManager.GetRolesAsync(caller);
            var callerRoleIds = await _dbContext.Roles
                .Where(role => callerRoleNames.Contains(role.Name))
                .Select(role => role.Id)
                .ToListAsync(cancellationToken);

            if (callerRoleIds.Count == 0)
            {
                return new HashSet<int>();
            }

            var callerMenuIds = await _dbContext.RoleClaims
                .Where(roleClaim => callerRoleIds.Contains(roleClaim.RoleId))
                .Select(roleClaim => roleClaim.MenuId)
                .Distinct()
                .ToListAsync(cancellationToken);

            return new HashSet<int>(callerMenuIds);
        }

        private static string BuildValidationErrorMessage(ValidationResult validationResult)
        {
            var errorMessages = new List<string>();
            foreach (var failure in validationResult.Errors)
            {
                errorMessages.Add(failure.ErrorMessage);
            }

            var combinedMessage = string.Join(" ", errorMessages);
            return combinedMessage;
        }
    }
}
