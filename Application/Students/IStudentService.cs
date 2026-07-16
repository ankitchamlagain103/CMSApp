using Application.Common.Models;
using Application.Students.Commands;
using Application.Students.Dtos;
using Application.Students.Queries;

namespace Application.Students
{
    public interface IStudentService
    {
        Task<CommonResponse<StudentDto>> CreateStudentAsync(CreateStudentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentDto>> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<StudentDto>>> GetStudentsAsync(GetStudentsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentDto>> UpdateStudentAsync(Guid id, UpdateStudentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteStudentAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentGuardianDto>> LinkGuardianAsync(Guid studentId, LinkGuardianCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> UnlinkGuardianAsync(Guid studentId, Guid linkId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<StudentGuardianDto>>> GetGuardiansAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentDocumentDto>> UploadDocumentAsync(Guid studentId, UploadStudentDocumentCommand command, Stream fileContent, string originalFileName, string contentType, long fileSizeBytes, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<StudentDocumentDto>>> GetDocumentsAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentDocumentFileDto>> GetDocumentFileAsync(Guid studentId, Guid documentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteDocumentAsync(Guid studentId, Guid documentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<DocumentPreviewDto>> GetIdCardPreviewAsync(Guid studentId, CancellationToken cancellationToken = default);
    }
}
