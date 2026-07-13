using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class AcademicClassRepository : Repository<AcademicClass, Guid>, IAcademicClassRepository
    {
        public AcademicClassRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PagedResult<AcademicClass>> GetPagedByFilterAsync(Guid? academicYearId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        {
            // Sections are included so the class list renders "one class, sections inside"
            // without a second call per row.
            IQueryable<AcademicClass> classesQuery = DbSet.Include(academicClass => academicClass.Sections);

            if (academicYearId.HasValue)
            {
                classesQuery = classesQuery.Where(academicClass => academicClass.AcademicYearId == academicYearId.Value);
            }

            var totalCount = await classesQuery.CountAsync(cancellationToken);
            var skipCount = (pageNumber - 1) * pageSize;
            var items = await classesQuery
                .OrderBy(academicClass => academicClass.GradeCode)
                .Skip(skipCount)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            var pagedResult = new PagedResult<AcademicClass>
            {
                Items = items,
                TotalCount = totalCount
            };

            return pagedResult;
        }

        public async Task<AcademicClass> GetWithSectionsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var academicClass = await DbSet
                .Include(c => c.Sections)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            return academicClass;
        }

        public async Task<IReadOnlyList<AcademicClass>> GetByYearWithChildrenAsync(Guid academicYearId, CancellationToken cancellationToken = default)
        {
            // Everything the clone-structure operation copies: sections and subject mappings
            // (with each subject's section nav, so section-scoped rows can be remapped by code).
            var classes = await DbSet
                .Include(c => c.Sections)
                .Include(c => c.ClassSubjects)
                    .ThenInclude(cs => cs.ClassSection)
                .Where(c => c.AcademicYearId == academicYearId)
                .OrderBy(c => c.GradeCode)
                .ToListAsync(cancellationToken);

            return classes;
        }

        public async Task<bool> CombinationExistsAsync(Guid academicYearId, string gradeCode, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the composite unique index still sees soft-deleted rows.
            var combinationExists = await DbSet
                .IgnoreQueryFilters()
                .AnyAsync(academicClass => academicClass.AcademicYearId == academicYearId
                    && academicClass.GradeCode == gradeCode, cancellationToken);

            return combinationExists;
        }

        public async Task<bool> HasSectionsAsync(Guid academicClassId, CancellationToken cancellationToken = default)
        {
            var hasSections = await DbContext.Set<ClassSection>()
                .AnyAsync(section => section.AcademicClassId == academicClassId, cancellationToken);

            return hasSections;
        }

        public async Task<ClassSection> GetSectionByIdAsync(Guid classSectionId, CancellationToken cancellationToken = default)
        {
            // AcademicClass is included because callers need the grade/year the section belongs to
            // (enrollment year-invariant, elective ownership checks).
            var classSection = await DbContext.Set<ClassSection>()
                .Include(section => section.AcademicClass)
                .FirstOrDefaultAsync(section => section.Id == classSectionId, cancellationToken);

            return classSection;
        }

        public async Task<IReadOnlyList<ClassSection>> GetSectionsAsync(Guid academicClassId, CancellationToken cancellationToken = default)
        {
            var sections = await DbContext.Set<ClassSection>()
                .Where(section => section.AcademicClassId == academicClassId)
                .OrderBy(section => section.SectionCode)
                .ToListAsync(cancellationToken);

            return sections;
        }

        public async Task<bool> SectionExistsAsync(Guid academicClassId, string sectionCode, CancellationToken cancellationToken = default)
        {
            // IgnoreQueryFilters: the composite unique index still sees soft-deleted rows.
            var sectionExists = await DbContext.Set<ClassSection>()
                .IgnoreQueryFilters()
                .AnyAsync(section => section.AcademicClassId == academicClassId
                    && section.SectionCode == sectionCode, cancellationToken);

            return sectionExists;
        }

        public async Task<bool> SectionHasEnrollmentsAsync(Guid classSectionId, CancellationToken cancellationToken = default)
        {
            var hasEnrollments = await DbContext.Set<Enrollment>()
                .AnyAsync(enrollment => enrollment.ClassSectionId == classSectionId, cancellationToken);

            return hasEnrollments;
        }

        public async Task<bool> SectionHasTeacherAssignmentsAsync(Guid classSectionId, CancellationToken cancellationToken = default)
        {
            var hasAssignments = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.ClassSectionId == classSectionId, cancellationToken);

            return hasAssignments;
        }

        public async Task<bool> SectionHasScopedSubjectsAsync(Guid classSectionId, CancellationToken cancellationToken = default)
        {
            var hasScopedSubjects = await DbContext.Set<ClassSubject>()
                .AnyAsync(cs => cs.ClassSectionId == classSectionId, cancellationToken);

            return hasScopedSubjects;
        }

        public async Task AddSectionAsync(ClassSection classSection, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<ClassSection>().AddAsync(classSection, cancellationToken);
        }

        public void RemoveSection(ClassSection classSection)
        {
            DbContext.Set<ClassSection>().Remove(classSection);
        }

        public async Task<ClassSubject> GetClassSubjectByIdAsync(Guid classSubjectId, CancellationToken cancellationToken = default)
        {
            var classSubject = await DbContext.Set<ClassSubject>()
                .Include(cs => cs.ClassSection)
                .FirstOrDefaultAsync(cs => cs.Id == classSubjectId, cancellationToken);

            return classSubject;
        }

        public async Task<IReadOnlyList<ClassSubject>> GetClassSubjectsAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default)
        {
            IQueryable<ClassSubject> subjectsQuery = DbContext.Set<ClassSubject>()
                .Include(cs => cs.ClassSection)
                .Where(cs => cs.AcademicClassId == academicClassId);

            // A section's effective subject list = class-wide rows + rows scoped to that section.
            if (classSectionId.HasValue)
            {
                subjectsQuery = subjectsQuery.Where(cs => cs.ClassSectionId == null
                    || cs.ClassSectionId == classSectionId.Value);
            }

            var classSubjects = await subjectsQuery
                .OrderBy(cs => cs.DisplayOrder)
                .ThenBy(cs => cs.SubjectCode)
                .ToListAsync(cancellationToken);

            return classSubjects;
        }

        public async Task<IReadOnlyList<ClassSubject>> GetClassSubjectRowsByCodeAsync(Guid academicClassId, string subjectCode, CancellationToken cancellationToken = default)
        {
            var rows = await DbContext.Set<ClassSubject>()
                .Where(cs => cs.AcademicClassId == academicClassId && cs.SubjectCode == subjectCode)
                .ToListAsync(cancellationToken);

            return rows;
        }

        public async Task<bool> ClassSubjectInUseAsync(Guid classSubjectId, CancellationToken cancellationToken = default)
        {
            var usedByTeacherAssignment = await DbContext.Set<TeacherAssignment>()
                .AnyAsync(assignment => assignment.ClassSubjectId == classSubjectId, cancellationToken);

            if (usedByTeacherAssignment)
            {
                return true;
            }

            var usedByEnrollmentSubject = await DbContext.Set<EnrollmentSubject>()
                .AnyAsync(electiveSubject => electiveSubject.ClassSubjectId == classSubjectId, cancellationToken);

            return usedByEnrollmentSubject;
        }

        public async Task AddClassSubjectAsync(ClassSubject classSubject, CancellationToken cancellationToken = default)
        {
            await DbContext.Set<ClassSubject>().AddAsync(classSubject, cancellationToken);
        }

        public void RemoveClassSubject(ClassSubject classSubject)
        {
            DbContext.Set<ClassSubject>().Remove(classSubject);
        }
    }
}
