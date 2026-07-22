using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    // Aggregate repository: Enrollment plus its elective EnrollmentSubject children.
    public interface IEnrollmentRepository : IRepository<Enrollment, Guid>
    {
        Task<PagedResult<Enrollment>> GetPagedByFilterAsync(EnrollmentFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<Enrollment> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Enrollment>> GetActiveByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Enrollment>> GetHistoryByStudentAsync(Guid studentId, CancellationToken cancellationToken = default);

        Task<bool> EnrollmentExistsAsync(Guid studentId, Guid classSectionId, CancellationToken cancellationToken = default);

        Task<bool> HasActiveEnrollmentInYearAsync(Guid studentId, Guid academicYearId, Guid? excludeEnrollmentId, CancellationToken cancellationToken = default);

        Task<int> CountActiveBySectionAsync(Guid classSectionId, CancellationToken cancellationToken = default);

        Task<bool> RollNumberExistsInSectionAsync(Guid classSectionId, string rollNumber, Guid? excludeEnrollmentId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EnrollmentSubject>> GetElectiveSubjectsAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<EnrollmentSubject> GetElectiveSubjectByIdAsync(Guid electiveSubjectId, CancellationToken cancellationToken = default);

        Task<bool> ElectiveSubjectExistsAsync(Guid enrollmentId, Guid classSubjectId, CancellationToken cancellationToken = default);

        Task AddElectiveSubjectAsync(EnrollmentSubject electiveSubject, CancellationToken cancellationToken = default);

        void RemoveElectiveSubject(EnrollmentSubject electiveSubject);

        Task<IReadOnlyList<StudentDiscount>> GetDiscountsAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<StudentDiscount> GetDiscountByIdAsync(Guid discountId, CancellationToken cancellationToken = default);

        Task AddDiscountAsync(StudentDiscount discount, CancellationToken cancellationToken = default);

        void RemoveDiscount(StudentDiscount discount);

        Task<IReadOnlyList<AwardSummaryItem>> GetDiscountSummaryAsync(Guid? academicYearId, string discountTypeCode, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentScholarship>> GetScholarshipsAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<StudentScholarship> GetScholarshipByIdAsync(Guid scholarshipId, CancellationToken cancellationToken = default);

        Task AddScholarshipAsync(StudentScholarship scholarship, CancellationToken cancellationToken = default);

        void RemoveScholarship(StudentScholarship scholarship);

        Task<IReadOnlyList<AwardSummaryItem>> GetScholarshipSummaryAsync(Guid? academicYearId, string scholarshipTypeCode, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EnrollmentFeeSelection>> GetFeeSelectionsAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<EnrollmentFeeSelection> GetFeeSelectionByIdAsync(Guid feeSelectionId, CancellationToken cancellationToken = default);

        Task<bool> FeeSelectionExistsAsync(Guid enrollmentId, Guid feeStructureItemId, CancellationToken cancellationToken = default);

        Task AddFeeSelectionAsync(EnrollmentFeeSelection feeSelection, CancellationToken cancellationToken = default);

        void RemoveFeeSelection(EnrollmentFeeSelection feeSelection);

        // Cross-aggregate delete guard -- a FeeStructureItem can't be removed while an enrollment
        // has opted into it (same "refuse delete while children exist" convention as ClassSection/
        // ClassSubject).
        Task<bool> FeeSelectionExistsForItemAsync(Guid feeStructureItemId, CancellationToken cancellationToken = default);

        // Fee-generation batch inputs (fee redesign, 2026-07-16): every Enrolled enrollment of
        // one academic year (optionally one class, or one section), section->class chain
        // included, plus the per-enrollment configuration rows batched across the whole run
        // instead of queried per row.
        Task<IReadOnlyList<Enrollment>> GetEnrolledByYearAsync(Guid academicYearId, Guid? academicClassId, Guid? classSectionId, CancellationToken cancellationToken = default);

        // Student search for the fee module (2026-07-17): active (Enrolled) enrollments whose
        // student matches by name, admission no, or email -- the "find the student, get their
        // enrollment" entry point for fee generation, statements, and payments.
        Task<PagedResult<Enrollment>> SearchEnrolledByStudentAsync(Guid? academicYearId, string search, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        // Same filter as SearchEnrolledByStudentAsync but unpaged (2026-07-18) -- the default
        // "students with money to settle" view needs every match's live outstanding balance
        // before it can sort/page, since that balance lives in a different aggregate
        // (FeeInvoice) the DB query here can't join against.
        Task<IReadOnlyList<Enrollment>> SearchEnrolledByStudentAllAsync(Guid? academicYearId, string search, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentDiscount>> GetDiscountsByEnrollmentIdsAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<StudentScholarship>> GetScholarshipsByEnrollmentIdsAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<EnrollmentFeeSelection>> GetFeeSelectionsByEnrollmentIdsAsync(IReadOnlyList<Guid> enrollmentIds, CancellationToken cancellationToken = default);
    }
}
