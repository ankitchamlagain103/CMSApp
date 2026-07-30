using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class GradeScaleRepository : Repository<GradeScale, Guid>, IGradeScaleRepository
    {
        public GradeScaleRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<bool> GradeExistsAsync(string grade, Guid? excludeGradeScaleId, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index still sees soft-deleted rows.
            IQueryable<GradeScale> query = DbSet
                .IgnoreQueryFilters()
                .Where(g => g.Grade == grade);

            if (excludeGradeScaleId.HasValue)
            {
                query = query.Where(g => g.Id != excludeGradeScaleId.Value);
            }

            var exists = await query.AnyAsync(cancellationToken);
            return exists;
        }

        public async Task<IReadOnlyList<GradeScale>> GetAllOrderedAsync(CancellationToken cancellationToken = default)
        {
            var gradeScales = await DbSet
                .OrderByDescending(g => g.MinPercent)
                .ToListAsync(cancellationToken);

            return gradeScales;
        }

        public async Task<GradeScale> FindByPercentageAsync(decimal percentage, CancellationToken cancellationToken = default)
        {
            var gradeScale = await DbSet
                .FirstOrDefaultAsync(g => percentage >= g.MinPercent && percentage <= g.MaxPercent, cancellationToken);

            return gradeScale;
        }
    }
}
