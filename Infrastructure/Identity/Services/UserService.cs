using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Users;
using Application.Users.Commands;
using Application.Users.Dtos;
using Application.Users.Queries;
using Application.Users.Validators;
using Domain.Constants;
using Domain.Enums;
using FluentValidation.Results;
using Infrastructure.Email;
using Infrastructure.Identity.Mapper;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Identity.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly CreateUserCommandValidator _createUserCommandValidator;
        private readonly UpdateUserCommandValidator _updateUserCommandValidator;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public UserService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            CreateUserCommandValidator createUserCommandValidator,
            UpdateUserCommandValidator updateUserCommandValidator,
            IEmailService emailService,
            IConfiguration configuration,
            ApplicationDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _createUserCommandValidator = createUserCommandValidator;
            _updateUserCommandValidator = updateUserCommandValidator;
            _emailService = emailService;
            _configuration = configuration;
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<CommonResponse<UserDto>> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _createUserCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<UserDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var trimmedUserName = command.UserName.Trim();
            var trimmedEmail = command.Email.Trim();

            var existingUserByName = await _userManager.FindByNameAsync(trimmedUserName);
            if (existingUserByName != null)
            {
                var userNameConflictMessage = "Username '" + trimmedUserName + "' is already taken.";
                var userNameConflictResponse = CommonResponse<UserDto>.Fail(ResponseCodes.Conflict, userNameConflictMessage);
                return userNameConflictResponse;
            }

            var existingUserByEmail = await _userManager.FindByEmailAsync(trimmedEmail);
            if (existingUserByEmail != null)
            {
                var emailConflictMessage = "Email '" + trimmedEmail + "' is already registered.";
                var emailConflictResponse = CommonResponse<UserDto>.Fail(ResponseCodes.Conflict, emailConflictMessage);
                return emailConflictResponse;
            }

            var requestedRoles = new List<ApplicationRole>();
            var roleResolutionError = await ResolveRolesByIdAsync(command.RoleIds, requestedRoles);
            if (roleResolutionError != null)
            {
                var roleNotFoundResponse = CommonResponse<UserDto>.Fail(ResponseCodes.NotFound, roleResolutionError);
                return roleNotFoundResponse;
            }

            var registrationTimestamp = DateTimeOffset.UtcNow;
            var user = new ApplicationUser
            {
                UserName = trimmedUserName,
                Email = trimmedEmail,
                FirstName = command.FirstName.Trim(),
                MiddleName = command.MiddleName?.Trim(),
                LastName = command.LastName.Trim(),
                Gender = command.Gender,
                UserType = UserType.User,
                Dob = command.Dob,
                PhoneCountryCode = command.PhoneCountryCode,
                PhoneNumber = command.PhoneNumber,
                CountryIso3 = command.CountryIso3,
                IsTosAgreed = command.IsTosAgreed,
                IsActive = true,
                LastPasswordChangedTs = registrationTimestamp
            };

            var createResult = await _userManager.CreateAsync(user, command.Password);
            if (!createResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in createResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var createFailureResponse = CommonResponse<UserDto>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return createFailureResponse;
            }

            if (requestedRoles.Count > 0)
            {
                var requestedRoleNames = new List<string>();
                foreach (var role in requestedRoles)
                {
                    requestedRoleNames.Add(role.Name);
                }

                await _userManager.AddToRolesAsync(user, requestedRoleNames);
            }
            else
            {
                await AssignDefaultRoleAsync(user);
            }

            await SendVerificationEmailAsync(user, cancellationToken);

            var assignedRoleIds = await GetRoleIdsAsync(user.Id, cancellationToken);
            var userDto = UserMapper.ToDto(user, assignedRoleIds);
            var successResponse = CommonResponse<UserDto>.Success(userDto, "User created successfully. Please check your email to verify your account.");
            return successResponse;
        }

        private async Task AssignDefaultRoleAsync(ApplicationUser user)
        {
            var defaultRoleExists = await _roleManager.RoleExistsAsync(RoleNames.User);
            if (defaultRoleExists)
            {
                await _userManager.AddToRoleAsync(user, RoleNames.User);
            }
        }

        private async Task SendVerificationEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var confirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var verificationLink = EmailLinkBuilder.BuildVerifyEmailLink(_configuration, user.Id, confirmationToken);
            var emailBody = "<p>Welcome to CMSApp!</p><p>Please verify your email address:</p><p>" + verificationLink + "</p>";
            await _emailService.SendEmailAsync(user.Email, "Verify your email address", emailBody, cancellationToken);
        }

        public async Task<CommonResponse<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                var notFoundMessage = "User with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<UserDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var roleIds = await GetRoleIdsAsync(user.Id, cancellationToken);
            var userDto = UserMapper.ToDto(user, roleIds);
            var successResponse = CommonResponse<UserDto>.Success(userDto);
            return successResponse;
        }

        public async Task<CommonResponse<PaginatedResponse<UserDto>>> GetUsersAsync(GetUsersQuery query, CancellationToken cancellationToken = default)
        {
            var totalCount = await _userManager.Users.CountAsync(cancellationToken);
            var skipCount = (query.Page - 1) * query.PageSize;
            var users = await _userManager.Users
                .OrderBy(user => user.UserName)
                .Skip(skipCount)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

            // One query for the whole page's role links, grouped in memory per user below --
            // not one query per row.
            var userIds = new List<Guid>();
            foreach (var user in users)
            {
                userIds.Add(user.Id);
            }

            var userRoleLinks = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(userRole => userIds.Contains(userRole.UserId))
                .ToListAsync(cancellationToken);

            var userDtos = new List<UserDto>();
            foreach (var user in users)
            {
                var roleIds = new List<Guid>();
                foreach (var userRoleLink in userRoleLinks)
                {
                    if (userRoleLink.UserId == user.Id)
                    {
                        roleIds.Add(userRoleLink.RoleId);
                    }
                }

                var userDto = UserMapper.ToDto(user, roleIds);
                userDtos.Add(userDto);
            }

            var paginatedResponse = new PaginatedResponse<UserDto>
            {
                Items = userDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = totalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<UserDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserCommand command, CancellationToken cancellationToken = default)
        {
            var validationResult = _updateUserCommandValidator.Validate(command);
            if (!validationResult.IsValid)
            {
                var errorMessage = BuildValidationErrorMessage(validationResult);
                var validationFailureResponse = CommonResponse<UserDto>.Fail(ResponseCodes.ValidationError, errorMessage);
                return validationFailureResponse;
            }

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                var notFoundMessage = "User with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<UserDto>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            var deactivatingSuperAdmin = user.UserType == UserType.SuperAdmin && !command.IsActive;
            if (deactivatingSuperAdmin)
            {
                var deactivateForbiddenResponse = CommonResponse<UserDto>.Fail(ResponseCodes.Forbidden, "A SuperAdmin account cannot be deactivated.");
                return deactivateForbiddenResponse;
            }

            // Changing a user's type (promotion or demotion) is restricted to SuperAdmin callers.
            // Holding the Users/UpdateUser permission alone is not enough -- otherwise anyone with
            // that grant could promote themselves (or anyone else) to SuperAdmin.
            var userTypeIsChanging = user.UserType != command.UserType;
            if (userTypeIsChanging)
            {
                var callerIsSuperAdmin = await IsCallerSuperAdminAsync();
                if (!callerIsSuperAdmin)
                {
                    var userTypeForbiddenResponse = CommonResponse<UserDto>.Fail(ResponseCodes.Forbidden, "Only a SuperAdmin can change a user's type.");
                    return userTypeForbiddenResponse;
                }
            }

            // Resolve before mutating anything, so an unknown role id fails the whole request cleanly.
            var requestedRoles = new List<ApplicationRole>();
            var roleResolutionError = await ResolveRolesByIdAsync(command.RoleIds, requestedRoles);
            if (roleResolutionError != null)
            {
                var roleNotFoundResponse = CommonResponse<UserDto>.Fail(ResponseCodes.NotFound, roleResolutionError);
                return roleNotFoundResponse;
            }

            user.FirstName = command.FirstName.Trim();
            user.MiddleName = command.MiddleName?.Trim();
            user.LastName = command.LastName.Trim();
            user.Gender = command.Gender;
            user.UserType = command.UserType;
            user.Dob = command.Dob;
            user.PhoneCountryCode = command.PhoneCountryCode;
            user.PhoneNumber = command.PhoneNumber;
            user.CountryIso3 = command.CountryIso3;
            user.IsActive = command.IsActive;
            user.IsIpRestricted = command.IsIpRestricted;
            user.UserIpAllowed = NormalizeIpList(command.UserIpAllowed);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in updateResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                var combinedMessage = string.Join(" ", errorMessages);
                var updateFailureResponse = CommonResponse<UserDto>.Fail(ResponseCodes.ValidationError, combinedMessage);
                return updateFailureResponse;
            }

            if (command.RoleIds != null)
            {
                await SyncUserRolesAsync(user, requestedRoles);
            }

            var currentRoleIds = await GetRoleIdsAsync(user.Id, cancellationToken);
            var userDto = UserMapper.ToDto(user, currentRoleIds);
            var successResponse = CommonResponse<UserDto>.Success(userDto, "User updated successfully.");
            return successResponse;
        }

        // The caller's type is read from the database (not a token claim), matching AuthorizedAction:
        // a demoted SuperAdmin loses this ability on their very next request.
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

        private async Task<string> ResolveRolesByIdAsync(List<Guid> roleIds, List<ApplicationRole> resolvedRoles)
        {
            if (roleIds == null)
            {
                return null;
            }

            var seenRoleIds = new HashSet<Guid>();
            foreach (var roleId in roleIds)
            {
                if (!seenRoleIds.Add(roleId))
                {
                    continue;
                }

                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                if (role == null)
                {
                    var roleNotFoundMessage = "Role with id '" + roleId + "' was not found.";
                    return roleNotFoundMessage;
                }

                resolvedRoles.Add(role);
            }

            return null;
        }

        private async Task SyncUserRolesAsync(ApplicationUser user, List<ApplicationRole> desiredRoles)
        {
            var desiredRoleNames = new List<string>();
            foreach (var role in desiredRoles)
            {
                desiredRoleNames.Add(role.Name);
            }

            var currentRoleNames = await _userManager.GetRolesAsync(user);

            var roleNamesToAdd = new List<string>();
            foreach (var desiredRoleName in desiredRoleNames)
            {
                if (!currentRoleNames.Contains(desiredRoleName))
                {
                    roleNamesToAdd.Add(desiredRoleName);
                }
            }

            var roleNamesToRemove = new List<string>();
            foreach (var currentRoleName in currentRoleNames)
            {
                if (!desiredRoleNames.Contains(currentRoleName))
                {
                    roleNamesToRemove.Add(currentRoleName);
                }
            }

            if (roleNamesToAdd.Count > 0)
            {
                await _userManager.AddToRolesAsync(user, roleNamesToAdd);
            }

            if (roleNamesToRemove.Count > 0)
            {
                await _userManager.RemoveFromRolesAsync(user, roleNamesToRemove);
            }
        }

        public async Task<CommonResponse<bool>> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                var notFoundMessage = "User with id '" + id + "' was not found.";
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, notFoundMessage);
                return notFoundResponse;
            }

            if (user.UserType == UserType.SuperAdmin)
            {
                var deleteForbiddenResponse = CommonResponse<bool>.Fail(ResponseCodes.Forbidden, "A SuperAdmin account cannot be deleted.");
                return deleteForbiddenResponse;
            }

            var deleteResult = await _userManager.DeleteAsync(user);
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

            var successResponse = CommonResponse<bool>.Success(true, "User deleted successfully.");
            return successResponse;
        }

        // Stores the list in a canonical "ip,ip,ip" form (tokens trimmed, empties dropped) so
        // any future enforcement check can split on ',' without re-trimming.
        private static string NormalizeIpList(string userIpAllowed)
        {
            if (string.IsNullOrWhiteSpace(userIpAllowed))
            {
                return null;
            }

            var normalizedTokens = new List<string>();
            var ipTokens = userIpAllowed.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var ipToken in ipTokens)
            {
                var trimmedToken = ipToken.Trim();
                if (trimmedToken.Length > 0)
                {
                    normalizedTokens.Add(trimmedToken);
                }
            }

            var normalizedList = string.Join(",", normalizedTokens);
            return normalizedList;
        }

        private async Task<List<Guid>> GetRoleIdsAsync(Guid userId, CancellationToken cancellationToken)
        {
            var roleIds = await _dbContext.UserRoles
                .AsNoTracking()
                .Where(userRole => userRole.UserId == userId)
                .Select(userRole => userRole.RoleId)
                .ToListAsync(cancellationToken);

            return roleIds;
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
