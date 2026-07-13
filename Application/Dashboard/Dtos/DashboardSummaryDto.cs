namespace Application.Dashboard.Dtos
{
    public class DashboardSummaryDto
    {
        public int TotalUserCount { get; set; }
        public int ActiveUserCount { get; set; }
        public int DistinctErrorCount { get; set; }
        public long TotalErrorCount { get; set; }
    }
}
