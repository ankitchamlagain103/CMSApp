using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class ErrorLogRepository : Repository<ErrorLog, long>, IErrorLogRepository
    {
        public ErrorLogRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<ErrorLog> GetByFingerprintAsync(string fingerprintHash, CancellationToken cancellationToken = default)
        {
            var errorLog = await DbSet.FirstOrDefaultAsync(log => log.FingerprintHash == fingerprintHash, cancellationToken);
            return errorLog;
        }

        public async Task<PagedResult<ErrorLog>> GetPagedByLastOccurredDescAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await DbSet.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await DbSet
                .OrderByDescending(log => log.LastOccurredTs)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<ErrorLog>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<int> GetDistinctErrorCountAsync(CancellationToken cancellationToken = default)
        {
            var distinctErrorCount = await DbSet.CountAsync(cancellationToken);
            return distinctErrorCount;
        }

        public async Task<long> GetTotalOccurrenceCountAsync(CancellationToken cancellationToken = default)
        {
            var totalOccurrenceCount = await DbSet.SumAsync(log => (long)log.ErrorCount, cancellationToken);
            return totalOccurrenceCount;
        }
    }
}
