using Domain.Entities.Interfaces;

namespace Domain.Entities
{
    public class SoftDeleteAuditableEntity : ISoftDeleteAuditableEntity
    {
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }
        public bool IsDeleted { get; set; }
        public string DeletedBy { get; set; }
        public DateTimeOffset? DeletedTs { get; set; }
    }
}
