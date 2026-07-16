namespace Application.Dashboard.Dtos
{
    public class UserListWidgetDto
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public List<DashboardUserSummaryDto> RecentUsers { get; set; } = new List<DashboardUserSummaryDto>();
    }
}
