namespace Domain.Entities.Interfaces
{
    public interface ISoftDeleteAuditableEntity : IAuditableEntity
    {
        public bool IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTimeOffset? DeletedTs { get; set; }
    }
}
