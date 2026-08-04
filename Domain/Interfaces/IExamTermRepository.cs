using Domain.Common;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository: ExamTerm plus its Exam children (same "owns its child link entities"
    // convention as IAcademicClassRepository/ITeacherRepository). Exam merges what used to be two
    // separate entities (a term-wide "Exam" container plus a per-subject-per-section
    // "ExamSchedule" child) into one row -- see the doc comment on the Exam entity for why.
    // Entities loaded via the Get*ByIdAsync methods below are tracked by EF, so an in-place
    // property edit followed by IUnitOfWork.SaveChangesAsync is enough -- no explicit Update
    // method needed for Exam, same convention ClassSubject/TeacherAssignment already use.
    public interface IExamTermRepository : IRepository<ExamTerm, Guid>
    {
        Task<PagedResult<ExamTerm>> GetPagedByFilterAsync(Guid? academicYearId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> CodeExistsAsync(string code, Guid? excludeExamTermId, CancellationToken cancellationToken = default);

        Task<bool> HasExamsAsync(Guid examTermId, CancellationToken cancellationToken = default);

        // --- Exams ---

        Task<Exam> GetExamByIdAsync(Guid examId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Exam>> GetExamsAsync(Guid? examTermId, Guid? classSubjectId, CancellationToken cancellationToken = default);

        Task<bool> ExamExistsAsync(Guid examTermId, Guid classSubjectId, Guid? excludeExamId, CancellationToken cancellationToken = default);

        Task AddExamAsync(Exam exam, CancellationToken cancellationToken = default);

        void RemoveExam(Exam exam);

        // --- Marks Entry & Result Processing (Phase 2) ---

        Task<bool> HasMarksAsync(Guid examId, CancellationToken cancellationToken = default);

        Task<StudentExamMark> GetMarkByIdAsync(Guid markId, CancellationToken cancellationToken = default);

        Task<StudentExamMark> GetMarkByExamAndEnrollmentAsync(Guid examId, Guid enrollmentId, CancellationToken cancellationToken = default);

        // classSectionId narrows to marks whose Enrollment.ClassSectionId matches -- since an Exam
        // always covers the whole grade (no section of its own), this is what lets a section-taught
        // teacher's marks list/roster stay scoped to just their own section's students, per the
        // "same subject taught by different teachers in different sections" case.
        Task<IReadOnlyList<StudentExamMark>> GetMarksAsync(Guid? examId, Guid? enrollmentId, Guid? classSectionId, CancellationToken cancellationToken = default);

        // Batched lookup for result generation -- every mark across a whole term's final-exam
        // rows in one query, same "avoid N+1 across enrollments" convention as
        // IEnrollmentRepository.GetDiscountsByEnrollmentIdsAsync.
        Task<IReadOnlyList<StudentExamMark>> GetMarksByExamIdsAsync(IReadOnlyList<Guid> examIds, CancellationToken cancellationToken = default);

        Task AddMarkAsync(StudentExamMark mark, CancellationToken cancellationToken = default);

        void RemoveMark(StudentExamMark mark);

        // Every Exam within a term, optionally narrowed to one grade (ClassSubject.AcademicClassId)
        // -- since the revised design has no "final exam" concept, every Exam row for the term
        // counts toward result generation.
        Task<IReadOnlyList<Exam>> GetExamsByTermAsync(Guid examTermId, Guid? academicClassId, CancellationToken cancellationToken = default);

        Task<StudentResult> GetResultByIdAsync(Guid id, CancellationToken cancellationToken = default);

        // IgnoreQueryFilters: a soft-deleted row (from a lifted withhold) must still be found and
        // resurrected rather than colliding with the unique (EnrollmentId, ExamTermId) index on
        // the next regenerate -- same "soft-deleted row keeps its identity reserved" convention as
        // Menu/AcademicYear/ClassSection.
        Task<StudentResult> GetResultByEnrollmentAndTermIgnoringFiltersAsync(Guid enrollmentId, Guid examTermId, CancellationToken cancellationToken = default);

        Task<PagedResult<StudentResult>> GetResultsPagedByFilterAsync(Guid? examTermId, Guid? classSectionId, Guid? enrollmentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentResult>> GetResultsForRankingAsync(Guid examTermId, Guid classSectionId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentResult>> GetResultsByTermAsync(Guid examTermId, CancellationToken cancellationToken = default);

        Task AddResultAsync(StudentResult result, CancellationToken cancellationToken = default);

        void RemoveResult(StudentResult result);
    }
}
