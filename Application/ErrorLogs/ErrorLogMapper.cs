using Application.ErrorLogs.Dtos;
using Domain.Entities;

namespace Application.ErrorLogs
{
    public static class ErrorLogMapper
    {
        public static ErrorLogDto ToDto(ErrorLog errorLog)
        {
            var errorLogDto = new ErrorLogDto
            {
                Id = errorLog.Id,
                ExceptionType = errorLog.ExceptionType,
                Message = errorLog.Message,
                Path = errorLog.Path,
                ErrorCount = errorLog.ErrorCount,
                FirstOccurredTs = errorLog.CreatedTs,
                LastOccurredTs = errorLog.LastOccurredTs
            };

            return errorLogDto;
        }
    }
}
