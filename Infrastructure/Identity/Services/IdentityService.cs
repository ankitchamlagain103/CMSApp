using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using Infrastructure.Email;
using Infrastructure.Identity.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Identity.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public IdentityService(
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            var userName = user?.UserName;
            return userName;
        }

        public async Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            var isInRole = await _userManager.IsInRoleAsync(user, role);
            return isInRole;
        }

        public async Task<IdentityOperationResult> CreateUserAsync(string userName, string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default)
        {
            // FirstName/LastName are NOT NULL in the database, so they are required parameters --
            // without them every create through this primitive fails at the database.
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                UserType = UserType.User,
                IsActive = true,
                LastPasswordChangedTs = DateTimeOffset.UtcNow
            };

            var createResult = await _userManager.CreateAsync(user, password);

            var operationResult = new IdentityOperationResult
            {
                Succeeded = createResult.Succeeded,
                UserId = user.Id.ToString()
            };

            if (!createResult.Succeeded)
            {
                var errorMessages = new List<string>();
                foreach (var error in createResult.Errors)
                {
                    errorMessages.Add(error.Description);
                }

                operationResult.Errors = errorMessages;
            }

            return operationResult;
        }

        public async Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return false;
            }

            var deleteResult = await _userManager.DeleteAsync(user);
            return deleteResult.Succeeded;
        }

        public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        {
            var existingUser = await _userManager.FindByEmailAsync(email);
            return existingUser != null;
        }

        // Creates a portal account with no password -- same "record, not a self-registered
        // stranger" shape as Google-created accounts (AuthService.GoogleLoginAsync): EmailConfirmed
        // is set true directly (an Employee/Student is already a verified record via HR/admission),
        // and a password-reset token is emailed immediately as the "set your password to activate
        // your account" step, reusing the existing anonymous reset-password endpoint instead of a
        // new activation mechanism.
        public async Task<IdentityOperationResult> ProvisionPortalAccountAsync(ProvisionPortalAccountRequest request, CancellationToken cancellationToken = default)
        {
            var resolvedRoleNames = new List<string>();
            var errors = new List<string>();

            foreach (var roleId in request.RoleIds ?? new List<Guid>())
            {
                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                if (role == null)
                {
                    errors.Add("Role with id '" + roleId + "' was not found.");
                    continue;
                }

                if (!resolvedRoleNames.Contains(role.Name))
                {
                    resolvedRoleNames.Add(role.Name);
                }
            }

            foreach (var roleName in request.RoleNames ?? new List<string>())
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null)
                {
                    errors.Add("Role '" + roleName + "' was not found.");
                    continue;
                }

                if (!resolvedRoleNames.Contains(role.Name))
                {
                    resolvedRoleNames.Add(role.Name);
                }
            }

            if (errors.Count > 0)
            {
                return new IdentityOperationResult { Succeeded = false, Errors = errors };
            }

            if (resolvedRoleNames.Count == 0)
            {
                return new IdentityOperationResult { Succeeded = false, Errors = new List<string> { "At least one role is required to create a portal account." } };
            }

            var userName = await UserNameGenerator.GenerateUniqueAsync(_userManager, request.Email);
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = request.Email,
                EmailConfirmed = true,
                PhoneNumber = request.PhoneNumber,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = request.Gender,
                UserType = UserType.User,
                IsActive = true,
                IsTosAgreed = true
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                var createErrorMessages = new List<string>();
                foreach (var error in createResult.Errors)
                {
                    createErrorMessages.Add(error.Description);
                }

                return new IdentityOperationResult { Succeeded = false, Errors = createErrorMessages };
            }

            await _userManager.AddToRolesAsync(user, resolvedRoleNames);

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var activationLink = EmailLinkBuilder.BuildResetPasswordLink(_configuration, user.Id, resetToken);
            var emailBody = "<p>An account was created for you in CMSApp.</p><p>Set your password to activate it:</p><p>" + activationLink + "</p>";
            await _emailService.SendEmailAsync(user.Email, "Set your password to activate your account", emailBody, cancellationToken);

            return new IdentityOperationResult { Succeeded = true, UserId = user.Id.ToString() };
        }
    }
}
