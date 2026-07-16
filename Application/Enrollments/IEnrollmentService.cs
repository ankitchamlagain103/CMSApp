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

        Task<CommonResponse<StudentDiscountDto>> AddDiscountAsync(Guid enrollmentId, AddDiscountCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveDiscountAsync(Guid enrollmentId, Guid discountId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<StudentDiscountDto>>> GetDiscountsAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<AwardSummaryDto>>> GetDiscountSummaryAsync(Guid? academicYearId, string discountTypeCode, CancellationToken cancellationToken = default);

        Task<CommonResponse<StudentScholarshipDto>> AddScholarshipAsync(Guid enrollmentId, AddScholarshipCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveScholarshipAsync(Guid enrollmentId, Guid scholarshipId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<StudentScholarshipDto>>> GetScholarshipsAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<AwardSummaryDto>>> GetScholarshipSummaryAsync(Guid? academicYearId, string scholarshipTypeCode, CancellationToken cancellationToken = default);

        Task<CommonResponse<EnrollmentFeeSelectionDto>> AddFeeSelectionAsync(Guid enrollmentId, Guid feeStructureItemId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> RemoveFeeSelectionAsync(Guid enrollmentId, Guid feeSelectionId, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<EnrollmentFeeSelectionDto>>> GetFeeSelectionsAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<EnrollmentFeeStructureDto>> GetFeeStructureAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<DocumentPreviewDto>> GetFeeReceiptPreviewAsync(Guid enrollmentId, CancellationToken cancellationToken = default);
    }
}
