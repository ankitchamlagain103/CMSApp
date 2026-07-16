using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class TeacherRepository : Repository<Teacher, Guid>, ITeacherRepository
    {
        public TeacherRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Teacher>> GetPagedByFilterAsync(TeacherFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            // Identity fields (name/phone/status/join date) live on Employee since the split --
            // filters join across the shared-PK nav.
            IQueryable<Teacher> teachersQuery = DbSet.Include(teacher => teacher.Employee);

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var searchPattern = "%" + filter.Search.Trim() + "%";
                teachersQuery = teachersQuery.Where(teacher =>
                    EF.Functions.ILike(teacher.Employee.FirstName, searchPattern)
                    || EF.Functions.ILike(teacher.Employee.LastName, searchPattern)
                    || EF.Functions.ILike(teacher.Employee.EmployeeCode, searchPattern));
            }

            if (!string.IsNullOrWhiteSpace(filter.Phone))
            {
                var phonePattern = "%" + filter.Phone.Trim() + "%";
                teachersQuery = teachersQuery.Where(teacher => EF.Functions.ILike(teacher.Employee.Phone, phonePattern));
            }

            if (filter.Status.HasValue)
            {
                teachersQuery = teachersQuery.Where(teacher => teacher.Employee.EmploymentStatus == filter.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.QualificationCode))
            {
                teachersQuery = teachersQuery.Where(teacher => teacher.Qualifications.Any(qualification => qualification.QualificationCode == filter.QualificationCode));
            }

            if (filter.FromDate.HasValue || filter.ToDate.HasValue)
            {
                teachersQuery = ApplyDateRange(teachersQuery, filter);
            }

            var totalCount = await teachersQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await teachersQuery
                .OrderBy(teacher => teacher.Employee.FirstName)
                .ThenBy(teacher => teacher.Employee.LastName)
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

        // CreatedDate compares against the Employee audit column; JoiningDate compares against
        // Employee.JoinDate (ToDate is inclusive of the whole day).
        private static IQueryable<Teacher> ApplyDateRange(IQueryable<Teacher> query, TeacherFilter filter)
        {
            if (filter.DateField == TeacherDateField.JoiningDate)
            {
                if (filter.FromDate.HasValue)
                {
                    query = query.Where(teacher => teacher.Employee.JoinDate.HasValue && teacher.Employee.JoinDate.Value >= filter.FromDate.Value.Date);
                }

                if (filter.ToDate.HasValue)
                {
                    query = query.Where(teacher => teacher.Employee.JoinDate.HasValue && teacher.Employee.JoinDate.Value < filter.ToDate.Value.Date.AddDays(1));
                }

                return query;
            }

            if (filter.FromDate.HasValue)
            {
                var fromTs = new DateTimeOffset(filter.FromDate.Value.Date, TimeSpan.Zero);
                query = query.Where(teacher => teacher.Employee.CreatedTs >= fromTs);
            }

            if (filter.ToDate.HasValue)
            {
                var toTs = new DateTimeOffset(filter.ToDate.Value.Date.AddDays(1), TimeSpan.Zero);
                query = query.Where(teacher => teacher.Employee.CreatedTs < toTs);
            }

            return query;
        }

        public async Task<Teacher> GetByIdWithEmployeeAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var teacher = await DbSet
                .Include(t => t.Employee)
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

            return teacher;
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
            // The year chain is included so the teacher detail can build service history from
            // the same query the assignments tab uses.
            var assignments = await DbContext.Set<TeacherAssignment>()
                .Include(assignment => assignment.ClassSubject)
                    .ThenInclude(cs => cs.AcademicClass)
                        .ThenInclude(c => c.AcademicYear)
                .Include(assignment => assignment.ClassSection)
                .Where(assignment => assignment.TeacherId == teacherId)
                .OrderBy(assignment => assignment.ClassSubject.AcademicClass.AcademicYear.StartDate)
                .ThenBy(assignment => assignment.ClassSubject.SubjectCode)
                .ToListAsync(cancellationToken);

            return assignments;
        }

        public async Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsByClassSubjectIdsAsync(IReadOnlyCollection<Guid> classSubjectIds, CancellationToken cancellationToken = default)
        {
            // Who teaches these subjects -- one batched query for the student profile's
            // subjects-studying block, Teacher (and its Employee, for the name) included.
            var assignments = await DbContext.Set<TeacherAssignment>()
                .Include(assignment => assignment.Teacher)
                    .ThenInclude(teacher => teacher.Employee)
                .Where(assignment => classSubjectIds.Contains(assignment.ClassSubjectId))
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
