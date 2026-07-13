using Application.Common.Models;

namespace Application.Common.Interfaces
{
    public interface IIdentityService
    {
        Task<string> GetUserNameAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<bool> IsInRoleAsync(Guid userId, string role, CancellationToken cancellationToken = default);

        Task<IdentityOperationResult> CreateUserAsync(string userName, string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default);

        Task<bool> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
