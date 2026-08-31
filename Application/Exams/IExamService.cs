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

        // The batch-scheduling workflow: syncs every Exam for one class within one exam term
        // against the submitted routine in a single atomic save (create missing, update existing,
        // remove ones no longer listed) -- see SaveExamRoutineCommand's own doc comment for the
        // full sync/validation rules. Idempotent: re-submitting the same command produces the same
        // end state.
        Task<CommonResponse<SaveExamRoutineResultDto>> SaveExamRoutineAsync(SaveExamRoutineCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamDto>> GetExamByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // teacherId (optional) narrows the list to exams the given teacher is actually assigned
        // to grade -- resolved against TeacherAssignment.ClassSubjectId, the "teacher-wise"
        // marks-entry worklist.
        Task<CommonResponse<List<ExamDto>>> GetExamsAsync(Guid? examTermId, Guid? classSubjectId, Guid? teacherId, CancellationToken cancellationToken = default);

        // Self-service marks entry (2026-08-07) -- resolves the caller's own Employee from the
        // JWT and either narrows the result (GetMyExamsAsync) or 403s when the target exam's
        // subject isn't one of the caller's own TeacherAssignment rows. "Show them only the
        // subject(s) he or she teaches for marks entry."
        Task<CommonResponse<List<ExamDto>>> GetMyExamsAsync(Guid? examTermId, Guid? classSubjectId, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamDto>> UpdateExamAsync(Guid id, UpdateExamCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteExamAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamDto>> LockExamAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<ExamDto>> UnlockExamAsync(Guid id, CancellationToken cancellationToken = default);

        // --- Marks Entry ---

        Task<CommonResponse<StudentExamMarkDto>> CreateStudentExamMarkAsync(CreateStudentExamMarkCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentExamMarkDto>> CreateMyStudentExamMarkAsync(CreateStudentExamMarkCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentExamMarkDto>> GetStudentExamMarkByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // classSectionId (optional) narrows the list to one section's students -- the same subject
        // can be taught by different teachers in different sections, so a teacher-wise list needs
        // this to stay scoped to just their own section.
        Task<CommonResponse<List<StudentExamMarkDto>>> GetStudentExamMarksAsync(Guid? examId, Guid? enrollmentId, Guid? classSectionId, CancellationToken cancellationToken = default);

        // Self-service -- examId is required here (unlike the admin overload above) since it's
        // what the assignment check runs against.
        Task<CommonResponse<List<StudentExamMarkDto>>> GetMyStudentExamMarksAsync(Guid examId, Guid? enrollmentId, Guid? classSectionId, CancellationToken cancellationToken = default);

        // Admin, student-wise marks entry: every exam within one term the enrollment is eligible
        // for (across every subject), each carrying its existing mark (or null) -- the "pick a
        // student, enter every subject's marks in one screen" flow, complementing the teacher-wise,
        // per-exam roster below.
        Task<CommonResponse<List<StudentExamMarkByStudentItemDto>>> GetStudentExamMarksByStudentAsync(Guid enrollmentId, Guid examTermId, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentExamMarkDto>> UpdateStudentExamMarkAsync(Guid id, UpdateStudentExamMarkCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentExamMarkDto>> UpdateMyStudentExamMarkAsync(Guid id, UpdateStudentExamMarkCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteStudentExamMarkAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<BulkUpsertStudentExamMarksResultDto>> BulkUpsertStudentExamMarksAsync(BulkUpsertStudentExamMarksCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<BulkUpsertStudentExamMarksResultDto>> BulkUpsertMyStudentExamMarksAsync(BulkUpsertStudentExamMarksCommand command, CancellationToken cancellationToken = default);

        // Every enrolled student eligible for the exam's subject, optionally narrowed to one
        // section (classSectionId -- teacher-wise entry: pass the calling teacher's own
        // TeacherAssignment.ClassSectionId for this subject so a teacher only ever sees their own
        // section's roster, even though the Exam itself spans the whole grade) and/or filtered by
        // a name/admission-no search -- each row carries its existing mark (or null) so the UI can
        // search, pick one student, and enter/edit their marks without a separate lookup.
        Task<CommonResponse<List<ExamMarkRosterItemDto>>> GetStudentExamMarkRosterAsync(Guid examId, string search, Guid? classSectionId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<ExamMarkRosterItemDto>>> GetMyStudentExamMarkRosterAsync(Guid examId, string search, Guid? classSectionId, CancellationToken cancellationToken = default);

        // --- Result Processing ---

        Task<CommonResponse<GenerateExamResultsResultDto>> GenerateExamResultsAsync(GenerateExamResultsCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> PublishExamResultsAsync(Guid examTermId, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<StudentResultDto>>> GetStudentResultsAsync(Guid? examTermId, Guid? classSectionId, Guid? enrollmentId, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentResultDetailDto>> GetStudentResultByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentResultDto>> WithholdExamResultAsync(Guid id, WithholdExamResultCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> LiftExamResultWithholdAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
