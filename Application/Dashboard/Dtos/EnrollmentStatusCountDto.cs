using Domain.Enums;

namespace Application.Dashboard.Dtos
{
    public class EnrollmentStatusCountDto
    {
        public EnrollmentStatus Status { get; set; }
        public int Count { get; set; }
    }
}
