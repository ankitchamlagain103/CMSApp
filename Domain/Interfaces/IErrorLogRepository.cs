using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IErrorLogRepository : IRepository<ErrorLog, long>
    {
        Task<ErrorLog> GetByFingerprintAsync(string fingerprintHash, CancellationToken cancellationToken = default);

        Task<PagedResult<ErrorLog>> GetPagedByLastOccurredDescAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<int> GetDistinctErrorCountAsync(CancellationToken cancellationToken = default);

        Task<long> GetTotalOccurrenceCountAsync(CancellationToken cancellationToken = default);
    }
}
