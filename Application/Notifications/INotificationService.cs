using Application.Common.Models;
using Application.Notifications.Dtos;
using Domain.Enums;

namespace Application.Notifications
{
    public interface INotificationService
    {
        // Internal-use entry point other feature services call directly (constructor-injected,
        // same "one Application service injects another" pattern as SalaryCalculatorService ->
        // IEmployeeService) to raise a notification as a side effect of their own action (e.g.
        // LeaveRequestService after a manager/HR decision). Does its own SaveChangesAsync -- callers
        // don't need to coordinate a shared unit-of-work transaction for this.
        Task CreateNotificationAsync(Guid employeeId, string title, string message, NotificationType type, CancellationToken cancellationToken = default);

        Task<CommonResponse<PaginatedResponse<NotificationDto>>> GetNotificationsAsync(Guid employeeId, bool? isRead, int page, int pageSize, CancellationToken cancellationToken = default);

        Task<CommonResponse<int>> GetUnreadCountAsync(Guid employeeId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> MarkReadAsync(Guid employeeId, Guid notificationId, CancellationToken cancellationToken = default);

        Task<CommonResponse<bool>> MarkAllReadAsync(Guid employeeId, CancellationToken cancellationToken = default);
    }
}
