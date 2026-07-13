using Application.Common.Models;
using Application.Dashboard.Dtos;

namespace Application.Dashboard
{
    public interface IDashboardService
    {
        Task<CommonResponse<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);
    }
}
