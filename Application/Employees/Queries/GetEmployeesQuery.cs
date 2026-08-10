using Domain.Enums;

namespace Application.Employees.Queries
{
    public class GetEmployeesQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string Search { get; set; }
        public string Phone { get; set; }
        public string EmployeeCategoryCode { get; set; }
        public string JobPositionCode { get; set; }

        // Ported from the removed GetTeachersQuery (2026-08-06) -- filters on this employee's own
        // Qualifications, no more Teacher.Employee indirection.
        public string QualificationCode { get; set; }

        public EmploymentStatus? EmploymentStatus { get; set; }
        public Gender? Gender { get; set; }
        public EmployeeDateField DateField { get; set; } = EmployeeDateField.CreatedDate;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
