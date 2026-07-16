using Domain.Enums;

namespace Application.Students.Queries
{
    // Grade/AcademicYear/ClassSection filter against the student's Enrolled-status enrollment
    // (a "currently in Grade X of year Y" snapshot, not full enrollment history -- use the
    // Enrollments list endpoint for history-wide queries). DateField picks which column
    // FromDate/ToDate applies to.
    public class GetStudentsQuery
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
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
