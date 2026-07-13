using Application.Common.Models;
using Application.Teachers.Commands;
using Application.Teachers.Dtos;
using Application.Teachers.Queries;

namespace Application.Teachers
{
    public interface ITeacherService
    {
        Task<CommonResponse<TeacherDto>> CreateTeacherAsync(CreateTeacherCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherDto>> GetTeacherByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<TeacherDto>>> GetTeachersAsync(GetTeachersQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherDto>> UpdateTeacherAsync(Guid id, UpdateTeacherCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteTeacherAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherQualificationDto>> AddQualificationAsync(Guid teacherId, AddTeacherQualificationCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveQualificationAsync(Guid teacherId, Guid qualificationId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<TeacherQualificationDto>>> GetQualificationsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherAssignmentDto>> AssignClassSubjectAsync(Guid teacherId, AssignTeacherCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveAssignmentAsync(Guid teacherId, Guid assignmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<TeacherAssignmentDto>>> GetAssignmentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherDocumentDto>> UploadDocumentAsync(Guid teacherId, UploadTeacherDocumentCommand command, Stream fileContent, string originalFileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<TeacherDocumentDto>>> GetDocumentsAsync(Guid teacherId, CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherDocumentFileDto>> GetDocumentFileAsync(Guid teacherId, Guid documentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteDocumentAsync(Guid teacherId, Guid documentId, CancellationToken cancellationToken = default);
    }
}
