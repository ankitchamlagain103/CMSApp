using Application.Common.Models;
using Application.Dashboard.Dtos;

namespace Application.Dashboard.Widgets
{
    // Adapts IDashboardService.GetHrSummaryAsync into the generic widget envelope. Registered
    // against Menu.Code "DASHBOARD_HR_SUMMARY".
    public class HrSummaryWidgetProvider : IDashboardWidgetProvider
    {
        private readonly IDashboardService _dashboardService;

        public HrSummaryWidgetProvider(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        public string WidgetCode => "DASHBOARD_HR_SUMMARY";

        public async Task<DashboardWidgetDataResult> GetDataAsync(int take, CancellationToken cancellationToken = default)
        {
            var effectiveTake = take > 0 ? take : 5;
            var response = await _dashboardService.GetHrSummaryAsync(effectiveTake, cancellationToken);
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
