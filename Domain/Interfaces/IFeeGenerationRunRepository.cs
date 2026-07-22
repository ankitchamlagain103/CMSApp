using Domain.Common;
using Domain.Common.Filters;
using Domain.Entities;

namespace Domain.Interfaces
{
    public interface IFeeGenerationRunRepository : IRepository<FeeGenerationRun, Guid>
    {
        Task<PagedResult<FeeGenerationRun>> GetPagedByFilterAsync(FeeGenerationRunFilter filter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        Task<FeeGenerationRun> GetByIdWithYearAsync(Guid id, CancellationToken cancellationToken = default);

        // The live (non-deleted) run for a period, headers only; null when the period hasn't
        // been generated yet -- the find-or-create target for GenerateAsync.
        Task<FeeGenerationRun> GetByPeriodAsync(Guid academicYearId, int billingYear, int billingMonth, CancellationToken cancellationToken = default);
    }
}
