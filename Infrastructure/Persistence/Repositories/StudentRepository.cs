using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class StudentRepository : Repository<Student, Guid>, IStudentRepository
    {
        public StudentRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Student>> GetPagedByFilterAsync(string search, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Student> studentsQuery = DbSet;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchPattern = "%" + search.Trim() + "%";
                studentsQuery = studentsQuery.Where(student =>
                    EF.Functions.ILike(student.FirstName, searchPattern)
                    || EF.Functions.ILike(student.LastName, searchPattern)
                    || EF.Functions.ILike(student.AdmissionNo, searchPattern));
            }

            var totalCount = await studentsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await studentsQuery
                .OrderBy(student => student.FirstName)
                .ThenBy(student => student.LastName)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Student>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> AdmissionNoExistsAsync(string admissionNo, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index still sees soft-deleted rows.
            var admissionNoExists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(student => student.AdmissionNo == admissionNo, cancellationToken);

            return admissionNoExists;
        }

        public async Task<IReadOnlyList<StudentGuardian>> GetGuardianLinksAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var guardianLinks = await DbContext.Set<StudentGuardian>()
                .Include(link => link.Guardian)
                .Where(link => link.StudentId == studentId)
                .OrderByDescending(link => link.IsPrimary)
                .ToListAsync(cancellationToken);

            return guardianLinks;
        }

        public async Task<StudentGuardian> GetGuardianLinkByIdAsync(Guid linkId, CancellationToken cancellationToken = default)
        {
            var guardianLink = await DbContext.Set<StudentGuardian>()
                .FirstOrDefaultAsync(link => link.Id == linkId, cancellationToken);

            return guardianLink;
        }

        public async Task<bool> GuardianLinkExistsAsync(Guid studentId, Guid guardianId, CancellationToken cancellationToken = default)
        {
            var linkExists = await DbContext.Set<StudentGuardian>()
                .AnyAsync(link => link.StudentId == studentId && link.GuardianId == guardianId, cancellationToken);

            return linkExists;
        }

        public async Task<IReadOnlyList<StudentGuardian>> GetPrimaryGuardianLinksAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var primaryLinks = await DbContext.Set<StudentGuardian>()
                .Where(link => link.StudentId == studentId && link.IsPrimary)
                .ToListAsync(cancellationToken);

            return primaryLinks;
        }

        public async Task AddGuardianLinkAsync(StudentGuardian link, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<StudentGuardian>().AddAsync(link, cancellationToken);
        }

        public void RemoveGuardianLink(StudentGuardian link)
        {
            DbContext.Set<StudentGuardian>().Remove(link);
        }
    }
}
