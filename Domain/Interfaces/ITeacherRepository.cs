using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository: Teacher plus its TeacherQualification, TeacherAssignment, and
    // TeacherDocument children -- unchanged since the Employee/Teacher split. Identity fields
    // (name/phone/status/employee code/join date) and salary now live on Employee, so
    // GetPagedByFilterAsync/GetByIdWithEmployeeAsync join across via the shared-PK Employee nav.
    public interface ITeacherRepository : IRepository<Teacher, Guid>
    {
        Task<PagedResult<Teacher>> GetPagedByFilterAsync(TeacherFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<Teacher> GetByIdWithEmployeeAsync(Guid id, CancellationToken cancellationToken = default);

        Task<bool> HasAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeacherQualification>> GetQualificationsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<TeacherQualification> GetQualificationByIdAsync(Guid qualificationId, CancellationToken cancellationToken = default);

        Task AddQualificationAsync(TeacherQualification qualification, CancellationToken cancellationToken = default);

        void RemoveQualification(TeacherQualification qualification);

        Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<TeacherAssignment>> GetAssignmentsByClassSubjectIdsAsync(IReadOnlyCollection<Guid> classSubjectIds, CancellationToken cancellationToken = default);

        Task<TeacherAssignment> GetAssignmentByIdAsync(Guid assignmentId, CancellationToken cancellationToken = default);

        Task<bool> AssignmentExistsAsync(Guid teacherId, Guid classSubjectId, Guid? classSectionId, CancellationToken cancellationToken = default);

        Task<bool> ClassTeacherExistsForSectionAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        Task AddAssignmentAsync(TeacherAssignment assignment, CancellationToken cancellationToken = default);

        void RemoveAssignment(TeacherAssignment assignment);

        Task<IReadOnlyList<TeacherDocument>> GetDocumentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<TeacherDocument> GetDocumentByIdAsync(Guid documentId, CancellationToken cancellationToken = default);

        Task AddDocumentAsync(TeacherDocument document, CancellationToken cancellationToken = default);

        void RemoveDocument(TeacherDocument document);
    }
}
