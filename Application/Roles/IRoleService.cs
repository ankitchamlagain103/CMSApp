using Application.Common.Models;
using Application.Roles.Commands;
using Application.Roles.Dtos;
using Application.Roles.Queries;

namespace Application.Roles
{
    public interface IRoleService
    {
        Task<CommonResponse<RoleDto>> CreateRoleAsync(CreateRoleCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<RoleDto>> GetRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<RoleDto>>> GetRolesAsync(GetRolesQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<RoleDto>> UpdateRoleAsync(Guid id, UpdateRoleCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteRoleAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<MenuClaimDto>>> GetUserRolesAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<List<RoleClaimDto>>> GetRoleClaimsAsync(Guid roleId, CancellationToken cancellationToken = default);

        Task<CommonResponse<RoleClaimDto>> AssignMenuToRoleAsync(AssignMenuToRoleCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveMenuFromRoleAsync(Guid roleId, int menuId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> AssignRoleToUserAsync(AssignRoleToUserCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    }
}
