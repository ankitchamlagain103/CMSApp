using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Roles;
using Application.Roles.Commands;
using Application.Roles.Dtos;
using Application.Roles.Queries;
using Application.Roles.Validators;
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
        private readonly ApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public RoleService(
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager,
            CreateRoleCommandValidator createRoleCommandValidator,
            UpdateRoleCommandValidator updateRoleCommandValidator,
            AssignMenuToRoleCommandValidator assignMenuToRoleCommandValidator,
            AssignRoleToUserCommandValidator assignRoleToUserCommandValidator,
            ApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _roleManager = roleManager;
            _userManager = userManager;
            _createRoleCommandValidator = createRoleCommandValidator;
            _updateRoleCommandValidator = updateRoleCommandValidator;
            _assignMenuToRoleCommandValidator = assignMenuToRoleCommandValidator;
            _assignRoleToUserCommandValidator = assignRoleToUserCommandValidator;
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

            var role = new ApplicationRole
            {
                Name = command.Name,
                Description = command.Description
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

            var menuClaims = await GetRoleMenuClaimsAsync(roleIds, cancellationToken);
            var successResponse = CommonResponse<List<MenuClaimDto>>.Success(menuClaims);
            return successResponse;
        }

        private async Task<List<MenuClaimDto>> GetRoleMenuClaimsAsync(List<Guid> roleIds, CancellationToken cancellationToken)
        {
            var rootMenuDtos = new List<MenuClaimDto>();
            if (roleIds.Count == 0)
            {
                return rootMenuDtos;
            }

            var allowedMenuIds = await _dbContext.RoleClaims
                .Where(roleClaim => roleIds.Contains(roleClaim.RoleId))
                .Select(roleClaim => roleClaim.MenuId)
                .Distinct()
                .ToListAsync(cancellationToken);

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
