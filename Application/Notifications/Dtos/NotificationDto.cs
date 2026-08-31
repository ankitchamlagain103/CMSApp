using Domain.Enums;

namespace Application.Notifications.Dtos
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
    }
}
