using Application.Common.Models;
using Application.Enrollments.Commands;
using Application.Enrollments.Dtos;
using Application.Enrollments.Queries;

namespace Application.Enrollments
{
    public interface IEnrollmentService
    {
        Task<CommonResponse<EnrollmentDto>> CreateEnrollmentAsync(CreateEnrollmentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<EnrollmentDto>> GetEnrollmentByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<EnrollmentDto>>> GetEnrollmentsAsync(GetEnrollmentsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<EnrollmentDto>> UpdateEnrollmentAsync(Guid id, UpdateEnrollmentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteEnrollmentAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<EnrollmentSubjectDto>> AddElectiveSubjectAsync(Guid enrollmentId, Guid classSubjectId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveElectiveSubjectAsync(Guid enrollmentId, Guid electiveSubjectId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EnrollmentSubjectDto>>> GetElectiveSubjectsAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    }
}
