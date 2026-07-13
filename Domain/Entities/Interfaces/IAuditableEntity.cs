namespace Domain.Entities.Interfaces
{
    public interface IAuditableEntity
    {
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }
    }
}
