namespace Application.Dashboard.Dtos
{
    // One row of GET /api/dashboard/widgets -- WidgetCode/DisplayName come from the caller's own
    // granted Menu row (Menu.Code/Menu.DisplayName where Menu.IsDashboardWidget is true); Data is
    // whichever concrete DTO the matching IDashboardWidgetProvider returns (AccountsDashboardSummaryDto,
    // HrDashboardSummaryDto, EmployeeDashboardDto, ...). Data is deliberately typed object -- this
    // is the one place in the codebase where the response payload's shape is genuinely
    // per-widget/per-caller, which is the entire point of a generic dashboard; every provider
    // still returns a real, already-documented DTO underneath, this just doesn't pin one type on
    // the wire the way every other CommonResponse<T> does.
    public class DashboardWidgetResultDto
    {
        public string WidgetCode { get; set; }
        public string DisplayName { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public object Data { get; set; }
    }
}
