namespace Application.Students.Commands
{
    public class LinkGuardianCommand
    {
        public Guid GuardianId { get; set; }
        public string RelationshipCode { get; set; }
        public bool IsPrimary { get; set; }
    }
}
