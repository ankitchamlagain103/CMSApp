using Application.Common.Models;
using Application.FeePayments.Commands;
using Application.FeePayments.Dtos;
using Application.FeePayments.Queries;

namespace Application.FeePayments
{
    public interface IFeePaymentService
    {
        Task<CommonResponse<FeePaymentPreviewDto>> PreviewAsync(CreateFeePaymentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeePaymentDto>> CreateAsync(CreateFeePaymentCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<FeePaymentDto>>> GetPaymentsAsync(GetFeePaymentsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeePaymentDto>> GetPaymentByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeePaymentDto>> VoidAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<DocumentPreviewDto>> GetReceiptAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeAdvanceQuoteDto>> GetAdvanceQuoteAsync(GetFeeAdvanceQuoteQuery query, CancellationToken cancellationToken = default);
    }
}
