using Domain.Enums;

namespace Application.Enrollments.Queries
{
    // AcademicClassId filters a whole class (all its sections); ClassSectionId narrows to one
    // section. They can be combined, though the section filter alone is usually enough.
    // DateField picks which column FromDate/ToDate applies to.
    public class GetEnrollmentsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
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
