namespace Domain.Entities
{
    // A guardian is its own aggregate (not a child of Student) because one guardian can be linked
    // to several students -- the N:N link lives in StudentGuardian.
    public class Guardian : SoftDeleteAuditableEntity
    {
        public Guid Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Occupation { get; set; }
        public string Address { get; set; }
        public virtual ICollection<StudentGuardian> StudentLinks { get; set; } = new List<StudentGuardian>();
    }
}
