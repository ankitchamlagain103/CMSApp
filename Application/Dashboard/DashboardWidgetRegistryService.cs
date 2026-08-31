using Application.Common.Models;
using Application.Dashboard.Dtos;
using Application.Roles;
using Application.Roles.Dtos;

namespace Application.Dashboard
{
    public class DashboardWidgetRegistryService : IDashboardWidgetRegistryService
    {
        private readonly IRoleService _roleService;
        private readonly IEnumerable<IDashboardWidgetProvider> _widgetProviders;

        public DashboardWidgetRegistryService(IRoleService roleService, IEnumerable<IDashboardWidgetProvider> widgetProviders)
        {
            _roleService = roleService;
            _widgetProviders = widgetProviders;
        }

        public async Task<CommonResponse<List<DashboardWidgetResultDto>>> GetWidgetsAsync(int take, CancellationToken cancellationToken = default)
        {
            var menuTreeResponse = await _roleService.GetUserRolesAsync(cancellationToken);
            if (menuTreeResponse.ResponseCode != ResponseCodes.Success)
            {
                var failureResponse = CommonResponse<List<DashboardWidgetResultDto>>.Fail(menuTreeResponse.ResponseCode, menuTreeResponse.ResponseMessage);
                return failureResponse;
            }

            var widgetMenus = new List<MenuClaimDto>();
            CollectDashboardWidgetMenus(menuTreeResponse.Data, widgetMenus);

            var providersByCode = new Dictionary<string, IDashboardWidgetProvider>();
            foreach (var provider in _widgetProviders)
            {
                providersByCode[provider.WidgetCode] = provider;
            }

            var widgetResults = new List<DashboardWidgetResultDto>();
            foreach (var widgetMenu in widgetMenus)
            {
                if (!providersByCode.TryGetValue(widgetMenu.Code, out var provider))
                {
                    // Flagged as a widget in the catalog but no provider registered yet -- skip
                    // this one widget rather than failing the whole call.
                    continue;
                }

                var widgetData = await provider.GetDataAsync(take, cancellationToken);
                var widgetResult = new DashboardWidgetResultDto
                {
                    WidgetCode = widgetMenu.Code,
                    DisplayName = widgetMenu.DisplayName,
                    Success = widgetData.Success,
                    Message = widgetData.Message,
                    Data = widgetData.Data
                };
                widgetResults.Add(widgetResult);
            }

            var successResponse = CommonResponse<List<DashboardWidgetResultDto>>.Success(widgetResults);
            return successResponse;
        }

        // The menu tree from GetUserRolesAsync is roots-with-nested-children -- walk every level
        // to find every IsDashboardWidget node, since a widget can be a hidden PERMISSION leaf
        // (DASHBOARD_SUMMARY) or a visible SUB_MENU (MY_DASHBOARD).
        private static void CollectDashboardWidgetMenus(List<MenuClaimDto> menus, List<MenuClaimDto> widgetMenus)
        {
            foreach (var menu in menus)
            {
                if (menu.IsDashboardWidget)
                {
                    widgetMenus.Add(menu);
                }

                if (menu.Children.Count > 0)
                {
                    CollectDashboardWidgetMenus(menu.Children, widgetMenus);
                }
            }
        }
    }
}
