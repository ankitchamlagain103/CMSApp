using Domain.Enums;

namespace Domain.Common.Filters
{
    // Repository-side filter for GetTeachersQuery -- same rationale as StudentFilter.
    public class TeacherFilter
    {
        public string Search { get; set; }
        public string Phone { get; set; }
        public string QualificationCode { get; set; }
        public EmploymentStatus? Status { get; set; }
        public TeacherDateField DateField { get; set; } = TeacherDateField.CreatedDate;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
