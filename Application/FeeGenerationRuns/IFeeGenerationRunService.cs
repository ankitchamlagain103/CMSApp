using Application.Common.Models;
using Application.FeeGenerationRuns.Dtos;
using Application.FeeGenerationRuns.Queries;
using Application.FeeInvoices.Dtos;

namespace Application.FeeGenerationRuns
{
    public interface IFeeGenerationRunService
    {
        Task<CommonResponse<PaginatedResponse<FeeGenerationRunDto>>> GetFeeGenerationRunsAsync(GetFeeGenerationRunsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeGenerationRunDetailDto>> GetFeeGenerationRunByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeGenerationClassGroupDto>> GetFeeGenerationRunClassDetailAsync(Guid id, Guid academicClassId, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeGenerationResultDto>> RefreshRunAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeGenerationResultDto>> RefreshRunClassAsync(Guid id, Guid academicClassId, CancellationToken cancellationToken = default);
    }
}
