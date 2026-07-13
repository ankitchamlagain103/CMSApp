using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ISystemAccessLogRepository : IRepository<SystemAccessLog, long>
    {
        Task<PagedResult<SystemAccessLog>> GetPagedByCreatedDescAsync(int pageNumber, int pageSize, Guid? userId, CancellationToken cancellationToken = default);
    }
}
