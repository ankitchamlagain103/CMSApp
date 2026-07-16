using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class FeeStructureRepository : Repository<FeeStructure, Guid>, IFeeStructureRepository
    {
        public FeeStructureRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<FeeStructure>> GetPagedByFilterAsync(FeeStructureFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<FeeStructure> feeStructuresQuery = DbSet.Include(f => f.AcademicClass).Include(f => f.Items);

            if (filter.AcademicClassId.HasValue)
            {
                feeStructuresQuery = feeStructuresQuery.Where(f => f.AcademicClassId == filter.AcademicClassId.Value);
            }

            if (filter.AcademicYearId.HasValue)
            {
                feeStructuresQuery = feeStructuresQuery.Where(f => f.AcademicClass.AcademicYearId == filter.AcademicYearId.Value);
            }

            if (filter.Status.HasValue)
            {
                feeStructuresQuery = feeStructuresQuery.Where(f => f.Status == filter.Status.Value);
            }

            var totalCount = await feeStructuresQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await feeStructuresQuery
                .OrderBy(f => f.AcademicClass.GradeCode)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<FeeStructure>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<FeeStructure> GetByAcademicClassIdAsync(Guid academicClassId, CancellationToken cancellationToken = default)
        {
            var feeStructure = await DbSet
                .Include(f => f.AcademicClass)
                .Include(f => f.Items)
                .FirstOrDefaultAsync(f => f.AcademicClassId == academicClassId, cancellationToken);

            return feeStructure;
        }

        public async Task<FeeStructure> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var feeStructure = await DbSet
                .Include(f => f.AcademicClass)
                .Include(f => f.Items)
                .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

            return feeStructure;
        }

        public async Task<bool> ExistsForAcademicClassAsync(Guid academicClassId, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index still sees soft-deleted rows.
            var exists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(f => f.AcademicClassId == academicClassId, cancellationToken);

            return exists;
        }

        public async Task<FeeStructureItem> GetItemByIdAsync(Guid itemId, CancellationToken cancellationToken = default)
        {
            var item = await DbContext.Set<FeeStructureItem>().FirstOrDefaultAsync(i => i.Id == itemId, cancellationToken);

            return item;
        }

        public async Task AddItemAsync(FeeStructureItem item, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<FeeStructureItem>().AddAsync(item, cancellationToken);
        }

        public void RemoveItem(FeeStructureItem item)
        {
            DbContext.Set<FeeStructureItem>().Remove(item);
        }
    }
}
