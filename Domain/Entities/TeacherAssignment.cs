namespace Domain.Entities
{
    // Links a teacher to a ClassSubject (i.e. "teaches this subject in this class"), optionally
    // narrowed to one ClassSection (null = teaches it to every section of the class). At most one
    // assignment per (Teacher, ClassSubject, ClassSection); IsClassTeacher requires a section and
    // at most one class teacher exists per ClassSection -- all enforced in the service layer.
    // Hard-deleted (pure link row).
    public class TeacherAssignment : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid TeacherId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public Guid? ClassSectionId { get; set; }
        public bool IsClassTeacher { get; set; }
        public virtual Teacher Teacher { get; set; }
        public virtual ClassSubject ClassSubject { get; set; }
        public virtual ClassSection ClassSection { get; set; }
    }
}
