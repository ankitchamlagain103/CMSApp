using Domain.Enums;

namespace Domain.Entities
{
    // No login/user linkage yet -- teachers are records, not accounts. When teacher logins are
    // built later, add the linkage on the Identity side (per the RefreshToken placement rule, an
    // entity referencing ApplicationUser cannot live in Domain).
    public class Teacher : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string EmployeeNo { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? JoiningDate { get; set; }
        public RecordStatus Status { get; set; }
        public virtual ICollection<TeacherQualification> Qualifications { get; set; } = new List<TeacherQualification>();
        public virtual ICollection<TeacherAssignment> Assignments { get; set; } = new List<TeacherAssignment>();
        public virtual ICollection<TeacherDocument> Documents { get; set; } = new List<TeacherDocument>();
    }
}
