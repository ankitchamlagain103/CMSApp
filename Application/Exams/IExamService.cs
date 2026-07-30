using Application.Common.Models;
using Application.Exams.Commands;
using Application.Exams.Dtos;

namespace Application.Exams
{
    public interface IExamService
    {
        Task<CommonResponse<ExamTermDto>> CreateExamTermAsync(CreateExamTermCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamTermDto>> GetExamTermByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<ExamTermDto>>> GetExamTermsAsync(Guid? academicYearId, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamTermDto>> UpdateExamTermAsync(Guid id, UpdateExamTermCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteExamTermAsync(Guid id, CancellationToken cancellationToken = default);

        // --- Exams (one subject's sitting per exam term -- no ClassSectionId; an exam always
        // covers the whole grade, see the Exam entity's doc comment) ---

        Task<CommonResponse<ExamDto>> CreateExamAsync(CreateExamCommand command, CancellationToken cancellationToken = default);

        // Schedules an Exam for every listed subject of one class (one exam term) in a single
        // call -- the "set the whole routine at once" flow. Skip-list style: an item whose
        // subject doesn't belong to the class, already has an exam this term, or names an
        // unknown invigilator is reported in the result's Skipped list rather than failing the
        // whole request.
        Task<CommonResponse<CreateExamRoutineResultDto>> CreateExamRoutineAsync(CreateExamRoutineCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamDto>> GetExamByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // teacherId (optional) narrows the list to exams the given teacher is actually assigned
        // to grade -- resolved against TeacherAssignment.ClassSubjectId, the "teacher-wise"
        // marks-entry worklist.
        Task<CommonResponse<List<ExamDto>>> GetExamsAsync(Guid? examTermId, Guid? classSubjectId, Guid? teacherId, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamDto>> UpdateExamAsync(Guid id, UpdateExamCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteExamAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamDto>> LockExamAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamDto>> UnlockExamAsync(Guid id, CancellationToken cancellationToken = default);

        // --- Marks Entry ---

        Task<CommonResponse<StudentExamMarkDto>> CreateStudentExamMarkAsync(CreateStudentExamMarkCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentExamMarkDto>> GetStudentExamMarkByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<StudentExamMarkDto>>> GetStudentExamMarksAsync(Guid? examId, Guid? enrollmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentExamMarkDto>> UpdateStudentExamMarkAsync(Guid id, UpdateStudentExamMarkCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteStudentExamMarkAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<BulkUpsertStudentExamMarksResultDto>> BulkUpsertStudentExamMarksAsync(BulkUpsertStudentExamMarksCommand command, CancellationToken cancellationToken = default);

        // Every enrolled student in the exam's section, optionally filtered by a name/admission-no
        // search -- each row carries its existing mark (or null) so the UI can search, pick one
        // student, and enter/edit their marks without a separate lookup.
        Task<CommonResponse<List<ExamMarkRosterItemDto>>> GetStudentExamMarkRosterAsync(Guid examId, string search, CancellationToken cancellationToken = default);

        // --- Result Processing ---

        Task<CommonResponse<GenerateExamResultsResultDto>> GenerateExamResultsAsync(GenerateExamResultsCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> PublishExamResultsAsync(Guid examTermId, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<StudentResultDto>>> GetStudentResultsAsync(Guid? examTermId, Guid? classSectionId, Guid? enrollmentId, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentResultDetailDto>> GetStudentResultByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentResultDto>> WithholdExamResultAsync(Guid id, WithholdExamResultCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> LiftExamResultWithholdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
