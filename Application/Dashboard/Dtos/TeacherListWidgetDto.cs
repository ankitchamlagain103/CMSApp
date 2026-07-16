namespace Application.Dashboard.Dtos
{
    public class TeacherListWidgetDto
    {
        public int TotalTeachers { get; set; }
        public int ActiveTeachers { get; set; }
        public List<DashboardTeacherSummaryDto> RecentTeachers { get; set; } = new List<DashboardTeacherSummaryDto>();
    }
}
