using Application.AccessLogs.Dtos;
using Domain.Entities;

namespace Application.AccessLogs
{
    public static class SystemAccessLogMapper
    {
        public static SystemAccessLogDto ToDto(SystemAccessLog systemAccessLog)
        {
            var systemAccessLogDto = new SystemAccessLogDto
            {
                Id = systemAccessLog.Id,
                UserId = systemAccessLog.UserId,
                UserName = systemAccessLog.UserName,
                Controller = systemAccessLog.Controller,
                Action = systemAccessLog.Action,
                HttpMethod = systemAccessLog.HttpMethod,
                Url = systemAccessLog.Url,
                IpAddress = systemAccessLog.IpAddress,
                AccessedTs = systemAccessLog.CreatedTs
            };

            return systemAccessLogDto;
        }
    }
}
