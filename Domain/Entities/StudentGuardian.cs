namespace Domain.Entities
{
    // N:N link between Student and Guardian. RelationshipCode is a Config code
    // (ConfigTypeCodes.GuardianRelationship, e.g. FATHER/MOTHER), validated in the service layer,
    // not a database FK. At most one IsPrimary link per student (service-enforced by unsetting
    // the previous primary). Hard-deleted (pure link row).
    public class StudentGuardian : AuditableEntity
    {
        public Guid Id { get; set; }
        public Guid StudentId { get; set; }
        public Guid GuardianId { get; set; }
        public string RelationshipCode { get; set; }
        public bool IsPrimary { get; set; }
        public virtual Student Student { get; set; }
        public virtual Guardian Guardian { get; set; }
    }
}
