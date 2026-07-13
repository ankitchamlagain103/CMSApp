using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository: Enrollment plus its elective EnrollmentSubject children.
    public interface IEnrollmentRepository : IRepository<Enrollment, Guid>
    {
        Task<PagedResult<Enrollment>> GetPagedByFilterAsync(Guid? studentId, Guid? academicClassId, Guid? classSectionId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<Enrollment> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Enrollment>> GetActiveByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<bool> EnrollmentExistsAsync(Guid studentId, Guid classSectionId, CancellationToken cancellationToken = default);

        Task<bool> HasActiveEnrollmentInYearAsync(Guid studentId, Guid academicYearId, Guid? excludeEnrollmentId, CancellationToken cancellationToken = default);

        Task<int> CountActiveBySectionAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        Task<bool> RollNumberExistsInSectionAsync(Guid classSectionId, string rollNumber, Guid? excludeEnrollmentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EnrollmentSubject>> GetElectiveSubjectsAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<EnrollmentSubject> GetElectiveSubjectByIdAsync(Guid electiveSubjectId, CancellationToken cancellationToken = default);

        Task<bool> ElectiveSubjectExistsAsync(Guid enrollmentId, Guid classSubjectId, CancellationToken cancellationToken = default);

        Task AddElectiveSubjectAsync(EnrollmentSubject electiveSubject, CancellationToken cancellationToken = default);

        void RemoveElectiveSubject(EnrollmentSubject electiveSubject);
    }
}
