using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class SystemAccessLogRepository : Repository<SystemAccessLog, long>, ISystemAccessLogRepository
    {
        public SystemAccessLogRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<SystemAccessLog>> GetPagedByCreatedDescAsync(int pageNumber, int pageSize, Guid? userId, CancellationToken cancellationToken = default)
        {
            var query = DbSet.AsQueryable();
            if (userId != null)
            {
                query = query.Where(log => log.UserId == userId.Value);
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await query
                .OrderByDescending(log => log.CreatedTs)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<SystemAccessLog>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }
    }
}
