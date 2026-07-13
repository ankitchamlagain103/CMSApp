using Application.Common.Models;
using Application.ErrorLogs.Commands;
using Application.ErrorLogs.Dtos;
using Application.ErrorLogs.Queries;

namespace Application.ErrorLogs
{
    public interface IErrorLogService
    {
        Task LogErrorAsync(CreateErrorLogCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<ErrorLogDto>>> GetErrorLogsAsync(GetErrorLogsQuery query, CancellationToken cancellationToken = default);

        Task<CommonResponse<ErrorLogSummaryDto>> GetErrorSummaryAsync(CancellationToken cancellationToken = default);
    }
}
