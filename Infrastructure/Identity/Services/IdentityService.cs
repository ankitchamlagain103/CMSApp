using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity.Services
{
    public class IdentityService : IIdentityService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public IdentityService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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
    }
}
