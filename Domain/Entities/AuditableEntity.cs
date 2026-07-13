using Domain.Entities.Interfaces;

namespace Domain.Entities
{
    public class AuditableEntity : IAuditableEntity
    {
        public string CreatedBy { get; set; }
        public DateTimeOffset CreatedTs { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedTs { get; set; }
    }
}
