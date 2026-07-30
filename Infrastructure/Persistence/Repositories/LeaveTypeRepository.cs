using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class LeaveTypeRepository : Repository<LeaveType, Guid>, ILeaveTypeRepository
    {
        public LeaveTypeRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public new async Task<PagedResult<LeaveType>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await DbSet.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await DbSet
                .OrderBy(t => t.Name)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<LeaveType>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index still sees soft-deleted rows.
            var exists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Name == name, cancellationToken);

            return exists;
        }

        public async Task<bool> IsReferencedAsync(Guid leaveTypeId, CancellationToken cancellationToken = default)
        {
            var referencedByBalance = await DbContext.Set<EmployeeLeaveBalance>()
                .AnyAsync(b => b.LeaveTypeId == leaveTypeId, cancellationToken);
            if (referencedByBalance)
            {
                return true;
            }

            var referencedByRequest = await DbContext.Set<LeaveRequest>()
                .AnyAsync(r => r.LeaveTypeId == leaveTypeId, cancellationToken);

            return referencedByRequest;
        }
    }
}
