using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Dashboard;
using Application.Dashboard.Dtos;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Identity.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IUnitOfWork _unitOfWork;

        public DashboardService(ApplicationDbContext dbContext, IUnitOfWork unitOfWork)
        {
            _dbContext = dbContext;
            _unitOfWork = unitOfWork;
        }

        public async Task<CommonResponse<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default)
        {
            // Soft-deleted users are excluded automatically by the global !IsDeleted query filter.
            var totalUserCount = await _dbContext.Users.CountAsync(cancellationToken);
            var activeUserCount = await _dbContext.Users.CountAsync(user => user.IsActive, cancellationToken);
            var distinctErrorCount = await _unitOfWork.ErrorLogs.GetDistinctErrorCountAsync(cancellationToken);
            var totalErrorCount = await _unitOfWork.ErrorLogs.GetTotalOccurrenceCountAsync(cancellationToken);

            var dashboardSummaryDto = new DashboardSummaryDto
            {
                TotalUserCount = totalUserCount,
                ActiveUserCount = activeUserCount,
                DistinctErrorCount = distinctErrorCount,
                TotalErrorCount = totalErrorCount
            };

            var successResponse = CommonResponse<DashboardSummaryDto>.Success(dashboardSummaryDto);
            return successResponse;
        }
    }
}
