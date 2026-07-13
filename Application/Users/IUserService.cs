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
    }
}
