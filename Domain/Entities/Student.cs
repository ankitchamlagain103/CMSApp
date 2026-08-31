using Domain.Enums;

namespace Domain.Entities
{
    public class Student : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }

        // Forward-looking, same convention as Employee.UserId: no navigation property (Domain
        // cannot reference ApplicationUser), plain nullable Guid, unique when populated (partial
        // index). Populated only when a portal account is provisioned for this student (2026-07-27,
        // see EmployeeService/StudentService.RegisterUserAccountAsync).
        public Guid? UserId { get; set; }

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
        public virtual ICollection<StudentDocument> Documents { get; set; } = new List<StudentDocument>();
    }
}
