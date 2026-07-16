using Domain.Enums;

namespace Application.Teachers.Queries
{
    // DateField picks which column FromDate/ToDate applies to.
    public class GetTeachersQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Search { get; set; }
        public string Phone { get; set; }
        public string QualificationCode { get; set; }
        public EmploymentStatus? Status { get; set; }
        public TeacherDateField DateField { get; set; } = TeacherDateField.CreatedDate;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
