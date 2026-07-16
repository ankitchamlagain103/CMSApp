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
    }
}
