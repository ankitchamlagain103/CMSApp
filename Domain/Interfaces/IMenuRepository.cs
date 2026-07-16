using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IMenuRepository : IRepository<Menu, int>
    {
        Task<PagedResult<Menu>> GetPagedByFilterAsync(MenuFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

        Task<bool> HasChildrenAsync(int id, CancellationToken cancellationToken = default);
    }
}
