using Application.Common.Models;
using Application.Dashboard.Dtos;

namespace Application.Dashboard
{
    public interface IDashboardWidgetRegistryService
    {
        // Resolves the caller's own granted-and-audience-filtered menu tree (IRoleService.GetUserRolesAsync),
        // keeps only the nodes flagged Menu.IsDashboardWidget, and invokes each one's registered
        // IDashboardWidgetProvider by matching Menu.Code -- so a role sees exactly the widgets its
        // granted menus flag as widgets, with no per-widget code to write or deploy for a new role.
        Task<CommonResponse<List<DashboardWidgetResultDto>>> GetWidgetsAsync(int take, CancellationToken cancellationToken = default);
    }
}
