using Application.Common.Models;
using Application.FeeInvoices.Commands;
using Application.FeeInvoices.Dtos;
using Application.FeeInvoices.Queries;

namespace Application.FeeInvoices
{
    public interface IFeeInvoiceService
    {
        Task<CommonResponse<FeeGenerationResultDto>> GenerateAsync(GenerateFeeInvoicesCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<FeeInvoiceDto>>> GetFeeInvoicesAsync(GetFeeInvoicesQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeInvoiceDto>> GetFeeInvoiceByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeInvoiceDto>> UpdateFeeInvoiceAsync(Guid id, UpdateFeeInvoiceCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeInvoiceDto>> AddLineAsync(Guid invoiceId, FeeInvoiceLineInput command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeInvoiceDto>> UpdateLineAsync(Guid invoiceId, Guid lineId, UpdateFeeInvoiceLineCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeInvoiceDto>> RemoveLineAsync(Guid invoiceId, Guid lineId, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeInvoiceDto>> SettleAnnualInFullAsync(Guid invoiceId, Guid lineId, CancellationToken cancellationToken = default);

        Task<CommonResponse<FinalizeResultDto>> FinalizeAsync(FinalizeFeeInvoicesCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeInvoiceDto>> UnfinalizeAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeInvoiceDto>> CancelAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeStatementDto>> GetStatementAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeAccountStatementDto>> GetAccountStatementAsync(Guid enrollmentId, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<FeeStudentSearchResultDto>>> SearchStudentsAsync(SearchFeeStudentsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeAdjustmentDto>> CreateAdjustmentAsync(CreateFeeAdjustmentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<BulkFeeAdjustmentResultDto>> CreateBulkAdjustmentAsync(CreateBulkFeeAdjustmentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<List<FeeAdjustmentDto>>> GetAdjustmentsAsync(GetFeeAdjustmentsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeAdjustmentDto>> UpdateAdjustmentAsync(Guid adjustmentId, UpdateFeeAdjustmentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> CancelAdjustmentAsync(Guid adjustmentId, CancellationToken cancellationToken = default);
    }
}
