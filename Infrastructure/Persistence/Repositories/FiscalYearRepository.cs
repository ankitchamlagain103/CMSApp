using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class FiscalYearRepository : Repository<FiscalYear, Guid>, IFiscalYearRepository
    {
        public FiscalYearRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<FiscalYear>> GetPagedOrderedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await DbSet.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await DbSet
                .OrderByDescending(year => year.StartDate)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<FiscalYear>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index on "code" still sees soft-deleted rows.
            var codeExists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(year => year.Code == code, cancellationToken);

            return codeExists;
        }

        public async Task<IReadOnlyList<FiscalYear>> GetCurrentYearsAsync(CancellationToken cancellationToken = default)
        {
            var currentYears = await DbSet
                .Where(year => year.IsCurrent)
                .ToListAsync(cancellationToken);

            return currentYears;
        }

        public async Task<FiscalYear> GetCurrentYearAsync(CancellationToken cancellationToken = default)
        {
            var currentYear = await DbSet
                .FirstOrDefaultAsync(year => year.IsCurrent, cancellationToken);

            return currentYear;
        }

        public async Task<bool> HasTaxSlabsAsync(Guid fiscalYearId, CancellationToken cancellationToken = default)
        {
            var hasTaxSlabs = await DbContext.Set<TaxSlab>()
                .AnyAsync(slab => slab.FiscalYearId == fiscalYearId, cancellationToken);

            return hasTaxSlabs;
        }

        public async Task<IReadOnlyList<TaxSlab>> GetTaxSlabsAsync(Guid fiscalYearId, TaxAssessmentType? assessmentType, CancellationToken cancellationToken = default)
        {
            IQueryable<TaxSlab> slabsQuery = DbContext.Set<TaxSlab>()
                .Where(slab => slab.FiscalYearId == fiscalYearId);

            if (assessmentType.HasValue)
            {
                slabsQuery = slabsQuery.Where(slab => slab.AssessmentType == assessmentType.Value);
            }

            var slabs = await slabsQuery
                .OrderBy(slab => slab.AssessmentType)
                .ThenBy(slab => slab.SlabOrder)
                .ToListAsync(cancellationToken);

            return slabs;
        }

        public async Task<TaxSlab> GetTaxSlabByIdAsync(Guid taxSlabId, CancellationToken cancellationToken = default)
        {
            var taxSlab = await DbContext.Set<TaxSlab>()
                .FirstOrDefaultAsync(slab => slab.Id == taxSlabId, cancellationToken);

            return taxSlab;
        }

        public async Task AddTaxSlabAsync(TaxSlab taxSlab, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<TaxSlab>().AddAsync(taxSlab, cancellationToken);
        }

        public void RemoveTaxSlab(TaxSlab taxSlab)
        {
            DbContext.Set<TaxSlab>().Remove(taxSlab);
        }
    }
}
