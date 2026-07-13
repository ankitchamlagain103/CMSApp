using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository: Student plus its StudentGuardian links.
    public interface IStudentRepository : IRepository<Student, Guid>
    {
        Task<PagedResult<Student>> GetPagedByFilterAsync(string search, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> AdmissionNoExistsAsync(string admissionNo, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentGuardian>> GetGuardianLinksAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<StudentGuardian> GetGuardianLinkByIdAsync(Guid linkId, CancellationToken cancellationToken = default);

        Task<bool> GuardianLinkExistsAsync(Guid studentId, Guid guardianId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentGuardian>> GetPrimaryGuardianLinksAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task AddGuardianLinkAsync(StudentGuardian link, CancellationToken cancellationToken = default);

        void RemoveGuardianLink(StudentGuardian link);
    }
}
