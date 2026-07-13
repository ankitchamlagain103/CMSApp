namespace Domain.Entities
{
    // Opt-in elective subjects for one enrollment; mandatory ClassSubjects apply implicitly and
    // are never rowed here. The referenced ClassSubject must belong to the enrollment's class
    // (service-enforced). Hard-deleted (pure link row).
    public class EnrollmentSubject : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid EnrollmentId { get; set; }
        public Guid ClassSubjectId { get; set; }
        public virtual Enrollment Enrollment { get; set; }
        public virtual ClassSubject ClassSubject { get; set; }
    }
}
