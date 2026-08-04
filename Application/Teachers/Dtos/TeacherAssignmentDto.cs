using Domain.Enums;

namespace Application.Teachers.Dtos
{
    // ClassSectionId/SectionCode are null when the assignment covers every section of the class
    // -- a shape that's no longer creatable as of 2026-08-04 (a teacher must now be assigned to
    // one specific section, see TeacherAssignmentBuilder.BuildAsync), but a row created before
    // that date may still read back this way, so the null case is still a valid read-side shape.
    // Scope is the same information as an explicit named discriminator (ClassWide/Section)
    // instead of requiring callers to infer it from ClassSectionId's nullability.
    public class TeacherAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public Guid AcademicClassId { get; set; }
        public string SubjectCode { get; set; }
        public Guid? ClassSectionId { get; set; }
        public string SectionCode { get; set; }
        public SubjectScope Scope { get; set; }
        public bool IsClassTeacher { get; set; }
        public Guid? TimePeriodId { get; set; }
        public string TimePeriodName { get; set; }
    }
}
