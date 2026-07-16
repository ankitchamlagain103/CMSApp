using Domain.Enums;

namespace Domain.Common.Filters
{
    // Repository-side filter for GetStudentsQuery. A plain Domain-owned object rather than a
    // long positional-parameter list -- Domain can't reference Application's query class, and
    // this many optional fields would be unwieldy as bare method parameters.
    public class StudentFilter
    {
        public string Search { get; set; }
        public string Phone { get; set; }
        public string GradeCode { get; set; }
        public Guid? AcademicYearId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public RecordStatus? Status { get; set; }
        public Gender? Gender { get; set; }
        public StudentDateField DateField { get; set; } = StudentDateField.CreatedDate;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
