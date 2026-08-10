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

        // Server-computed only (2026-08-07) -- never bound from a caller-supplied query. Set by
        // StudentService.GetMyStudentsAsync to the caller's own TeacherAssignment.ClassSectionId
        // values, so a teacher's "my students" list can never include a section they aren't
        // actually assigned to, regardless of what ClassSectionId/GradeCode the caller also sends.
        public List<Guid> ClassSectionIds { get; set; }
        public RecordStatus? Status { get; set; }
        public Gender? Gender { get; set; }
        public StudentDateField DateField { get; set; } = StudentDateField.CreatedDate;
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
