using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class AcademicYearRepository : Repository<AcademicYear, Guid>, IAcademicYearRepository
    {
        public AcademicYearRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<AcademicYear>> GetPagedByFilterAsync(AcademicYearFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<AcademicYear> yearsQuery = DbSet;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchPattern = "%" + filter.Search.Trim() + "%";
                yearsQuery = yearsQuery.Where(year =>
                    EF.Functions.ILike(year.Code, searchPattern)
                    || EF.Functions.ILike(year.Name, searchPattern));
            }

            if (filter.IsCurrent.HasValue)
            {
                yearsQuery = yearsQuery.Where(year => year.IsCurrent == filter.IsCurrent.Value);
            }

            if (filter.Status.HasValue)
            {
                yearsQuery = yearsQuery.Where(year => year.Status == filter.Status.Value);
            }

            var totalCount = await yearsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await yearsQuery
                .OrderByDescending(year => year.StartDate)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<AcademicYear>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index on "code" still sees soft-deleted rows, so a
            // code held by a deleted year must count as taken (same lesson as MenuRepository).
            var codeExists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(year => year.Code == code, cancellationToken);

            return codeExists;
        }

        public async Task<IReadOnlyList<AcademicYear>> GetCurrentYearsAsync(CancellationToken cancellationToken = default)
        {
            var currentYears = await DbSet
                .Where(year => year.IsCurrent)
                .ToListAsync(cancellationToken);

            return currentYears;
        }

        public async Task<bool> HasClassesAsync(Guid academicYearId, CancellationToken cancellationToken = default)
        {
            var hasClasses = await DbContext.Set<AcademicClass>()
                .AnyAsync(academicClass => academicClass.AcademicYearId == academicYearId, cancellationToken);

            return hasClasses;
        }
    }
}
