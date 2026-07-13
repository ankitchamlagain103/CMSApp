using Domain.Common;
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

        public async Task<PagedResult<AcademicYear>> GetPagedOrderedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await DbSet.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await DbSet
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
