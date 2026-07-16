using Application.AccessLogs;
using Application.AccessLogs.Dtos;
using Application.AccessLogs.Queries;
using Application.Common.Models;
using Application.Dashboard;
using Application.Dashboard.Dtos;
using Application.ErrorLogs;
using Application.ErrorLogs.Dtos;
using Application.ErrorLogs.Queries;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IErrorLogService _errorLogService;
        private readonly ISystemAccessLogService _systemAccessLogService;
        private readonly IDashboardService _dashboardService;

        public DashboardController(IErrorLogService errorLogService, ISystemAccessLogService systemAccessLogService, IDashboardService dashboardService)
        {
            _errorLogService = errorLogService;
            _systemAccessLogService = systemAccessLogService;
            _dashboardService = dashboardService;
        }

        [HttpGet("summary")]
        public async Task<ActionResult<CommonResponse<DashboardSummaryDto>>> GetSummary(CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetSummaryAsync(cancellationToken);
            return Ok(response);
        }

        [HttpGet("enrollment-stats")]
        public async Task<ActionResult<CommonResponse<EnrollmentStatsDto>>> GetEnrollmentStats(CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetEnrollmentStatsAsync(cancellationToken);
            return Ok(response);
        }

        [HttpGet("teachers")]
        public async Task<ActionResult<CommonResponse<TeacherListWidgetDto>>> GetTeacherListWidget([FromQuery] int take, CancellationToken cancellationToken)
        {
            var effectiveTake = take > 0 ? take : 5;
            var response = await _dashboardService.GetTeacherListWidgetAsync(effectiveTake, cancellationToken);
            return Ok(response);
        }

        [HttpGet("users")]
        public async Task<ActionResult<CommonResponse<UserListWidgetDto>>> GetUserListWidget([FromQuery] int take, CancellationToken cancellationToken)
        {
            var effectiveTake = take > 0 ? take : 5;
            var response = await _dashboardService.GetUserListWidgetAsync(effectiveTake, cancellationToken);
            return Ok(response);
        }

        [HttpGet("bar-graph")]
        public async Task<ActionResult<CommonResponse<BarGraphDto>>> GetBarGraph([FromQuery] string metric, CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetBarGraphAsync(metric, cancellationToken);
            if (response.ResponseCode == ResponseCodes.ValidationError)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }

        [HttpGet("current-academic-year")]
        public async Task<ActionResult<CommonResponse<CurrentAcademicYearDto>>> GetCurrentAcademicYear(CancellationToken cancellationToken)
        {
            var response = await _dashboardService.GetCurrentAcademicYearAsync(cancellationToken);
            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("quick-menus")]
        public async Task<ActionResult<CommonResponse<List<QuickMenuDto>>>> GetQuickMenus([FromQuery] int take, CancellationToken cancellationToken)
        {
            var effectiveTake = take > 0 ? take : 8;
            var response = await _dashboardService.GetQuickMenusAsync(effectiveTake, cancellationToken);
            if (response.ResponseCode == ResponseCodes.Unauthorized)
            {
                return Unauthorized(response);
            }

            if (response.ResponseCode == ResponseCodes.NotFound)
            {
                return NotFound(response);
            }

            return Ok(response);
        }

        [HttpGet("error-logs")]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<ErrorLogDto>>>> GetErrorLogs([FromQuery] GetErrorLogsQuery query, CancellationToken cancellationToken)
        {
            var response = await _errorLogService.GetErrorLogsAsync(query, cancellationToken);
            return Ok(response);
        }

        [HttpGet("error-logs/summary")]
        public async Task<ActionResult<CommonResponse<ErrorLogSummaryDto>>> GetErrorSummary(CancellationToken cancellationToken)
        {
            var response = await _errorLogService.GetErrorSummaryAsync(cancellationToken);
            return Ok(response);
        }

        [HttpGet("access-logs")]
        public async Task<ActionResult<CommonResponse<PaginatedResponse<SystemAccessLogDto>>>> GetAccessLogs([FromQuery] GetSystemAccessLogsQuery query, CancellationToken cancellationToken)
        {
            var response = await _systemAccessLogService.GetSystemAccessLogsAsync(query, cancellationToken);
            return Ok(response);
        }
    }
}
