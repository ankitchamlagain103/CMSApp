using Application.Notifications.Dtos;
using Domain.Entities;

namespace Application.Notifications
{
    public static class NotificationMapper
    {
        public static NotificationDto ToDto(Notification notification)
        {
            var notificationDto = new NotificationDto
            {
                Id = notification.Id,
                EmployeeId = notification.EmployeeId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                IsRead = notification.IsRead,
                CreatedTs = notification.CreatedTs
            };

            return notificationDto;
        }
    }
}
