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

        // Navbar "Ctrl+K"-style global search across Students and Employees (2026-08-05) -- by
        // name, AdmissionNo/EmployeeCode, or the record's own id (a pasted Guid matches exactly).
        // `limit` caps each group independently, not the combined total.
        Task<CommonResponse<GlobalSearchResultDto>> GlobalSearchAsync(string query, int limit, CancellationToken cancellationToken = default);
    }
}
