using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class StudentPromotionRepository : Repository<StudentPromotion, Guid>, IStudentPromotionRepository
    {
        public StudentPromotionRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<bool> HasPromotionFromEnrollmentAsync(Guid fromEnrollmentId, CancellationToken cancellationToken = default)
        {
            var exists = await DbSet
                .AnyAsync(p => p.FromEnrollmentId == fromEnrollmentId, cancellationToken);

            return exists;
        }

        public async Task<PagedResult<StudentPromotion>> GetPagedByFilterAsync(Guid? studentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<StudentPromotion> promotionsQuery = DbSet
                .Include(p => p.Student)
                .Include(p => p.FromEnrollment)
                    .ThenInclude(e => e.ClassSection)
                        .ThenInclude(cs => cs.AcademicClass)
                .Include(p => p.ToEnrollment)
                    .ThenInclude(e => e.ClassSection)
                        .ThenInclude(cs => cs.AcademicClass);

            if (studentId.HasValue)
            {
                promotionsQuery = promotionsQuery.Where(p => p.StudentId == studentId.Value);
            }

            var totalCount = await promotionsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await promotionsQuery
                .OrderByDescending(p => p.PromotionDate)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<StudentPromotion>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<StudentPromotion> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var promotion = await DbSet
                .Include(p => p.Student)
                .Include(p => p.FromEnrollment)
                    .ThenInclude(e => e.ClassSection)
                        .ThenInclude(cs => cs.AcademicClass)
                .Include(p => p.ToEnrollment)
                    .ThenInclude(e => e.ClassSection)
                        .ThenInclude(cs => cs.AcademicClass)
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            return promotion;
        }
    }
}
