using Domain.Enums;

namespace Domain.Entities
{
    // A student's placement in a ClassSection. A student may hold at most one Enrolled-status
    // row per academic year (service-enforced) -- history rows (Transferred/Withdrawn/...) stay.
    public class Enrollment : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid ClassSectionId { get; set; }
        public string RollNumber { get; set; }
        public DateTime? EnrollmentDate { get; set; }
        public EnrollmentStatus Status { get; set; }
        public virtual Student Student { get; set; }
        public virtual ClassSection ClassSection { get; set; }
        public virtual ICollection<EnrollmentSubject> ElectiveSubjects { get; set; } = new List<EnrollmentSubject>();
    }
}
