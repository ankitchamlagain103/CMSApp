using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class GuardianRepository : Repository<Guardian, Guid>, IGuardianRepository
    {
        public GuardianRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Guardian>> GetPagedByFilterAsync(GuardianFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Guardian> guardiansQuery = DbSet;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchPattern = "%" + filter.Search.Trim() + "%";
                guardiansQuery = guardiansQuery.Where(guardian =>
                    EF.Functions.ILike(guardian.FirstName, searchPattern)
                    || EF.Functions.ILike(guardian.LastName, searchPattern));
            }

            if (!string.IsNullOrWhiteSpace(filter.Phone))
            {
                var phonePattern = "%" + filter.Phone.Trim() + "%";
                guardiansQuery = guardiansQuery.Where(guardian => EF.Functions.ILike(guardian.Phone, phonePattern));
            }

            if (filter.FromDate.HasValue)
            {
                var fromTs = new DateTimeOffset(filter.FromDate.Value.Date, TimeSpan.Zero);
                guardiansQuery = guardiansQuery.Where(guardian => guardian.CreatedTs >= fromTs);
            }

            if (filter.ToDate.HasValue)
            {
                var toTs = new DateTimeOffset(filter.ToDate.Value.Date.AddDays(1), TimeSpan.Zero);
                guardiansQuery = guardiansQuery.Where(guardian => guardian.CreatedTs < toTs);
            }

            var totalCount = await guardiansQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await guardiansQuery
                .OrderBy(guardian => guardian.FirstName)
                .ThenBy(guardian => guardian.LastName)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Guardian>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> HasStudentLinksAsync(Guid guardianId, CancellationToken cancellationToken = default)
        {
            var hasStudentLinks = await DbContext.Set<StudentGuardian>()
                .AnyAsync(link => link.GuardianId == guardianId, cancellationToken);

            return hasStudentLinks;
        }
    }
}
