using Application.Common.Models;
using Application.Dashboard.Dtos;

namespace Application.Dashboard
{
    public interface IDashboardService
    {
        Task<CommonResponse<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<EnrollmentStatsDto>> GetEnrollmentStatsAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<TeacherListWidgetDto>> GetTeacherListWidgetAsync(int take, CancellationToken cancellationToken = default);

        Task<CommonResponse<UserListWidgetDto>> GetUserListWidgetAsync(int take, CancellationToken cancellationToken = default);

        Task<CommonResponse<BarGraphDto>> GetBarGraphAsync(string metric, CancellationToken cancellationToken = default);

        Task<CommonResponse<CurrentAcademicYearDto>> GetCurrentAcademicYearAsync(CancellationToken cancellationToken = default);

        Task<CommonResponse<List<QuickMenuDto>>> GetQuickMenusAsync(int take, CancellationToken cancellationToken = default);

        // Persona-oriented composite widgets (2026-07-28) -- same one-call shape as
        // GetSummaryAsync, scoped to the Accounts (finance) and HR functions respectively.
        Task<CommonResponse<AccountsDashboardSummaryDto>> GetAccountsSummaryAsync(int take, CancellationToken cancellationToken = default);

        Task<CommonResponse<HrDashboardSummaryDto>> GetHrSummaryAsync(int take, CancellationToken cancellationToken = default);
    }
}
