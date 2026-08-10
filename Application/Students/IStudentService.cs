using Application.Common.Models;
using Application.Students.Commands;
using Application.Students.Dtos;
using Application.Students.Queries;

namespace Application.Students
{
    public interface IStudentService
    {
        Task<CommonResponse<StudentDto>> CreateStudentAsync(CreateStudentCommand command, CancellationToken cancellationToken = default);

        // 2026-08-05: profile-header shape only -- see this method's own doc comment for what
        // moved out (Guardians/full timetable/enrollment history, each now its own endpoint).
        Task<CommonResponse<StudentDto>> GetStudentByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // "History" tab -- every enrollment ever, oldest year first. Split out of
        // GetStudentByIdAsync 2026-08-05.
        Task<CommonResponse<List<StudentEnrollmentHistoryDto>>> GetEnrollmentHistoryAsync(Guid studentId, CancellationToken cancellationToken = default);

        // "Current Class" tab -- the student's subject/teacher/period routine for their active
        // enrollment. New 2026-08-05.
        Task<CommonResponse<StudentTimetableDto>> GetTimetableAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<StudentDto>>> GetStudentsAsync(GetStudentsQuery query, CancellationToken cancellationToken = default);

        // Self-service (2026-08-07) -- "a teacher should see his or her students details only":
        // scoped to the caller's own TeacherAssignment.ClassSectionId values. Resolved from the
        // JWT, no id parameter.
        Task<CommonResponse<PaginatedResponse<StudentDto>>> GetMyStudentsAsync(GetStudentsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentDto>> GetMyStudentByIdAsync(Guid id, CancellationToken cancellationToken = default);

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

        // Portal account provisioning retrofit (2026-07-27) -- for a student that didn't get a
        // login at creation time. Always the fixed RoleNames.Student role. 409 Conflict if one
        // already exists.
        Task<CommonResponse<StudentDto>> RegisterUserAccountAsync(Guid studentId, CancellationToken cancellationToken = default);
    }
}
