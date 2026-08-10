namespace Application.Students.Dtos
{
    // Flattened link + guardian info so the "Guardians" tab doesn't need a second call per
    // guardian. RelationshipLabel resolves RelationshipCode against the GuardianRelationship
    // Config catalog server-side (2026-08-05) -- the caller no longer needs its own
    // code->label dropdown lookup just to render "Father"/"Mother" instead of "FATHER"/"MOTHER".
    public class StudentGuardianDto
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid GuardianId { get; set; }
        public string RelationshipCode { get; set; }
        public string RelationshipLabel { get; set; }
        public bool IsPrimary { get; set; }
        public string GuardianFirstName { get; set; }
        public string GuardianLastName { get; set; }
        public string GuardianPhone { get; set; }
        public string GuardianEmail { get; set; }
    }
}
