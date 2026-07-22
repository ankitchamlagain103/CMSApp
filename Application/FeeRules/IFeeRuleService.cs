using Application.Common.Models;
using Application.FeeRules.Commands;
using Application.FeeRules.Dtos;
using Application.FeeRules.Queries;

namespace Application.FeeRules
{
    public interface IFeeRuleService
    {
        Task<CommonResponse<FeeRuleDto>> CreateFeeRuleAsync(CreateFeeRuleCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeRuleDto>> GetFeeRuleByIdAsync(Guid id, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<FeeRuleDto>>> GetFeeRulesAsync(GetFeeRulesQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<FeeRuleDto>> UpdateFeeRuleAsync(Guid id, UpdateFeeRuleCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> DeleteFeeRuleAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
