using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class LeaveRequestRepository : Repository<LeaveRequest, Guid>, ILeaveRequestRepository
    {
        public LeaveRequestRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<LeaveRequest> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var leaveRequest = await DbSet
                .Include(r => r.Employee)
                .Include(r => r.LeaveType)
                .Include(r => r.SubstituteEmployee)
                .Include(r => r.Substitutes)
                    .ThenInclude(s => s.Employee)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            return leaveRequest;
        }

        public async Task<PagedResult<LeaveRequest>> GetPagedByFilterAsync(LeaveRequestFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<LeaveRequest> requestsQuery = DbSet
                .Include(r => r.Employee)
                .Include(r => r.LeaveType)
                .Include(r => r.SubstituteEmployee);

            if (filter.EmployeeId.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.EmployeeId == filter.EmployeeId.Value);
            }

            if (filter.LeaveTypeId.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.LeaveTypeId == filter.LeaveTypeId.Value);
            }

            if (filter.ManagerStatus.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.ManagerStatus == filter.ManagerStatus.Value);
            }

            if (filter.HrStatus.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.HrStatus == filter.HrStatus.Value);
            }

            if (filter.FromDate.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.ToDate >= filter.FromDate.Value.Date);
            }

            if (filter.ToDate.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.FromDate <= filter.ToDate.Value.Date);
            }

            if (filter.IsPending.HasValue && filter.IsPending.Value)
            {
                requestsQuery = requestsQuery.Where(r => r.ManagerStatus == LeaveApprovalStatus.Pending || r.HrStatus == LeaveApprovalStatus.Pending);
            }

            var totalCount = await requestsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await requestsQuery
                .OrderByDescending(r => r.FromDate)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<LeaveRequest>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> HasOverlappingRequestAsync(Guid employeeId, DateTime fromDate, DateTime toDate, Guid? excludeRequestId, CancellationToken cancellationToken = default)
        {
            var overlapQuery = DbSet.Where(r => r.EmployeeId == employeeId
                && r.HrStatus != LeaveApprovalStatus.Rejected
                && r.FromDate <= toDate
                && r.ToDate >= fromDate);

            if (excludeRequestId.HasValue)
            {
                overlapQuery = overlapQuery.Where(r => r.Id != excludeRequestId.Value);
            }

            var hasOverlap = await overlapQuery.AnyAsync(cancellationToken);
            return hasOverlap;
        }

        public async Task<IReadOnlyList<LeaveRequest>> GetActiveRequestsInRangeAsync(Guid employeeId, Guid leaveTypeId, DateTime rangeStart, DateTime rangeEnd, Guid? excludeRequestId, CancellationToken cancellationToken = default)
        {
            var requestsQuery = DbSet.Where(r => r.EmployeeId == employeeId
                && r.LeaveTypeId == leaveTypeId
                && r.HrStatus != LeaveApprovalStatus.Rejected
                && r.FromDate <= rangeEnd
                && r.ToDate >= rangeStart);

            if (excludeRequestId.HasValue)
            {
                requestsQuery = requestsQuery.Where(r => r.Id != excludeRequestId.Value);
            }

            var requests = await requestsQuery.ToListAsync(cancellationToken);
            return requests;
        }

        public async Task<IReadOnlyList<LeaveSubstitute>> GetSubstitutesAsync(Guid leaveRequestId, CancellationToken cancellationToken = default)
        {
            var substitutes = await DbContext.Set<LeaveSubstitute>()
                .Where(s => s.LeaveRequestId == leaveRequestId)
                .ToListAsync(cancellationToken);

            return substitutes;
        }

        public async Task AddSubstituteAsync(LeaveSubstitute substitute, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<LeaveSubstitute>().AddAsync(substitute, cancellationToken);
        }

        public void RemoveSubstitute(LeaveSubstitute substitute)
        {
            DbContext.Set<LeaveSubstitute>().Remove(substitute);
        }
    }
}
