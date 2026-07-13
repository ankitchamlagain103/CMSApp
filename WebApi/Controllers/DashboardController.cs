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
