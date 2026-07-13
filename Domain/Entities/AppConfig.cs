namespace Domain.Entities
{
    public class AppConfig : AuditableEntity
    {
        public Guid Id { get; set; }
        public string ConfigParam { get; set; }
        public string ConfigValue { get; set; }
        public string ConfigGroup { get; set; }
        public bool IsEnable { get; set; }
    }
}
