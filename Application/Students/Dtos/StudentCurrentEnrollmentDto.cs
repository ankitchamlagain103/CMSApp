namespace Application.Students.Dtos
{
    // The student profile header's lightweight "current class" indicator -- just enough to
    // render "Studying in Grade X - Section Y since AY2083" and to give the UI the
    // EnrollmentId/AcademicClassId/ClassSectionId it needs to call other tab-scoped endpoints
    // (fee summary, statement, timetable). Deliberately does NOT carry the subject list anymore
    // (2026-08-05) -- that required a per-subject teacher lookup that made this "always loaded"
    // block expensive for something most page loads never look at. See
    // GET /api/students/{id}/timetable for the full subject+teacher+period breakdown, loaded only
    // when the "Current Class" tab is actually opened.
    public class StudentCurrentEnrollmentDto
    {
        public Guid EnrollmentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public string AcademicYearCode { get; set; }
        public string AcademicYearName { get; set; }
        public Guid AcademicClassId { get; set; }
        public string GradeCode { get; set; }
        public string GradeLabel { get; set; }
        public Guid ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public string SectionLabel { get; set; }
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
    }
}
