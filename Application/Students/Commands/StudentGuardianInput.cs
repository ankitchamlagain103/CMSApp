namespace Application.Students.Commands
{
    // One guardian entry in the student-onboarding payload. Either reference an existing
    // guardian by GuardianId (siblings share one guardian record), or leave it null and supply
    // the inline fields to create the guardian in the same call.
    public class StudentGuardianInput
    {
        public Guid? GuardianId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Occupation { get; set; }
        public string Address { get; set; }
        public string RelationshipCode { get; set; }
        public bool IsPrimary { get; set; }
    }
}
