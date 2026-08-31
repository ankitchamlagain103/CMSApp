using Application.Common.Models;
using Application.Dashboard.Dtos;
using Application.Students;

namespace Application.Dashboard.Widgets
{
    // Adapts IStudentService.GetMyDashboardAsync into the generic widget envelope, mirroring
    // MyDashboardWidgetProvider for the Staff Portal. Registered against Menu.Code
    // "STUDENT_MY_DASHBOARD". A caller whose account isn't linked to a Student record gets
    // Success = false with the same "not linked to a student record" message
    // GetMyDashboardAsync already returns.
    public class StudentDashboardWidgetProvider : IDashboardWidgetProvider
    {
        private readonly IStudentService _studentService;

        public StudentDashboardWidgetProvider(IStudentService studentService)
        {
            _studentService = studentService;
        }

        public string WidgetCode => "STUDENT_MY_DASHBOARD";

        public async Task<DashboardWidgetDataResult> GetDataAsync(int take, CancellationToken cancellationToken = default)
        {
            var response = await _studentService.GetMyDashboardAsync(cancellationToken);
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
