using Domain.Enums;

namespace Application.Dashboard.Dtos
{
    public class EmploymentStatusCountDto
    {
        public EmploymentStatus Status { get; set; }
        public int Count { get; set; }
    }
}
