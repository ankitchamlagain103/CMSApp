using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class TeacherRepository : Repository<Teacher, Guid>, ITeacherRepository
    {
        public TeacherRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Teacher>> GetPagedByFilterAsync(string search, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Teacher> teachersQuery = DbSet;

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchPattern = "%" + search.Trim() + "%";
                teachersQuery = teachersQuery.Where(teacher =>
                    EF.Functions.ILike(teacher.FirstName, searchPattern)
                    || EF.Functions.ILike(teacher.LastName, searchPattern)
                    || EF.Functions.ILike(teacher.EmployeeNo, searchPattern));
            }

            var totalCount = await teachersQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await teachersQuery
                .OrderBy(teacher => teacher.FirstName)
                .ThenBy(teacher => teacher.LastName)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Teacher>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<bool> EmployeeNoExistsAsync(string employeeNo, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index still sees soft-deleted rows.
            var employeeNoExists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(teacher => teacher.EmployeeNo == employeeNo, cancellationToken);

            return employeeNoExists;
        }

        public async Task<bool> HasAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            var hasAssignments = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.TeacherId == teacherId, cancellationToken);

            return hasAssignments;
        }

        public async Task<IReadOnlyList<TeacherQualification>> GetQualificationsAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            var qualifications = await DbContext.Set<TeacherQualification>()
                .Where(qualification => qualification.TeacherId == teacherId)
                .OrderByDescending(qualification => qualification.CompletionYear)
                .ToListAsync(cancellationToken);

            return qualifications;
        }

        public async Task<TeacherQualification> GetQualificationByIdAsync(Guid qualificationId, CancellationToken cancellationToken = default)
        {
            var qualification = await DbContext.Set<TeacherQualification>()
                .FirstOrDefaultAsync(q => q.Id == qualificationId, cancellationToken);

            return qualification;
        }

        public async Task AddQualificationAsync(TeacherQualification qualification, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<TeacherQualification>().AddAsync(qualification, cancellationToken);
        }

        public void RemoveQualification(TeacherQualification qualification)
        {
            DbContext.Set<TeacherQualification>().Remove(qualification);
        }

        public async Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            var assignments = await DbContext.Set<TeacherAssignment>()
                .Include(assignment => assignment.ClassSubject)
                .Include(assignment => assignment.ClassSection)
                .Where(assignment => assignment.TeacherId == teacherId)
                .ToListAsync(cancellationToken);

            return assignments;
        }

        public async Task<TeacherAssignment> GetAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default)
        {
            var assignment = await DbContext.Set<TeacherAssignment>()
                .FirstOrDefaultAsync(a => a.Id == assignmentId, cancellationToken);

            return assignment;
        }

        public async Task<bool> AssignmentExistsAsync(Guid teacherId, Guid classSubjectId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            // Exact triple match, null section included -- the unique index can't catch the
            // duplicate-null case (Postgres treats NULLs as distinct), so this check must.
            var assignmentExists = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.TeacherId == teacherId
                    && assignment.ClassSubjectId == classSubjectId
                    && assignment.ClassSectionId == classSectionId, cancellationToken);

            return assignmentExists;
        }

        public async Task<bool> ClassTeacherExistsForSectionAsync(Guid classSectionId, CancellationToken cancellationToken = default)
        {
            // "At most one class teacher per ClassSection" -- regardless of which subject the
            // class-teacher assignment rides on.
            var classTeacherExists = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.IsClassTeacher
                    && assignment.ClassSectionId == classSectionId, cancellationToken);

            return classTeacherExists;
        }

        public async Task AddAssignmentAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<TeacherAssignment>().AddAsync(assignment, cancellationToken);
        }

        public void RemoveAssignment(TeacherAssignment assignment)
        {
            DbContext.Set<TeacherAssignment>().Remove(assignment);
        }

        public async Task<IReadOnlyList<TeacherDocument>> GetDocumentsAsync(Guid teacherId, CancellationToken cancellationToken = default)
        {
            var documents = await DbContext.Set<TeacherDocument>()
                .Where(document => document.TeacherId == teacherId)
                .OrderBy(document => document.DocumentTypeCode)
                .ThenBy(document => document.DocumentName)
                .ToListAsync(cancellationToken);

            return documents;
        }

        public async Task<TeacherDocument> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await DbContext.Set<TeacherDocument>()
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            return document;
        }

        public async Task AddDocumentAsync(TeacherDocument document, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<TeacherDocument>().AddAsync(document, cancellationToken);
        }

        public void RemoveDocument(TeacherDocument document)
        {
            DbContext.Set<TeacherDocument>().Remove(document);
        }
    }
}
