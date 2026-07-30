using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Notifications.Dtos;
using Domain.Entities;
using Domain.Enums;

namespace Application.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task CreateNotificationAsync(Guid employeeId, string title, string message, NotificationType type, CancellationToken cancellationToken = default)
        {
            var notification = new Notification
            {
                EmployeeId = employeeId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false
            };

            await _unitOfWork.Notifications.AddAsync(notification, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<CommonResponse<PaginatedResponse<NotificationDto>>> GetNotificationsAsync(Guid employeeId, bool? isRead, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var pagedNotifications = await _unitOfWork.Notifications.GetPagedByEmployeeIdAsync(employeeId, isRead, page, pageSize, cancellationToken);

            var notificationDtos = new List<NotificationDto>();
            foreach (var notification in pagedNotifications.Items)
            {
                var notificationDto = NotificationMapper.ToDto(notification);
                notificationDtos.Add(notificationDto);
            }

            var paginatedResponse = new PaginatedResponse<NotificationDto>
            {
                Items = notificationDtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = pagedNotifications.TotalCount
            };

            var successResponse = CommonResponse<PaginatedResponse<NotificationDto>>.Success(paginatedResponse);
            return successResponse;
        }

        public async Task<CommonResponse<int>> GetUnreadCountAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            var unreadCount = await _unitOfWork.Notifications.GetUnreadCountAsync(employeeId, cancellationToken);
            var successResponse = CommonResponse<int>.Success(unreadCount);
            return successResponse;
        }

        public async Task<CommonResponse<bool>> MarkReadAsync(Guid employeeId, Guid notificationId, CancellationToken cancellationToken = default)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId, cancellationToken);
            if (notification == null || notification.EmployeeId != employeeId)
            {
                var notFoundResponse = CommonResponse<bool>.Fail(ResponseCodes.NotFound, "Notification was not found for this employee.");
                return notFoundResponse;
            }

            notification.IsRead = true;
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "Notification marked as read.");
            return successResponse;
        }

        public async Task<CommonResponse<bool>> MarkAllReadAsync(Guid employeeId, CancellationToken cancellationToken = default)
        {
            await _unitOfWork.Notifications.MarkAllReadAsync(employeeId, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var successResponse = CommonResponse<bool>.Success(true, "All notifications marked as read.");
            return successResponse;
        }
    }
}
