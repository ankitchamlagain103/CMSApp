namespace Application.Teachers.Commands
{
    // ClassSectionId null = the teacher teaches this subject to every section of the class.
    // A specific section narrows the assignment -- and is required when IsClassTeacher is true,
    // because a class teacher belongs to exactly one section.
    public class AssignTeacherCommand
    {
        public Guid ClassSubjectId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public bool IsClassTeacher { get; set; }
    }
}
