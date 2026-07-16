using Domain.Enums;

namespace Application.Teachers.Dtos
{
    // One row of the teacher's service history -- an assignment with its academic year
    // flattened in, ordered oldest year first. The first row answers "teaching at this school
    // since which year" (alongside JoiningDate). SectionCode null = taught all sections.
    public class TeacherServiceHistoryDto
    {
        public Guid AssignmentId { get; set; }
        public Guid AcademicYearId { get; set; }
        public string AcademicYearCode { get; set; }
        public string AcademicYearName { get; set; }
        public DateTime AcademicYearStartDate { get; set; }
        public Guid AcademicClassId { get; set; }
        public string GradeCode { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public SubjectScope Scope { get; set; }
        public Guid ClassSubjectId { get; set; }
        public string SubjectCode { get; set; }
        public bool IsClassTeacher { get; set; }
    }
}
