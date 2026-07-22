using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class FeeGenerationRunRepository : Repository<FeeGenerationRun, Guid>, IFeeGenerationRunRepository
    {
        public FeeGenerationRunRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<FeeGenerationRun>> GetPagedByFilterAsync(FeeGenerationRunFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<FeeGenerationRun> runsQuery = DbSet
                .Include(r => r.AcademicYear);

            if (filter.AcademicYearId.HasValue)
            {
                runsQuery = runsQuery.Where(r => r.AcademicYearId == filter.AcademicYearId.Value);
            }

            if (filter.BillingYear.HasValue)
            {
                runsQuery = runsQuery.Where(r => r.BillingYear == filter.BillingYear.Value);
            }

            if (filter.BillingMonth.HasValue)
            {
                runsQuery = runsQuery.Where(r => r.BillingMonth == filter.BillingMonth.Value);
            }

            var totalCount = await runsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await runsQuery
                .OrderByDescending(r => r.BillingYear)
                .ThenByDescending(r => r.BillingMonth)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<FeeGenerationRun>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<FeeGenerationRun> GetByIdWithYearAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var run = await DbSet
                .Include(r => r.AcademicYear)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            return run;
        }

        public async Task<FeeGenerationRun> GetByPeriodAsync(Guid academicYearId, int billingYear, int billingMonth, CancellationToken cancellationToken = default)
        {
            var run = await DbSet
                .FirstOrDefaultAsync(r => r.AcademicYearId == academicYearId
                    && r.BillingYear == billingYear
                    && r.BillingMonth == billingMonth, cancellationToken);

            return run;
        }
    }
}
