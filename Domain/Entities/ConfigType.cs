namespace Domain.Entities
{
    public class ConfigType : AuditableEntity
    {
        public Guid Id { get; set; }
        public int TypeCode { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual ICollection<Config> Configs { get; set; } = new List<Config>();
    }
}
