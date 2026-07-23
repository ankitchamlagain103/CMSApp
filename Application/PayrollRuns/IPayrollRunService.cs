using Application.Common.Models;
using Application.PayrollRuns.Commands;
using Application.PayrollRuns.Dtos;
using Application.PayrollRuns.Queries;

namespace Application.PayrollRuns
{
    public interface IPayrollRunService
    {
        Task<CommonResponse<PayrollGenerationResultDto>> CreateRunAsync(CreatePayrollRunCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<PayrollGenerationResultDto>> RefreshRunAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<PayrollRunDto>>> GetRunsAsync(GetPayrollRunsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<PayrollRunDto>> GetRunByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PayrollRunDto>> ApproveRunAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PayrollRunDto>> MarkPaidAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PayrollRunDto>> CancelRunAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalarySlipDto>> GetSlipByIdAsync(Guid runId, Guid slipId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalarySlipDto>> CancelSlipAsync(Guid runId, Guid slipId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalarySlipDto>> ApproveSlipAsync(Guid runId, Guid slipId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalarySlipDto>> RegenerateSlipAsync(Guid runId, Guid slipId, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalarySlipDto>> AddSlipLineAsync(Guid runId, Guid slipId, SalarySlipLineInput command, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalarySlipDto>> UpdateSlipLineAsync(Guid runId, Guid slipId, Guid lineId, UpdateSalarySlipLineCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<SalarySlipDto>> RemoveSlipLineAsync(Guid runId, Guid slipId, Guid lineId, CancellationToken cancellationToken = default);
    }
}
