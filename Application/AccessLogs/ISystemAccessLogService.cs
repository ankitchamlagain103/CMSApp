using Application.AccessLogs.Commands;
using Application.AccessLogs.Dtos;
using Application.AccessLogs.Queries;
using Application.Common.Models;

namespace Application.AccessLogs
{
    public interface ISystemAccessLogService
    {
        Task LogAccessAsync(CreateSystemAccessLogCommand command, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<SystemAccessLogDto>>> GetSystemAccessLogsAsync(GetSystemAccessLogsQuery query, CancellationToken cancellationToken = default);
    }
}
