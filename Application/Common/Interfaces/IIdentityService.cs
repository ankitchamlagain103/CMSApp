using Application.Common.Models;

namespace Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

        Task<IdentityOperationResult> CreateUserAsync(string userName, string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default);

        Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

        // Portal account provisioning (2026-07-27) -- the shared primitive behind "create a login
        // for this Employee/Student record". See ProvisionPortalAccountRequest for the two ways a
        // caller can specify roles.
        Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

        Task<IdentityOperationResult> ProvisionPortalAccountAsync(ProvisionPortalAccountRequest request, CancellationToken cancellationToken = default);
    }
}
