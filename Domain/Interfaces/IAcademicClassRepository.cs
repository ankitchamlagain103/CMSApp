using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository: AcademicClass plus its ClassSection and ClassSubject children.
    public interface IAcademicClassRepository : IRepository<AcademicClass, Guid>
    {
        Task<PagedResult<AcademicClass>> GetPagedByFilterAsync(Guid? academicYearId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<AcademicClass> GetWithSectionsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<AcademicClass>> GetByYearWithChildrenAsync(Guid academicYearId, CancellationToken cancellationToken = default);

        Task<bool> CombinationExistsAsync(Guid academicYearId, string gradeCode, CancellationToken cancellationToken = default);

        Task<bool> HasSectionsAsync(Guid academicClassId, CancellationToken cancellationToken = default);

        Task<ClassSection> GetSectionByIdAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ClassSection>> GetSectionsAsync(Guid academicClassId, CancellationToken cancellationToken = default);

        Task<bool> SectionExistsAsync(Guid academicClassId, string sectionCode, CancellationToken cancellationToken = default);

        Task<bool> SectionHasEnrollmentsAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        Task<bool> SectionHasTeacherAssignmentsAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        Task AddSectionAsync(ClassSection classSection, CancellationToken cancellationToken = default);

        void RemoveSection(ClassSection classSection);

        Task<bool> SectionHasScopedSubjectsAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        Task<ClassSubject> GetClassSubjectByIdAsync(Guid classSubjectId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ClassSubject>> GetClassSubjectsAsync(Guid academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<ClassSubject>> GetClassSubjectRowsByCodeAsync(Guid academicClassId, string subjectCode, CancellationToken cancellationToken = default);

        Task<bool> ClassSubjectInUseAsync(Guid classSubjectId, CancellationToken cancellationToken = default);

        Task AddClassSubjectAsync(ClassSubject classSubject, CancellationToken cancellationToken = default);

        void RemoveClassSubject(ClassSubject classSubject);
    }
}
