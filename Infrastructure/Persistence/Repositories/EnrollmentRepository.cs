using Domain.Common;
using Domain.Entities;
using Domain.Enums;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class EnrollmentRepository : Repository<Enrollment, Guid>, IEnrollmentRepository
    {
        public EnrollmentRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<Enrollment>> GetPagedByFilterAsync(Guid? studentId, Guid? academicClassId, Guid? classSectionId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            // ClassSection + its AcademicClass are included so the DTO can flatten grade/section
            // info without a second call per row.
            IQueryable<Enrollment> enrollmentsQuery = DbSet
                .Include(enrollment => enrollment.Student)
                .Include(enrollment => enrollment.ClassSection)
                    .ThenInclude(section => section.AcademicClass);

            if (studentId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(enrollment => enrollment.StudentId == studentId.Value);
            }

            if (academicClassId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(enrollment => enrollment.ClassSection.AcademicClassId == academicClassId.Value);
            }

            if (classSectionId.HasValue)
            {
                enrollmentsQuery = enrollmentsQuery.Where(enrollment => enrollment.ClassSectionId == classSectionId.Value);
            }

            var totalCount = await enrollmentsQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await enrollmentsQuery
                .OrderBy(enrollment => enrollment.RollNumber)
                .ThenBy(enrollment => enrollment.Id)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<Enrollment>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<Enrollment> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var enrollment = await DbSet
                .Include(e => e.Student)
                .Include(e => e.ClassSection)
                    .ThenInclude(section => section.AcademicClass)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

            return enrollment;
        }

        public async Task<IReadOnlyList<Enrollment>> GetActiveByStudentAsync(Guid studentId, CancellationToken cancellationToken = default)
        {
            // The student profile's "current class" lookup: active rows with the year included
            // so the caller can prefer the IsCurrent academic year.
            var activeEnrollments = await DbSet
                .Include(e => e.ClassSection)
                    .ThenInclude(section => section.AcademicClass)
                        .ThenInclude(academicClass => academicClass.AcademicYear)
                .Where(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Enrolled)
                .ToListAsync(cancellationToken);

            return activeEnrollments;
        }

        public async Task<bool> EnrollmentExistsAsync(Guid studentId, Guid classSectionId, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the composite unique index still sees soft-deleted rows.
            var enrollmentExists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(enrollment => enrollment.StudentId == studentId
                    && enrollment.ClassSectionId == classSectionId, cancellationToken);

            return enrollmentExists;
        }

        public async Task<bool> HasActiveEnrollmentInYearAsync(Guid studentId, Guid academicYearId, Guid? excludeEnrollmentId, CancellationToken cancellationToken = default)
        {
            // One Enrolled-status row per student per academic year -- the guard against the same
            // student sitting in two grades/sections of one year at the same time.
            IQueryable<Enrollment> activeQuery = DbSet
                .Where(enrollment => enrollment.StudentId == studentId
                    && enrollment.Status == EnrollmentStatus.Enrolled
                    && enrollment.ClassSection.AcademicClass.AcademicYearId == academicYearId);

            if (excludeEnrollmentId.HasValue)
            {
                activeQuery = activeQuery.Where(enrollment => enrollment.Id != excludeEnrollmentId.Value);
            }

            var hasActiveEnrollment = await activeQuery.AnyAsync(cancellationToken);
            return hasActiveEnrollment;
        }

        public async Task<int> CountActiveBySectionAsync(Guid classSectionId, CancellationToken cancellationToken = default)
        {
            var activeCount = await DbSet
                .CountAsync(enrollment => enrollment.ClassSectionId == classSectionId
                    && enrollment.Status == EnrollmentStatus.Enrolled, cancellationToken);

            return activeCount;
        }

        public async Task<bool> RollNumberExistsInSectionAsync(Guid classSectionId, string rollNumber, Guid? excludeEnrollmentId, CancellationToken cancellationToken = default)
        {
            IQueryable<Enrollment> rollNumberQuery = DbSet
                .Where(enrollment => enrollment.ClassSectionId == classSectionId
                    && enrollment.RollNumber == rollNumber);

            if (excludeEnrollmentId.HasValue)
            {
                rollNumberQuery = rollNumberQuery.Where(enrollment => enrollment.Id != excludeEnrollmentId.Value);
            }

            var rollNumberExists = await rollNumberQuery.AnyAsync(cancellationToken);
            return rollNumberExists;
        }

        public async Task<IReadOnlyList<EnrollmentSubject>> GetElectiveSubjectsAsync(Guid enrollmentId, CancellationToken cancellationToken = default)
        {
            var electiveSubjects = await DbContext.Set<EnrollmentSubject>()
                .Include(electiveSubject => electiveSubject.ClassSubject)
                .Where(electiveSubject => electiveSubject.EnrollmentId == enrollmentId)
                .ToListAsync(cancellationToken);

            return electiveSubjects;
        }

        public async Task<EnrollmentSubject> GetElectiveSubjectByIdAsync(Guid electiveSubjectId, CancellationToken cancellationToken = default)
        {
            var electiveSubject = await DbContext.Set<EnrollmentSubject>()
                .FirstOrDefaultAsync(es => es.Id == electiveSubjectId, cancellationToken);

            return electiveSubject;
        }

        public async Task<bool> ElectiveSubjectExistsAsync(Guid enrollmentId, Guid classSubjectId, CancellationToken cancellationToken = default)
        {
            var electiveSubjectExists = await DbContext.Set<EnrollmentSubject>()
                .AnyAsync(es => es.EnrollmentId == enrollmentId && es.ClassSubjectId == classSubjectId, cancellationToken);

            return electiveSubjectExists;
        }

        public async Task AddElectiveSubjectAsync(EnrollmentSubject electiveSubject, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<EnrollmentSubject>().AddAsync(electiveSubject, cancellationToken);
        }

        public void RemoveElectiveSubject(EnrollmentSubject electiveSubject)
        {
            DbContext.Set<EnrollmentSubject>().Remove(electiveSubject);
        }
    }
}
