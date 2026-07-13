using System.Security.Cryptography;
using System.Text;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.ErrorLogs.Commands;
using Application.ErrorLogs.Dtos;
using Application.ErrorLogs.Queries;
using Domain.Entities;

namespace Application.ErrorLogs
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ErrorLogService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task LogErrorAsync(CreateErrorLogCommand command, CancellationToken cancellationToken = default)
        {
            var fingerprintHash = ComputeFingerprintHash(command.ExceptionType, command.Message, command.StackTrace);
            var occurredTs = DateTimeOffset.UtcNow;

            var existingErrorLog = await _unitOfWork.ErrorLogs.GetByFingerprintAsync(fingerprintHash, cancellationToken);
            if (existingErrorLog != null)
            {
                existingErrorLog.ErrorCount = existingErrorLog.ErrorCount + 1;
                existingErrorLog.LastOccurredTs = occurredTs;
                existingErrorLog.Path = command.Path;

                _unitOfWork.ErrorLogs.Update(existingErrorLog);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return;
            }

            var errorLog = new ErrorLog
            {
                FingerprintHash = fingerprintHash,
                ExceptionType = command.ExceptionType,
                Message = command.Message,
                StackTrace = command.StackTrace,
                Path = command.Path,
                ErrorCount = 1,
                LastOccurredTs = occurredTs
            };

            await _unitOfWork.ErrorLogs.AddAsync(errorLog, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<CommonResponse<PaginatedResponse<ErrorLogDto>>> GetErrorLogsAsync(GetErrorLogsQuery query, CancellationToken cancellationToken = default)
        {
            var pagedErrorLogs = await _unitOfWork.ErrorLogs.GetPagedByLastOccurredDescAsync(query.Page, query.PageSize, cancellationToken);

            var errorLogDtos = new List<ErrorLogDto>();
            foreach (var errorLog in pagedErrorLogs.Items)
            {
                var errorLogDto = ErrorLogMapper.ToDto(errorLog);
                errorLogDtos.Add(errorLogDto);
            }

            var paginatedResponse = new PaginatedResponse<ErrorLogDto>
            {
                Items = errorLogDtos,
                Page = query.Page,
                PageSize = query.PageSize,
                TotalCount = pagedErrorLogs.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<ErrorLogDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<ErrorLogSummaryDto>> GetErrorSummaryAsync(CancellationToken cancellationToken = default)
        {
            var distinctErrorCount = await _unitOfWork.ErrorLogs.GetDistinctErrorCountAsync(cancellationToken);
            var totalErrorCount = await _unitOfWork.ErrorLogs.GetTotalOccurrenceCountAsync(cancellationToken);

            var errorLogSummaryDto = new ErrorLogSummaryDto
            {
                DistinctErrorCount = distinctErrorCount,
                TotalErrorCount = totalErrorCount
            };

            var successResponse = CommonResponse<ErrorLogSummaryDto>.Success(errorLogSummaryDto);
            return successResponse;
        }

        // Two occurrences count as "the same error" when exception type, message, and stack trace
        // all match -- hashed so the lookup is an indexed 64-char comparison instead of comparing
        // unbounded text columns.
        private static string ComputeFingerprintHash(string exceptionType, string message, string stackTrace)
        {
            var fingerprintSource = (exceptionType ?? string.Empty) + "|" + (message ?? string.Empty) + "|" + (stackTrace ?? string.Empty);
            var fingerprintBytes = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource));
            var fingerprintHash = Convert.ToHexString(fingerprintBytes);
            return fingerprintHash;
        }
    }
}
