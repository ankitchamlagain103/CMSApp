namespace Application.Dashboard.Dtos
{
    public class EnrollmentStatsDto
    {
        public int TotalStudents { get; set; }
        public int TotalActiveEnrollments { get; set; }
        public List<EnrollmentStatusCountDto> EnrollmentsByStatus { get; set; } = new List<EnrollmentStatusCountDto>();
        public List<GradeEnrollmentCountDto> EnrollmentsByGrade { get; set; } = new List<GradeEnrollmentCountDto>();
    }
}
