using Application.Common.Models;
using Application.Dashboard.Dtos;
using Application.Employees;

namespace Application.Dashboard.Widgets
{
    // Adapts IEmployeeService.GetMyDashboardAsync (leave/routine/upcoming-events, see
    // employee_self_service_implementation_guide.md) into the generic widget envelope.
    // Registered against Menu.Code "MY_DASHBOARD". A caller whose account isn't linked to an
    // Employee record (e.g. a Student, or an unlinked Admin) gets Success = false with the same
    // "not linked to an employee record" message GetMyDashboardAsync already returns -- the
    // registry still includes the row rather than silently dropping it, since the caller's role
    // is what decided MY_DASHBOARD should show up at all.
    public class MyDashboardWidgetProvider : IDashboardWidgetProvider
    {
        private readonly IEmployeeService _employeeService;

        public MyDashboardWidgetProvider(IEmployeeService employeeService)
        {
            _employeeService = employeeService;
        }

        public string WidgetCode => "MY_DASHBOARD";

        public async Task<DashboardWidgetDataResult> GetDataAsync(int take, CancellationToken cancellationToken = default)
        {
            var response = await _employeeService.GetMyDashboardAsync(cancellationToken);
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
