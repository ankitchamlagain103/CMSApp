using Domain.Enums;

namespace Application.Employees.Dtos
{
    // One row of an employee's teaching service history -- an assignment with its academic year
    // flattened in, ordered oldest year first. The first row answers "teaching at this school
    // since which year" (alongside JoinDate). SectionCode null = taught all sections (legacy rows
    // only, see TeacherAssignmentDto's own comment).
    public class TeacherServiceHistoryDto
    {
        public Guid AssignmentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public string AcademicYearCode { get; set; }
        public string AcademicYearName { get; set; }
        public DateTime AcademicYearStartDate { get; set; }
        public Guid AcademicClassId { get; set; }
        public string GradeCode { get; set; }
        public string GradeLabel { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public string SectionLabel { get; set; }
        public SubjectScope Scope { get; set; }
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectLabel { get; set; }
        public bool IsClassTeacher { get; set; }
    }
}
