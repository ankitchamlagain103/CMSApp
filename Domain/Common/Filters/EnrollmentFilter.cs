using Domain.Enums;

namespace Domain.Common.Filters
{
    // Repository-side filter for GetEnrollmentsQuery -- same rationale as StudentFilter.
    public class EnrollmentFilter
    {
        public Guid? StudentId { get; set; }
        public Guid? AcademicClassId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public Guid? AcademicYearId { get; set; }
        public EnrollmentStatus? Status { get; set; }
        public EnrollmentDateField DateField { get; set; } = EnrollmentDateField.EnrollmentDate;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
