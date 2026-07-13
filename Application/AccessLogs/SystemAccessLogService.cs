using Application.AccessLogs.Commands;
using Application.AccessLogs.Dtos;
using Application.AccessLogs.Queries;
using Application.Common.Interfaces;
using Application.Common.Models;
using Domain.Entities;

namespace Application.AccessLogs
{
    public class SystemAccessLogService : ISystemAccessLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SystemAccessLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogAccessAsync(CreateSystemAccessLogCommand command, CancellationToken cancellationToken = default)
        {
            var systemAccessLog = new SystemAccessLog
            {
                UserId = command.UserId,
                UserName = command.UserName,
                Controller = command.Controller,
                Action = command.Action,
                HttpMethod = command.HttpMethod,
                Url = command.Url,
                IpAddress = command.IpAddress
            };

            await _unitOfWork.SystemAccessLogs.AddAsync(systemAccessLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<CommonResponse<PaginatedResponse<SystemAccessLogDto>>> GetSystemAccessLogsAsync(GetSystemAccessLogsQuery query, CancellationToken cancellationToken = default)
        {
            var pagedAccessLogs = await _unitOfWork.SystemAccessLogs.GetPagedByCreatedDescAsync(query.Page, query.PageSize, query.UserId, cancellationToken);

            var systemAccessLogDtos = new List<SystemAccessLogDto>();
            foreach (var systemAccessLog in pagedAccessLogs.Items)
            {
                var systemAccessLogDto = SystemAccessLogMapper.ToDto(systemAccessLog);
                systemAccessLogDtos.Add(systemAccessLogDto);
            }

            var paginatedResponse = new PaginatedResponse<SystemAccessLogDto>
            {
                Items = systemAccessLogDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedAccessLogs.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<SystemAccessLogDto>>.Success(paginatedResponse);
            return successResponse;
        }
    }
}
