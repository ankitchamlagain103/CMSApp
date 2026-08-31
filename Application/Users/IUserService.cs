using Application.Common.Models;
using Application.Users.Commands;
using Application.Users.Dtos;
using Application.Users.Queries;

namespace Application.Users
{
    public interface IUserService
    {
        Task<CommonResponse<UserDto>> CreateUserAsync(CreateUserCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<UserDto>> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<UserDto>>> GetUsersAsync(GetUsersQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<UserDto>> UpdateUserAsync(Guid id, UpdateUserCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);

        // Per-user menu overrides (2026-08-07) -- additive on top of whatever the user's roles
        // already grant, never a substitute. Same privilege-escalation guard as
        // IRoleService.AssignMenuToRoleAsync: a caller can only hand out a menu they already hold
        // themselves (SuperAdmin exempt).
        Task<CommonResponse<List<UserClaimDto>>> GetUserClaimsAsync(Guid userId, CancellationToken cancellationToken = default);

        Task<CommonResponse<UserClaimDto>> AssignMenuToUserAsync(AssignMenuToUserCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveMenuFromUserAsync(Guid userId, int menuId, CancellationToken cancellationToken = default);
    }
}
