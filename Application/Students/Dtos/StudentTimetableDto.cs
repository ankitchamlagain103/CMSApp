namespace Application.Students.Dtos
{
    // GET /api/students/{id}/timetable's payload -- the student's current class/section
    // routine: every subject they study, who teaches it, and (when configured) which
    // TimePeriod slot it's in. Backs the "Current Class" tab.
    public class StudentTimetableDto
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

        // The section's homeroom teacher (any TeacherAssignment in this section with
        // IsClassTeacher = true), independent of which subject row it happens to ride on. Null
        // if no class teacher has been assigned to this section yet.
        public string ClassTeacherName { get; set; }

        public List<StudentTimetableEntryDto> Entries { get; set; } = new List<StudentTimetableEntryDto>();
    }
}
