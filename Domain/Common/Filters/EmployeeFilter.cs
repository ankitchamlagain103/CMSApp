using Domain.Enums;

namespace Domain.Common.Filters
{
    public class EmployeeFilter
    {
        public string Search { get; set; }
        public string Phone { get; set; }
        public string EmployeeCategoryCode { get; set; }
        public string JobPositionCode { get; set; }
        public EmploymentStatus? EmploymentStatus { get; set; }
        public Gender? Gender { get; set; }
        public EmployeeDateField DateField { get; set; } = EmployeeDateField.CreatedDate;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
