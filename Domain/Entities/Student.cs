using Domain.Enums;

namespace Domain.Entities
{
    // No login/user linkage yet -- same note as Teacher.
    public class Student : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string AdmissionNo { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public Gender Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public DateTime? AdmissionDate { get; set; }
        public RecordStatus Status { get; set; }
        public virtual ICollection<StudentGuardian> GuardianLinks { get; set; } = new List<StudentGuardian>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
