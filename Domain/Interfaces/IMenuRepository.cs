using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IMenuRepository : IRepository<Menu, int>
    {
        Task<PagedResult<Menu>> GetPagedByFilterAsync(string menuType, string menuFor, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

        Task<bool> HasChildrenAsync(int id, CancellationToken cancellationToken = default);
    }
}
