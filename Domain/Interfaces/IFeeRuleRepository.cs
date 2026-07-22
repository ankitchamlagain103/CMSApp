using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;
using Domain.Enums;

namespace Domain.Interfaces
{
    public interface IFeeRuleRepository : IRepository<FeeRule, Guid>
    {
        Task<PagedResult<FeeRule>> GetPagedByFilterAsync(FeeRuleFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

        // The rule engine's input: active rules for one trigger stage whose validity window
        // contains asOfDate, ordered by Priority. Scope filtering (class/category) happens in
        // the engine against the concrete payment/generation context.
        Task<IReadOnlyList<FeeRule>> GetActiveRulesAsync(FeeRuleTrigger triggerStage, DateTime asOfDate, CancellationToken cancellationToken = default);
    }
}
