using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class TimePeriodRepository : Repository<TimePeriod, Guid>, ITimePeriodRepository
    {
        public TimePeriodRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public new async Task<PagedResult<TimePeriod>> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await DbSet.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await DbSet
                .OrderBy(t => t.Order)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<TimePeriod>
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

        public async Task<bool> IsReferencedAsync(Guid timePeriodId, CancellationToken cancellationToken = default)
        {
            var referencedByExam = await DbContext.Set<Exam>()
                .AnyAsync(e => e.TimePeriodId == timePeriodId, cancellationToken);
            if (referencedByExam)
            {
                return true;
            }

            var referencedByAssignment = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(a => a.TimePeriodId == timePeriodId, cancellationToken);
            if (referencedByAssignment)
            {
                return true;
            }

            var referencedByMapping = await DbContext.Set<ClassTimePeriod>()
                .AnyAsync(m => m.TimePeriodId == timePeriodId, cancellationToken);

            return referencedByMapping;
        }

        public async Task<List<ClassTimePeriod>> GetMappingsByClassIdAsync(Guid academicClassId, CancellationToken cancellationToken = default)
        {
            var mappings = await DbContext.Set<ClassTimePeriod>()
                .Include(m => m.TimePeriod)
                .Include(m => m.AcademicClass)
                .Where(m => m.AcademicClassId == academicClassId)
                .OrderBy(m => m.TimePeriod.Order)
                .ToListAsync(cancellationToken);

            return mappings;
        }

        public async Task<bool> IsMappedToClassAsync(Guid academicClassId, Guid timePeriodId, CancellationToken cancellationToken = default)
        {
            var isMapped = await DbContext.Set<ClassTimePeriod>()
                .AnyAsync(m => m.AcademicClassId == academicClassId && m.TimePeriodId == timePeriodId, cancellationToken);

            return isMapped;
        }

        public async Task AddMappingAsync(ClassTimePeriod mapping, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<ClassTimePeriod>().AddAsync(mapping, cancellationToken);
        }

        public async Task<ClassTimePeriod> GetMappingAsync(Guid academicClassId, Guid timePeriodId, CancellationToken cancellationToken = default)
        {
            var mapping = await DbContext.Set<ClassTimePeriod>()
                .Include(m => m.TimePeriod)
                .FirstOrDefaultAsync(m => m.AcademicClassId == academicClassId && m.TimePeriodId == timePeriodId, cancellationToken);

            return mapping;
        }

        public void RemoveMapping(ClassTimePeriod mapping)
        {
            DbContext.Set<ClassTimePeriod>().Remove(mapping);
        }
    }
}
