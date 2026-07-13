namespace Application.Students.Dtos
{
    // Flattened link + guardian info so the student detail screen doesn't need a second call
    // per guardian.
    public class StudentGuardianDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid GuardianId { get; set; }
        public string RelationshipCode { get; set; }
        public bool IsPrimary { get; set; }
        public string GuardianFirstName { get; set; }
        public string GuardianLastName { get; set; }
        public string GuardianPhone { get; set; }
        public string GuardianEmail { get; set; }
    }
}
