using Domain.Enums;

namespace Application.AcademicClasses.Dtos
{
    // Read-side listing row for GET /api/academicclasses/{id}/teacher-assignments -- "who
    // teaches this class." Same core fields as Application.Employees.Dtos.TeacherAssignmentDto,
    // plus TeacherName/EmployeeCode -- a class-scoped listing is read by someone looking at the
    // class, who doesn't already have the teacher's name in context the way a teacher's own
    // profile page does.
    public class ClassTeacherAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string EmployeeCode { get; set; }
        public Guid ClassSubjectId { get; set; }
        public Guid AcademicClassId { get; set; }
        public string SubjectCode { get; set; }
        public string SubjectLabel { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public string SectionLabel { get; set; }
        public SubjectScope Scope { get; set; }
        public bool IsClassTeacher { get; set; }
        public Guid? TimePeriodId { get; set; }
        public string TimePeriodName { get; set; }
        public TimeSpan? TimePeriodStartTime { get; set; }
        public TimeSpan? TimePeriodEndTime { get; set; }
    }
}
