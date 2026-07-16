namespace Application.Dashboard
{
    // Allowed values for the `metric` query parameter on GET /api/dashboard/bar-graph.
    // Add a new constant here, plus a matching Build*BarGraphAsync branch in DashboardService,
    // whenever a new chart is needed -- keeps the metric name validated in one place.
    public static class DashboardBarGraphMetrics
    {
        public const string EnrollmentsByGrade = "EnrollmentsByGrade";
        public const string EnrollmentsByMonth = "EnrollmentsByMonth";
        public const string StudentsByStatus = "StudentsByStatus";
        public const string TeachersByStatus = "TeachersByStatus";

        public static readonly string[] All =
        {
            EnrollmentsByGrade,
            EnrollmentsByMonth,
            StudentsByStatus,
            TeachersByStatus
        };
    }
}
