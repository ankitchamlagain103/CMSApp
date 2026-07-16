using Domain.Enums;

namespace Application.Dashboard.Dtos
{
    public class DashboardUserSummaryDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public UserType UserType { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
    }
}
