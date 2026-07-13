using Application.Common.Models;
using Application.Menus.Commands;
using Application.Menus.Dtos;
using Application.Menus.Queries;

namespace Application.Menus
{
    public interface IMenuService
    {
        Task<CommonResponse<MenuDto>> CreateMenuAsync(CreateMenuCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<MenuDto>> GetMenuByIdAsync(int id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<MenuDto>>> GetMenusAsync(GetMenusQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<MenuDto>> UpdateMenuAsync(int id, UpdateMenuCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteMenuAsync(int id, CancellationToken cancellationToken = default);
    }
}
