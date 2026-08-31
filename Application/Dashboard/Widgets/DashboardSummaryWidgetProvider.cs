using Application.Common.Models;
using Application.Dashboard.Dtos;

namespace Application.Dashboard.Widgets
{
    // Adapts IDashboardService.GetSummaryAsync (the SuperAdmin-oriented overview widget) into the
    // generic widget envelope. Registered against Menu.Code "DASHBOARD_SUMMARY".
    public class DashboardSummaryWidgetProvider : IDashboardWidgetProvider
    {
        private readonly IDashboardService _dashboardService;

        public DashboardSummaryWidgetProvider(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public string WidgetCode => "DASHBOARD_SUMMARY";

        public async Task<DashboardWidgetDataResult> GetDataAsync(int take, CancellationToken cancellationToken = default)
        {
            var response = await _dashboardService.GetSummaryAsync(cancellationToken);
            var widgetData = new DashboardWidgetDataResult
            {
                Success = response.ResponseCode == ResponseCodes.Success,
                Message = response.ResponseMessage,
                Data = response.Data
            };

            return widgetData;
        }
    }
}
