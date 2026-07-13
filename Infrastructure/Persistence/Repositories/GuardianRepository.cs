using Domain.Common;
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

        public async Task<PagedResult<Guardian>> GetPagedOrderedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            var totalCount = await DbSet.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await DbSet
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
