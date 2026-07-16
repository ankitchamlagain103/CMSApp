using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class StudentRepository : Repository<Student, Guid>, IStudentRepository
    {
        public StudentRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Student>> GetPagedByFilterAsync(StudentFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            IQueryable<Student> studentsQuery = DbSet;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchPattern = "%" + filter.Search.Trim() + "%";
                studentsQuery = studentsQuery.Where(student =>
                    EF.Functions.ILike(student.FirstName, searchPattern)
                    || EF.Functions.ILike(student.LastName, searchPattern)
                    || EF.Functions.ILike(student.AdmissionNo, searchPattern));
            }

            if (!string.IsNullOrWhiteSpace(filter.Phone))
            {
                var phonePattern = "%" + filter.Phone.Trim() + "%";
                studentsQuery = studentsQuery.Where(student => EF.Functions.ILike(student.Phone, phonePattern));
            }

            if (filter.Status.HasValue)
            {
                studentsQuery = studentsQuery.Where(student => student.Status == filter.Status.Value);
            }

            if (filter.Gender.HasValue)
            {
                studentsQuery = studentsQuery.Where(student => student.Gender == filter.Gender.Value);
            }

            // Grade/AcademicYear/Section live on the student's Enrollment chain, not on Student
            // itself -- match against any Enrolled-status row (a "who is currently in Grade X /
            // year Y / section Z" snapshot, not full enrollment history).
            if (!string.IsNullOrWhiteSpace(filter.GradeCode) || filter.AcademicYearId.HasValue || filter.ClassSectionId.HasValue)
            {
                studentsQuery = studentsQuery.Where(student => student.Enrollments.Any(enrollment =>
                    enrollment.Status == EnrollmentStatus.Enrolled
                    && (string.IsNullOrWhiteSpace(filter.GradeCode) || enrollment.ClassSection.AcademicClass.GradeCode == filter.GradeCode)
                    && (!filter.AcademicYearId.HasValue || enrollment.ClassSection.AcademicClass.AcademicYearId == filter.AcademicYearId.Value)
                    && (!filter.ClassSectionId.HasValue || enrollment.ClassSectionId == filter.ClassSectionId.Value)));
            }

            if (filter.FromDate.HasValue || filter.ToDate.HasValue)
            {
                studentsQuery = ApplyDateRange(studentsQuery, filter);
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

        // CreatedDate compares against the audit column; EnrollmentDate matches if any of the
        // student's enrollments falls in range (ToDate is inclusive of the whole day).
        private static IQueryable<Student> ApplyDateRange(IQueryable<Student> query, StudentFilter filter)
        {
            if (filter.DateField == StudentDateField.EnrollmentDate)
            {
                query = query.Where(student => student.Enrollments.Any(enrollment =>
                    (!filter.FromDate.HasValue || (enrollment.EnrollmentDate.HasValue && enrollment.EnrollmentDate.Value >= filter.FromDate.Value.Date))
                    && (!filter.ToDate.HasValue || (enrollment.EnrollmentDate.HasValue && enrollment.EnrollmentDate.Value < filter.ToDate.Value.Date.AddDays(1)))));

                return query;
            }

            if (filter.FromDate.HasValue)
            {
                var fromTs = new DateTimeOffset(filter.FromDate.Value.Date, TimeSpan.Zero);
                query = query.Where(student => student.CreatedTs >= fromTs);
            }

            if (filter.ToDate.HasValue)
            {
                var toTs = new DateTimeOffset(filter.ToDate.Value.Date.AddDays(1), TimeSpan.Zero);
                query = query.Where(student => student.CreatedTs < toTs);
            }

            return query;
        }

        public async Task<bool> AdmissionNoExistsAsync(string admissionNo, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the unique index still sees soft-deleted rows.
            var admissionNoExists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(student => student.AdmissionNo == admissionNo, cancellationToken);

            return admissionNoExists;
        }

        public async Task<IReadOnlyList<string>> GetAdmissionNosByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: soft-deleted students keep their numbers reserved, so the
            // sequence must see them too.
            var admissionNos = await DbSet
                .IgnoreQueryFilters()
                .Where(student => student.AdmissionNo.StartsWith(prefix))
                .Select(student => student.AdmissionNo)
                .ToListAsync(cancellationToken);

            return admissionNos;
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

        public async Task<IReadOnlyList<StudentDocument>> GetDocumentsAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            var documents = await DbContext.Set<StudentDocument>()
                .Where(document => document.StudentId == studentId)
                .OrderBy(document => document.DocumentTypeCode)
                .ThenBy(document => document.DocumentName)
                .ToListAsync(cancellationToken);

            return documents;
        }

        public async Task<StudentDocument> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default)
        {
            var document = await DbContext.Set<StudentDocument>()
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken);

            return document;
        }

        public async Task AddDocumentAsync(StudentDocument document, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<StudentDocument>().AddAsync(document, cancellationToken);
        }

        public void RemoveDocument(StudentDocument document)
        {
            DbContext.Set<StudentDocument>().Remove(document);
        }
    }
}
